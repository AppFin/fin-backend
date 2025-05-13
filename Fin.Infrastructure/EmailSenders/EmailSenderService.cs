using Fin.Infrastructure.AutoServices.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Fin.Infrastructure.EmailSenders;

public interface IEmailSenderService
{
    public Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailSenderService: IEmailSenderService, IAutoTransient
{
    private readonly IConfiguration _configuration;
    
    private const string EmailConfigKey = "ApiSettings:EmailSender:EmailAddress";
    private const string PasswordConfigKey = "ApiSettings:EmailSender:Password";

    public EmailSenderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailAddress = _configuration.GetSection(EmailConfigKey).Value ?? "";
        var emailPassword = _configuration.GetSection(PasswordConfigKey).Value ?? "";
        
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(emailAddress));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(emailAddress, emailPassword);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}