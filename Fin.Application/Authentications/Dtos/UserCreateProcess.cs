namespace Fin.Application.Authentications.Dtos;

public class UserCreateProcess
{
    public string EncryptedEmail { get; set; }
    public string EncryptedPassword { get; set; }
    public string Token { get; set; }
    public string EmailConfirmationCode { get; set; }
    public string PhoneConfirmationCode { get; set; }
    public DateTime StarDateTime { get; set; }
}