using System.Security;
using Fin.Application.Tenants.Dtos;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Tenants;

public interface ITenantService
{
    public Task<TenantOutput> Get(Guid id);
}

public class TenantService(IRepository<Tenant> tenantsRepository, IAmbientData ambientData): ITenantService, IAutoTransient
{
    public async Task<TenantOutput> Get(Guid id)
    {
        var entity = await tenantsRepository
            .Include(tenant => tenant.Users )
            .FirstOrDefaultAsync(tenant => tenant.Id == id);
        
        if (entity == null) return null;

        var userIsOnTenant = entity.Users.Select(user => user.Id).Contains(ambientData.UserId.GetValueOrDefault());
        if (!ambientData.IsAdmin &&  !userIsOnTenant)
            throw new SecurityException("You are not authorized to access this resource");   
        
        return new TenantOutput(entity);
    }
}