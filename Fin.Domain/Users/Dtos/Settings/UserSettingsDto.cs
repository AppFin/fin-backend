using Fin.Domain.Users.Entities;

namespace Fin.Domain.Users.Dtos;

public class UserSettingsDto : UserDto
{
    public bool EmailNotifications { get; set; }
    public bool PushNotifications { get; set; }

    public UserSettingsDto()
    {
    }
    
    public UserSettingsDto(User user, UserSettings settings = null) : base(user)
    {
        EmailNotifications = settings?.EmailNotifications ?? true;
        PushNotifications = settings?.PushNotifications ?? true;
    }
}
