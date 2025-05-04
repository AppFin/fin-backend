namespace Fin.Application.Authentications.Dtos;

public class UserStartCreateOutput
{
    public string CreationToken { get; set; }
    public string Email { get; set; }
    public DateTime SentEmaiDateTime { get; set; }
}