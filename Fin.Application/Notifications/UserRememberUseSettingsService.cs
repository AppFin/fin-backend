using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications;

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
        var entidade = await repository.Query()
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        return new UserRememberUseSettingOutput(entidade);
    }

    public async Task<bool> UpdateByCurrentUser(UserRememberUseSettingInput input, bool autoSave = false)
    {
        var setting = await repository.Query()
            .FirstOrDefaultAsync(u => u.UserId == ambientData.UserId);
        if (setting == null) return false;
        
        setting.Update(input);
        await repository.UpdateAsync(setting, autoSave);
        return true;       
    }
}