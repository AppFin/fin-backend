using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Domain.Users.Enums;

namespace Fin.Domain.Users.Dtos;

public class UserSettingsOutput : UserDto
{
    public string Theme { get; set; }
    public string Locale { get; set; }
    public string Timezone { get; set; }
    public string CurrencyCode { get; set; }


    public UserSettingsOutput(User user, Tenant tenant) : base(user)
    {
        Locale = tenant.Locale;
        Timezone = tenant.Timezone;
        CurrencyCode = tenant.CurrencyCode;
        Theme = user.Theme;
    }
    
}