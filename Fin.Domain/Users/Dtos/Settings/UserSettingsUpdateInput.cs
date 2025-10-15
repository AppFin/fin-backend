namespace Fin.Domain.Users.Dtos;

public class UserSettingsUpdateInput : UserUpdateOrCreateInput
{
    public string Theme { get; set; }
    
    public bool? EmailNotifications { get; set; }
    public bool? PushNotifications { get; set; }
}
