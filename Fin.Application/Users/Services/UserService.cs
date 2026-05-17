using System.Security;
using Fin.Domain.Global.Classes;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Users.Services;

public interface IUserService
{
    public Task<UserDto> Get(Guid id);
    public Task<PagedOutput<UserDto>> GetList(PagedFilteredAndSortedInput input);
    public Task Update(Guid userId, UserUpdateOrCreateInput input, CancellationToken token);
}

public class UserService(IRepository<User> repository, IAmbientData ambientData, IDateTimeProvider dateTimeProvider) : IUserService, IAutoTransient
{
    public async Task<UserDto> Get(Guid id)
    {
        if (!ambientData.IsAdmin && id != ambientData.UserId)
            throw new SecurityException("You are not authorized to access this resource");

        var entity = await repository
            .Include(u => u.Tenants)
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);
        
        if (entity == null) return null;
        
        entity.Tenants?.ToList()?.ForEach(t =>
        {
            t.Users = null;
        });
        return new UserDto(entity);
    }

    public async Task<PagedOutput<UserDto>> GetList(PagedFilteredAndSortedInput input)
    {
        return await repository.AsNoTracking()
            .WhereIf(!ambientData.IsAdmin, user => user.Id == ambientData.UserId)
            .ApplyFilterAndSorter(input)
            .Select(user => new UserDto(user))
            .ToPagedResult(input);
    }

    public async Task Update(Guid userId, UserUpdateOrCreateInput input, CancellationToken token)
    {
        if (!ambientData.IsAdmin && userId != ambientData.UserId)
            throw new SecurityException("You are not authorized to access this resource");

        var entity = await repository
            .Include(u => u.Tenants)
            .FirstOrDefaultAsync(user => user.Id == userId, token);
        if (entity == null) throw new KeyNotFoundException("User not found");

        var now = dateTimeProvider.UtcNow();
        entity.Update(input, now);
        entity.Tenants?.FirstOrDefault()?.Update(now, input.Timezone, input.Locale);
        
        await repository.UpdateAsync(entity, autoSave: true, token);
    }
}