namespace Fin.Application.Authentications.Dtos;

public class LoginWithGoogleInput
{
    public string GoogleId { get; set; }
    public string DisplayName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PictureUrl { get; set; }
}