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
    Task<UserSettingsOutput> Get();
    Task<bool> Update(UserSettingsInput input);
}

public class UserSettingsService(
    IRepository<User> userRepository,
    IAmbientData ambientData
) : IUserSettingsService, IAutoTransient
{
    public async Task<UserSettingsOutput> Get()
    {
        var userId = ambientData.UserId;

        var user = await userRepository.Query(false)
            .Include(u => u.Tenants)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new UserSettingsOutput(user, user.Tenants.FirstOrDefault());
    }

    public async Task<bool> Update(UserSettingsInput input)
    {
        var userId = ambientData.UserId;

        var user = await userRepository.Query()
            .Include(u => u.Tenants)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActivity);

        if (user == null)
            return false;
        user.Update(input);
        user.Theme = input.Theme;

        user.Tenants.FirstOrDefault().Update(input);
        await userRepository.UpdateAsync(user, true);
        return true;
       }
}
