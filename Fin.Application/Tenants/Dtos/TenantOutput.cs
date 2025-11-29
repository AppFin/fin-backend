using Fin.Domain.Tenants.Entities;

namespace Fin.Application.Tenants.Dtos;

public class TenantOutput(Tenant tenant)
{
    public Guid Id { get; set; } = tenant.Id;
    public string Locale { get; set; } = tenant.Locale;
    public string Timezone { get; set; } = tenant.Timezone;
}