using System.Security;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Users.Services;

public interface IUserDeleteService
{
    public Task<bool> RequestDeleteUser(Guid userId);
    public Task<bool> DeleteUser(Guid userId);
    public Task<bool> AbortDeleteUser(Guid userId);
}

public class UserDeleteService(
    IRepository<UserDeleteRequest> userDeleteRequestRepo,
    IRepository<User> userRepo,

    IDateTimeProvider dateTimeProvider,
    IAmbientData ambientData,
    IUnitOfWork unitOfWork,

    IRepository<UserCredential> credentialRepo,
    IRepository<Notification> notificationRepo,
    IRepository<NotificationUserDelivery> notificationDeliveryRepo,
    IRepository<UserRememberUseSetting> rememberRepo,
    IRepository<UserNotificationSettings> notificationSettingsRepo,
    IRepository<Tenant> tenantRepo,
    IRepository<TenantUser> tenantUserRepo
    ): IUserDeleteService, IAutoTransient
{
    public async Task<bool> RequestDeleteUser(Guid userId)
    {
        var alreadyRequest = await userDeleteRequestRepo.Query(false)
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted);
        if (alreadyRequest != null) return false;

        var user = userRepo.Query().FirstOrDefault(u => u.Id == userId);
        if (user == null) return false;

        user.RequestDelete(dateTimeProvider.UtcNow());
        await userRepo.UpdateAsync(user, true);
        return true;
    }

    public async Task<bool> DeleteUser(Guid userId)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow());

        var deleteRequest = await userDeleteRequestRepo.Query()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted && u.DeleteEffectivatedAt <= today);
        if (deleteRequest == null) return false;

        // TODO here need to add all related tables;

        var user = await userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId);
        var credential = await credentialRepo.Query().FirstOrDefaultAsync(u => u.Id == userId);
        var tenant = await tenantRepo.Query().FirstOrDefaultAsync(t => t.Id == userId);
        var tenantUser = await tenantUserRepo.Query().FirstOrDefaultAsync(u => u.UserId == userId && u.TenantId == tenant.Id);

        var notificationSetting = await notificationSettingsRepo.Query().FirstOrDefaultAsync(n => n.UserId == userId);
        var rememberSetting = await rememberRepo.Query().FirstOrDefaultAsync(n => n.UserId == userId);

        var notificationDeliveries = await notificationDeliveryRepo.Query()
            .Include(n => n.Notification)
            .ThenInclude(n => n.UserDeliveries)
            .Where(n => n.UserId == userId).ToListAsync();
        var notifications = notificationDeliveries.Select(n => n.Notification)
            .Where(n => n.UserDeliveries.Count == 1);

        await unitOfWork.BeginTransactionAsync();

        foreach (var notification in notifications)
            await notificationRepo.DeleteAsync(notification);
        foreach (var delivery in notificationDeliveries)
            await notificationDeliveryRepo.DeleteAsync(delivery);

        await rememberRepo.DeleteAsync(rememberSetting);
        await notificationSettingsRepo.DeleteAsync(notificationSetting);

        await tenantUserRepo.DeleteAsync(tenantUser);
        await tenantRepo.DeleteAsync(tenant);
        await credentialRepo.DeleteAsync(credential);
        await userDeleteRequestRepo.DeleteAsync(deleteRequest);
        await userRepo.DeleteAsync(user);

        await unitOfWork.CommitAsync();

        return false;
    }

    public async Task<bool> AbortDeleteUser(Guid userId)
    {
        if ((!ambientData.IsAdmin && ambientData.UserId != userId) || !ambientData.UserId.HasValue)
            throw new SecurityException("You are not admin");


        var now = dateTimeProvider.UtcNow();
        var deleteRequest = await userDeleteRequestRepo.Query()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.Aborted);
        if (deleteRequest == null) return false;

        deleteRequest.Abort(ambientData.UserId.Value, now);
        await userDeleteRequestRepo.UpdateAsync(deleteRequest);
        return true;
    }
}