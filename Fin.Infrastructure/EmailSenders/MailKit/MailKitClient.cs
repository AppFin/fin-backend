using Fin.Infrastructure.EmailSenders.Dto;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Fin.Infrastructure.EmailSenders.MailKit;

public interface IMailKitClient
{
    public Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken);
}

public class MailKitClient(IConfiguration configuration): IMailKitClient
{
    private const string EmailConfigKey = "ApiSettings:EmailSender:EmailAddress";
    private const string PasswordConfigKey = "ApiSettings:EmailSender:Password";
    
    public async Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken)
    {
        var emailAddress = configuration.GetSection(EmailConfigKey).Value ?? "";
        var emailPassword = configuration.GetSection(PasswordConfigKey).Value ?? "";
        
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(emailAddress));
        email.To.Add(MailboxAddress.Parse(dto.ToEmail));
        email.Subject = dto.Subject;
        email.Body = new TextPart("html") { Text = dto.HtmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);
        await smtp.AuthenticateAsync(emailAddress, emailPassword, cancellationToken);
        await smtp.SendAsync(email, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        return true;
    }
}