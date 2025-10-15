using Fin.Domain.Global.Interfaces;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;

namespace Fin.Domain.Tenants.Entities;

public class Tenant : IEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string Locale { get; set; }
    public string Timezone { get; set; }
    public string CurrencyCode { get; set; }

    public ICollection<User> Users { get; set; }

    public Tenant()
    {
    }

    public Tenant(DateTime now)
    {
        Id = Guid.NewGuid();
        CreatedAt = now;
        UpdatedAt = now;
        Locale = "pt-BR";
        Timezone = "America/Sao_Paulo";
        CurrencyCode = "BRL";
    }

    public void Update(UserSettingsInput input)
    {
        Locale = input.Locale;
        Timezone = input.Timezone;
        CurrencyCode = input.CurrencyCode;
    }
}