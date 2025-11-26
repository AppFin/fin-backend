namespace Fin.Infrastructure.EmailSenders.Dto;

public class SendEmailDto
{
    public string ToEmail { get; set; }
    public string ToName { get; set; }
    public string Subject { get; set; }
    public string PlainBody { get; set; }
    public string HtmlBody { get; set; }
}