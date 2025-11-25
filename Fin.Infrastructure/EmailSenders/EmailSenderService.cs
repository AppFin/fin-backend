using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.EmailSenders.Constants;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.EmailSenders.MailSender;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Fin.Infrastructure.EmailSenders;

public interface IEmailSenderService
{
    public Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken = default);
}

public class EmailSenderService(
    IConfiguration configuration,
    IMailSenderClient mailSenderClient
    ) : IEmailSenderService, IAutoTransient
{
    private const string EmailConfigKey = "ApiSettings:EmailSender:EmailAddress";
    private const string PasswordConfigKey = "ApiSettings:EmailSender:Password";

    public async Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken = default)
    {
        return GetMailService() switch
        {
            MailServicesConst.MailSender => await mailSenderClient.SendEmailAsync(dto, cancellationToken),
            _ => await SendEmailWithMailKit(dto, cancellationToken)
        };
    }
    
    private string GetMailService()
    {
        var mailService = configuration.GetSection(MailServicesConst.MailServiceConfigurationKey).Value;
        return mailService ?? "";
    }

    private async Task<bool> SendEmailWithMailKit(SendEmailDto dto, CancellationToken cancellationToken)
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