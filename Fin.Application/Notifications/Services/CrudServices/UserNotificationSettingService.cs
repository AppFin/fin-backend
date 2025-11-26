using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Services.CrudServices;

public interface IUserNotificationSettingService
{
    public Task<UserNotificationSettingsOutput> GetByCurrentUser();
    public Task<bool> UpdateByCurrentUser(UserNotificationSettingsInput input, bool autoSave = false);
    public Task<bool> AddFirebaseToken(string token, bool autoSave = false);
}

public class UserNotificationSettingService(IRepository<UserNotificationSettings> repository, IAmbientData ambientData)
    : IUserNotificationSettingService, IAutoTransient
{
    public async Task<UserNotificationSettingsOutput> GetByCurrentUser()
    {
         var entity = await repository
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
         return entity == null ? null : new UserNotificationSettingsOutput(entity);
    }

    public async Task<bool> UpdateByCurrentUser(UserNotificationSettingsInput input, bool autoSave = false)
    {
        var setting = await repository
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        if (setting == null) return false;
        
        setting.Update(input);
        await repository.UpdateAsync(setting, autoSave);
        return true;       
    }

    public async Task<bool> AddFirebaseToken(string token, bool autoSave = false)
    {
        var setting = await repository
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        if (setting == null) return false;

        var add = setting.AddTokenIfNotExist(token);
        if (add)
            await repository.UpdateAsync(setting, autoSave);
        return true;
    }
}