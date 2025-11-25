using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Fin.Infrastructure.EmailSenders.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.EmailSenders.MailSender;

public interface IMailSenderClient
{
    public Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken = default);
}

public class MailSenderClient(HttpClient httpClient, IConfiguration configuration, ILogger<MailSenderClient> logger) : IMailSenderClient
{

    public async Task<bool> SendEmailAsync(SendEmailDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = configuration[MailSenderConstants.MailSenderApiKeyConfigurationKey];
            var fromEmail = configuration[MailSenderConstants.MailSenderAddressConfigurationKey];
            var fromName = configuration[MailSenderConstants.MailSenderNameConfigurationKey];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("MailerSend API Key was not set.");
            }

            var payload = new
            {
                from = new
                {
                    email = fromEmail,
                    name = fromName
                },
                to = new[]
                {
                    new
                    {
                        email = dto.ToEmail,
                        name = dto.ToName
                    }
                },
                subject = dto.Subject,
                text = dto.PlainBody,
                html = dto.HtmlBody
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "email");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError("Erro on send email: {}", ex.Message);
            return false;
        }
    }
}