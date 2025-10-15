using System.Security;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Users.Services;

public interface IUserSettingsService
{
    Task<UserSettingsDto> Get();
    Task<UserSettingsDto> Update(UserSettingsUpdateInput input);
}

public class UserSettingsService(
    IRepository<UserSettings> settingsRepository,
    IRepository<User> userRepository,
    IAmbientData ambientData
) : IUserSettingsService, IAutoTransient
{
    public async Task<UserSettingsDto> Get()
    {
        var userId = ambientData.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var user = await userRepository.Query(false)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var settings = await settingsRepository.Query(false)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return new UserSettingsDto(user, settings);
    }

    public async Task<UserSettingsDto> Update(UserSettingsUpdateInput input)
    {
        var userId = ambientData.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var user = await userRepository.Query()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new BadHttpRequestException("User not found");

        var now = DateTime.UtcNow;
        var userChanged = false;

        if (!string.IsNullOrEmpty(input.FirstName) || !string.IsNullOrEmpty(input.LastName) || 
            !string.IsNullOrEmpty(input.DisplayName) || input.Gender != default || 
            input.BirthDate.HasValue || !string.IsNullOrEmpty(input.ImagePublicUrl))
        {
            input.FirstName ??= user.FirstName;
            input.LastName ??= user.LastName;
            input.DisplayName ??= user.DisplayName;
            input.Gender = input.Gender != default ? input.Gender : user.Gender;
            input.BirthDate ??= user.BirthDate;
            input.ImagePublicUrl ??= user.ImagePublicUrl;
            
            user.Update(input, now);
            userChanged = true;
        }

        if (!string.IsNullOrEmpty(input.Theme))
        {
            user.UpdateTheme(input.Theme, now);
            userChanged = true;
        }

        if (userChanged)
            await userRepository.UpdateAsync(user, autoSave: false);

        UserSettings settings = await settingsRepository.Query()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (input.EmailNotifications.HasValue || input.PushNotifications.HasValue)
        {
            if (settings == null)
            {
                settings = new UserSettings(
                    userId,
                    input.EmailNotifications ?? true,
                    input.PushNotifications ?? false
                );
                await settingsRepository.AddAsync(settings, autoSave: false);
            }
            else
            {
                settings.Update(
                    input.EmailNotifications ?? settings.EmailNotifications,
                    input.PushNotifications ?? settings.PushNotifications
                );
                await settingsRepository.UpdateAsync(settings, autoSave: false);
            }
        }

        await userRepository.SaveChangesAsync();

        return new UserSettingsDto(user, settings);
    }
}
