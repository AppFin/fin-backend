namespace Fin.Application.Users.Dtos;

public class UserStartCreateOutput
{
    public string CreationToken { get; set; }
    public string Email { get; set; }
    public DateTime SentEmailDateTime { get; set; }
}