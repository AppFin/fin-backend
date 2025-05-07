namespace Fin.Application.Users.Dtos;

public class UserCreateProcessDto
{
    public string EncryptedPassword { get; set; }
    public string Token { get; set; }
    
    public string EncryptedEmail { get; set; }
    public string EmailConfirmationCode { get; set; }
    public bool ValidatedEmail { get; set; }
    public DateTime EmailSentDateTime { get; set; }
    
    public void ValidEmail(string code)
    {
        var validCode = string.Equals(code, EmailConfirmationCode, StringComparison.CurrentCultureIgnoreCase);
        ValidatedEmail = ValidatedEmail || validCode;
    }
}