namespace Fin.Application.Users.Dtos;

public class UserStartCreateOutput
{
    public string CreationToken { get; set; }
    public string Email { get; set; }
    public DateTime SentEmailDateTime { get; set; }

    /// Only populated in portfolio demo mode, since no real email is sent with the code.
    public string ConfirmationCode { get; set; }
}