namespace Fin.Domain.Users.Dtos;

public class UserSettingsInput : UserUpdateOrCreateInput
{
    public string Theme { get; set; }
    public string Locale { get; set; }
    public string Timezone { get; set; }
    public string CurrencyCode { get; set; }


}
