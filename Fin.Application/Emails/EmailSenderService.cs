using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Constants;
using Fin.Infrastructure.EmailSenders.Constants;
using Fin.Infrastructure.EmailSenders.Dto;
using Fin.Infrastructure.EmailSenders.MailKit;
using Fin.Infrastructure.EmailSenders.MailSender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fin.Application.Emails;

public interface IEmailSenderService
{
    public Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken = default);
}

public class EmailSenderService(
    IConfiguration configuration,
    IMailSenderClient mailSenderClient,
    IMailKitClient mailKitClient,
    IEmailTemplateService emailTemplateService,
    ILogger<EmailSenderService> logger
    ) : IEmailSenderService, IAutoTransient
{
    public async Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken = default)
    {
        if (configuration.GetValue<bool>(AppConstants.DemoModeConfigKey))
        {
            logger.LogInformation("Demo mode: skipping real email send to {ToEmail} (template {Template})",
                dto.ToEmail, dto.BaseTemplatesName);
            return true;
        }

        PopulateWithTemplates(dto);

        return GetMailService() switch
        {
            MailServicesConst.MailSender => await mailSenderClient.SendEmailAsync(dto, cancellationToken),
            _ => await mailKitClient.SendEmailAsync(dto, cancellationToken)
        };
    }

    private string GetMailService()
    {
        var mailService = configuration.GetSection(MailServicesConst.MailServiceConfigurationKey).Value;
        return mailService ?? "";
    }
    
    private void PopulateWithTemplates(SendEmailDto dto)
    {
        if (string.IsNullOrEmpty(dto.BaseTemplatesName)) return;
        
        dto.TemplateProperties ??= new Dictionary<string, string>();
            
        dto.HtmlBody ??= emailTemplateService.Get($"{dto.BaseTemplatesName}HTML", dto.TemplateProperties);
        dto.PlainBody ??= emailTemplateService.Get($"{dto.BaseTemplatesName}Plain", dto.TemplateProperties);
        dto.Subject ??= emailTemplateService.Get($"{dto.BaseTemplatesName}Subject", dto.TemplateProperties);
    }
}