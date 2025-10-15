using System.Security;
using Fin.Domain.Global.Classes;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Users.Services;

public interface IUserService
{
    public Task<UserDto> Get(Guid id);
    public Task<PagedOutput<UserDto>> GetList(PagedFilteredAndSortedInput input);
    public Task<UserDto> UpdateTheme(Guid userId, string theme, bool autoSave = false);
}

public class UserService(IRepository<User> repository, IAmbientData ambientData): IUserService, IAutoTransient
{
    public async Task<UserDto> Get(Guid id)
    {
        if (!ambientData.IsAdmin && id != ambientData.UserId)
            throw new SecurityException("You are not authorized to access this resource");
        
        var entity = await repository.Query()
            .FirstOrDefaultAsync(user => user.Id == id);
        return entity != null ? new UserDto(entity) : null;
    }

    public async Task<PagedOutput<UserDto>> GetList(PagedFilteredAndSortedInput input)
    {
        return await repository.Query(false)
            .WhereIf(!ambientData.IsAdmin, user => user.Id == ambientData.UserId)
            .ApplyFilterAndSorter(input)
            .Select(user => new UserDto(user))
            .ToPagedResult(input);
    }

    public async Task<UserDto> UpdateTheme(Guid userId, string theme, bool autoSave = false)
    {
        if (!ambientData.IsAdmin && userId != ambientData.UserId)
            throw new SecurityException("You are not authorized to access this resource");

        var user = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Microsoft.AspNetCore.Http.BadHttpRequestException("User not found");

        user.UpdateTheme(theme, DateTime.UtcNow);
        await repository.UpdateAsync(user, autoSave);

        return new UserDto(user);
    }
}