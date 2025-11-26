using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Services.CrudServices;

public interface IUserRememberUseSettingsService
{
    public Task<UserRememberUseSettingOutput> GetByCurrentUser();
    public Task<bool> UpdateByCurrentUser(UserRememberUseSettingInput input, bool autoSave = false);
}

public class UserRememberUseSettingsService(IRepository<UserRememberUseSetting> repository, IAmbientData ambientData)
    : IUserRememberUseSettingsService, IAutoTransient
{
    public async Task<UserRememberUseSettingOutput> GetByCurrentUser()
    {
        var entity = await repository
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        return entity == null ? null : new UserRememberUseSettingOutput(entity);
    }

    public async Task<bool> UpdateByCurrentUser(UserRememberUseSettingInput input, bool autoSave = false)
    {
        var setting = await repository
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        if (setting == null) return false;
        
        setting.Update(input);
        await repository.UpdateAsync(setting, autoSave);
        return true;       
    }
}