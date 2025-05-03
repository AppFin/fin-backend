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

    public EmailSenderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailAddress = _configuration["AppSettings:EmailSender:EmailAddress"];
        var emailPassword = _configuration["AppSettings:EmailSender:Password"];
        
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