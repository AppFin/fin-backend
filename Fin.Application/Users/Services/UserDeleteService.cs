using System.Security;
using Fin.Domain.Global;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications.Consts;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Users.Services;

public interface IUserDeleteService
{
    public Task<bool> RequestDeleteUser(CancellationToken cancellationToken = default);
    public Task<bool> EffectiveDeleteUsers(CancellationToken cancellationToken = default);
    public Task<bool> AbortDeleteUser(Guid userId, CancellationToken cancellationToken = default);
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
        configuration.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value ?? "",
        configuration.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value ?? "");

    public async Task<bool> RequestDeleteUser(CancellationToken cancellationToken = default)
    {
        var userId = ambientData.UserId;

        var alreadyRequest = await userDeleteRequestRepo.Query(false)
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted, cancellationToken);
        if (alreadyRequest != null) return false;

        var user = userRepo.Query().Include(u => u.Credential).FirstOrDefault(u => u.Id == userId);
        if (user == null) return false;

        var userEmail = _cryptoHelper.Decrypt(user.Credential.EncryptedEmail);

        user.RequestDelete(dateTimeProvider.UtcNow());
        await userRepo.UpdateAsync(user, true, cancellationToken);

        await emailSender.SendEmailAsync(userEmail, "Solicitação de deleção", "Recebemos sua solicitação de deleção de conta. Sua conta foi inativada e será deletada em 30 dias. Caso você se arrependa entre em contato com nosso suporte para abortar a deleção.");
        return true;
    }

    public async Task<bool> EffectiveDeleteUsers(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow());
        var userIds = await userDeleteRequestRepo.Query(false)
            .Where(u => !u.Aborted && u.DeleteEffectivatedAt == today)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
            await userDeleteRequestRepo.DeleteAsync(userId, cancellationToken);

        return true;
    }

    public async Task<bool> AbortDeleteUser(Guid userId, CancellationToken cancellationToken = default)
    {
        if ((!ambientData.IsAdmin && ambientData.UserId != userId) || !ambientData.UserId.HasValue)
            throw new SecurityException("You are not admin");

        var now = dateTimeProvider.UtcNow();
        var deleteRequest = await userDeleteRequestRepo.Query()
            .Include(u => u.User)
            .ThenInclude(u => u.Credential)
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted, cancellationToken);
        if (deleteRequest == null) return false;

        deleteRequest.Abort(ambientData.UserId.Value, now);
        await userDeleteRequestRepo.UpdateAsync(deleteRequest, cancellationToken);

        var userEmail = _cryptoHelper.Decrypt(deleteRequest.User.Credential.EncryptedEmail);
        await emailSender.SendEmailAsync(userEmail, "Solicitação de deleção abortada", "Sua solicitação de deleção do FinApp foi abortada e sua conta não será mais deletada.");

        return true;
    }

    private async Task<bool> DeleteUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow());

        var deleteRequest = await userDeleteRequestRepo.Query()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted && u.DeleteEffectivatedAt <= today,
                cancellationToken);
        if (deleteRequest == null) return false;

        // TODO here need to add all related tables;

        var user = await userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var credential = await credentialRepo.Query().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var tenant = await tenantRepo.Query().FirstOrDefaultAsync(t => t.Id == userId, cancellationToken);
        var tenantUser = await tenantUserRepo.Query()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenant.Id, cancellationToken);

        var userEmail = _cryptoHelper.Decrypt(credential.EncryptedEmail);

        var notificationSetting = await notificationSettingsRepo.Query()
            .FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);
        var rememberSetting =
            await rememberRepo.Query().FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken);

        var notificationDeliveries = await notificationDeliveryRepo.Query()
            .Include(n => n.Notification)
            .ThenInclude(n => n.UserDeliveries)
            .Where(n => n.UserId == userId).ToListAsync(cancellationToken);
        var notifications = notificationDeliveries.Select(n => n.Notification)
            .Where(n => n.UserDeliveries.Count == 1);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        foreach (var notification in notifications)
            await notificationRepo.DeleteAsync(notification, cancellationToken);
        foreach (var delivery in notificationDeliveries)
            await notificationDeliveryRepo.DeleteAsync(delivery, cancellationToken);

        await rememberRepo.DeleteAsync(rememberSetting, cancellationToken);
        await notificationSettingsRepo.DeleteAsync(notificationSetting, cancellationToken);

        await tenantUserRepo.DeleteAsync(tenantUser, cancellationToken);
        await tenantRepo.DeleteAsync(tenant, cancellationToken);
        await credentialRepo.DeleteAsync(credential, cancellationToken);
        await userDeleteRequestRepo.DeleteAsync(deleteRequest, cancellationToken);
        await userRepo.DeleteAsync(user, cancellationToken);

        await emailSender.SendEmailAsync(userEmail, "Conta deletada", "Sua conta no FinApp foi deletada. Agora você não poderá mais acessar seus dados e eles foram removidos da plataforma.");

        await unitOfWork.CommitAsync(cancellationToken);

        return false;
    }
}