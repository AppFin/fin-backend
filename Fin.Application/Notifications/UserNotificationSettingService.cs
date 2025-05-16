using Fin.Application.Globals.Dtos;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications;

public interface IUserNotificationSettingService
{
    public Task<UserNotificationSettingsOutput> GetByCurrentUser();
    public Task<bool> UpdateByCurrentUser(UserNotificationSettingsInput input, bool autoSave = false);
}

public class UserNotificationSettingService(IRepository<UserNotificationSettings> repository, IAmbientData ambientData)
    : IUserNotificationSettingService, IAutoTransient
{
    public async Task<UserNotificationSettingsOutput> GetByCurrentUser()
    {
         var entidade = await repository.Query()
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
         return new UserNotificationSettingsOutput(entidade);
    }

    public async Task<bool> UpdateByCurrentUser(UserNotificationSettingsInput input, bool autoSave = false)
    {
        var setting = await repository.Query()
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        if (setting == null) return false;
        
        setting.Update(input);
        await repository.UpdateAsync(setting, autoSave);
        return true;       
    }
}