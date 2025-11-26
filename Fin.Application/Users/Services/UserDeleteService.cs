using System.Security;
using Fin.Application.Users.Utils;
using Fin.Domain.Global;
using Fin.Domain.Global.Classes;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Constants;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Users.Services;

public interface IUserDeleteService
{
    public Task<bool> RequestDeleteUser(CancellationToken cancellationToken = default);
    public Task<bool> EffectiveDeleteUsers(CancellationToken cancellationToken = default);
    public Task<bool> AbortDeleteUser(Guid userId, CancellationToken cancellationToken = default);
    public Task<PagedOutput<UserDeleteRequestDto>> GetList(PagedFilteredAndSortedInput input, CancellationToken cancellationToken = default);
}

public class UserDeleteService(
    IRepository<UserDeleteRequest> userDeleteRequestRepo,
    IRepository<User> userRepo,
    IDateTimeProvider dateTimeProvider,
    IAmbientData ambientData,
    IUnitOfWork unitOfWork,
    IEmailSenderService emailSender,
    IConfiguration configuration,
    IRepository<UserCredential> credentialRepo,
    IRepository<Notification> notificationRepo,
    IRepository<NotificationUserDelivery> notificationDeliveryRepo,
    IRepository<UserRememberUseSetting> rememberRepo,
    IRepository<UserNotificationSettings> notificationSettingsRepo,
    IRepository<Tenant> tenantRepo,
    IRepository<TenantUser> tenantUserRepo
) : IUserDeleteService, IAutoTransient
{
    private readonly CryptoHelper _cryptoHelper = new(
        configuration.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "",
        configuration.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ?? "");

    public async Task<bool> RequestDeleteUser(CancellationToken cancellationToken = default)
    {
        var userId = ambientData.UserId;

        var alreadyRequest = await userDeleteRequestRepo.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted, cancellationToken);
        if (alreadyRequest != null) return false;

        var user = userRepo.Include(u => u.Credential).FirstOrDefault(u => u.Id == userId);
        if (user == null) return false;

        var userEmail = _cryptoHelper.Decrypt(user.Credential.EncryptedEmail);

        user.RequestDelete(dateTimeProvider.UtcNow());
        await userRepo.UpdateAsync(user, true, cancellationToken);

        await SendDeleteAccountEmailAsync(cancellationToken, userEmail);
        return true;
    }

    public async Task<bool> EffectiveDeleteUsers(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow());
        var userIds = await userDeleteRequestRepo
            .Where(u => !u.Aborted && u.DeleteEffectivatedAt == today)
            .Select(u => u.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
            await DeleteUser(userId, cancellationToken);

        return true;
    }

    public async Task<bool> AbortDeleteUser(Guid userId, CancellationToken cancellationToken = default)
    {
        if ((!ambientData.IsAdmin && ambientData.UserId != userId) || !ambientData.UserId.HasValue)
            throw new SecurityException("You are not admin");

        var now = dateTimeProvider.UtcNow();
        var deleteRequest = await userDeleteRequestRepo
            .Include(u => u.User)
            .ThenInclude(u => u.Credential)
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted, cancellationToken);
        if (deleteRequest == null) return false;

        deleteRequest.Abort(ambientData.UserId.Value, now);
        await userDeleteRequestRepo.UpdateAsync(deleteRequest, true, cancellationToken);

        await SendAbortDeleteEmailAsync(cancellationToken, deleteRequest);
        return true;
    }

    public async Task<PagedOutput<UserDeleteRequestDto>> GetList(PagedFilteredAndSortedInput input, CancellationToken cancellationToken = default)
    {
        return await userDeleteRequestRepo.AsNoTracking()
            .Include(u => u.User)
            .Include(u => u.UserAborted)
            .ApplyFilterAndSorter(input)
            .Select(n => new UserDeleteRequestDto(n))
            .ToPagedResult(input, cancellationToken );
    }

    private async Task DeleteUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow());

        var deleteRequest = await userDeleteRequestRepo
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted && u.DeleteEffectivatedAt <= today,
                cancellationToken);
        if (deleteRequest == null) return;

        // TODO here need to add all related tables;

        var user = await userRepo.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var credential = await credentialRepo.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        var tenantsUser = await tenantUserRepo
            .Where(u => u.UserId == userId)
            .ToListAsync(cancellationToken);
        var tenantIds = tenantsUser.Select(t => t.TenantId).ToList();
        var tenants = await tenantRepo
            .Where(t => tenantIds.Contains(t.Id))
            .ToListAsync(cancellationToken);
        var userEmail = _cryptoHelper.Decrypt(credential.EncryptedEmail);


        var otherDeleteRequests = await userDeleteRequestRepo
            .Where(u => u.UserId == userId && u.Id != deleteRequest.Id)
            .ToListAsync(cancellationToken);

        var notificationSetting = await notificationSettingsRepo
            .FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);
        var rememberSetting =
            await rememberRepo.FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);

        var notificationDeliveries = await notificationDeliveryRepo
            .Include(n => n.Notification)
            .ThenInclude(n => n.UserDeliveries)
            .Where(n => n.UserId == userId).ToListAsync(cancellationToken);
        var notifications = notificationDeliveries.Select(n => n.Notification)
            .Where(n => n.UserDeliveries.Count == 1);

        await using (var scope = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {

            foreach (var notification in notifications)
                await notificationRepo.DeleteAsync(notification, cancellationToken);
            foreach (var delivery in notificationDeliveries)
                await notificationDeliveryRepo.DeleteAsync(delivery, cancellationToken);

            await rememberRepo.DeleteAsync(rememberSetting, cancellationToken);
            await notificationSettingsRepo.DeleteAsync(notificationSetting, cancellationToken);

            foreach (var otherDeleteRequest in otherDeleteRequests)
                await userDeleteRequestRepo.DeleteAsync(otherDeleteRequest, cancellationToken);

            foreach (var tenantUser in tenantsUser)
                await tenantUserRepo.DeleteAsync(tenantUser, cancellationToken);

            foreach (var tenant in tenants)
                await tenantRepo.DeleteAsync(tenant, cancellationToken);

            await credentialRepo.DeleteAsync(credential, cancellationToken);
            await userDeleteRequestRepo.DeleteAsync(deleteRequest, cancellationToken);
            await userRepo.DeleteAsync(user, cancellationToken);

            await SendAccountDeletedEmailAsync(cancellationToken, userEmail);

            await scope.CompleteAsync(cancellationToken);
        }
    }
    
     private async Task<bool> SendAbortDeleteEmailAsync(CancellationToken cancellationToken, UserDeleteRequest deleteRequest)
    {
        var userEmail = _cryptoHelper.Decrypt(deleteRequest.User.Credential.EncryptedEmail);
        
        var frontUrl = configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
        var logoIconUrl = $"{frontUrl}/icons/fin.png";

        var htmlBody = AbortDeleteUserTemplates.AbortDeletionTemplate
            .Replace("{{appName}}", AppConstants.AppName)
            .Replace("{{logoIconUrl}}", logoIconUrl);
        
        var plainBody = AbortDeleteUserTemplates.AbortDeletionPlainTemplate
            .Replace("{{appName}}", AppConstants.AppName);
        
        var subject = AbortDeleteUserTemplates.AbortDeletionSubject
            .Replace("{{appName}}", AppConstants.AppName);
        
        return await emailSender.SendEmailAsync(new SendEmailDto
        {
            ToEmail = userEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainBody = plainBody
        }, cancellationToken);
    }
    
    private async Task<bool> SendDeleteAccountEmailAsync(CancellationToken cancellationToken, string userEmail)
    {
        var frontUrl = configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
        var logoIconUrl = $"{frontUrl}/icons/fin.png";

        var htmlBody = DeleteUserTemplates.AccountDeletionTemplate
            .Replace("{{appName}}", AppConstants.AppName)
            .Replace("{{logoIconUrl}}", logoIconUrl);
        
        var plainBody = DeleteUserTemplates.AccountDeletionPlainTemplate
            .Replace("{{appName}}", AppConstants.AppName);
        
        var subject = DeleteUserTemplates.AccountDeletionSubject
            .Replace("{{appName}}", AppConstants.AppName);
        
        return await emailSender.SendEmailAsync(new SendEmailDto
        {
            ToEmail = userEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainBody = plainBody
        }, cancellationToken);
    }
    
    private async Task<bool> SendAccountDeletedEmailAsync(CancellationToken cancellationToken, string userEmail)
    {
        var frontUrl = configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
        var logoIconUrl = $"{frontUrl}/icons/fin.png";

        var htmlBody = AccountDeletedTemplates.AccountDeletedTemplate
            .Replace("{{appName}}", AppConstants.AppName)
            .Replace("{{logoIconUrl}}", logoIconUrl);
        
        var plainBody = AccountDeletedTemplates.AccountDeletedPlainTemplate
            .Replace("{{appName}}", AppConstants.AppName);
        
        var subject = AccountDeletedTemplates.AccountDeletedSubject
            .Replace("{{appName}}", AppConstants.AppName);
        
        return await emailSender.SendEmailAsync(new SendEmailDto
        {
            ToEmail = userEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainBody = plainBody
        }, cancellationToken);
    }
}