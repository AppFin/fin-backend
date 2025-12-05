using Fin.Domain.Global.Interfaces;
using Fin.Domain.Users.Entities;

namespace Fin.Domain.Tenants.Entities;

public class Tenant: IEntity, ILoggable
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public string Locale { get; set; }
    public string Timezone { get; set; }
    
    public ICollection<User> Users { get; set; }

    public Tenant()
    {
    }
    
    public Tenant(DateTime now, string timezone, string locale)
    {
        Id = Guid.NewGuid();
        CreatedAt = now;
        UpdatedAt = now;
        Locale = locale ?? "pt-BR";
        Timezone = timezone ?? "America/Sao_Paulo";
    }

    public object GetLog()
    {
        return new
        {
            Id,
            CreatedAt,
            UpdatedAt,
            Locale,
            Timezone
        };
    }
}