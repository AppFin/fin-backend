using Fin.Application.Resources.EmailTemplates;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Constants;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Emails;

public interface IEmailTemplateService
{
    public string Get(string key);
    public string Get(string key, Dictionary<string, string> parameters);
}

public class EmailTemplateService(IConfiguration configuration): IEmailTemplateService, IAutoTransient
{
    public string Get(string key)
    {
        return EmailTemplates.ResourceManager.GetString(key);
    }

    public string Get(string key, Dictionary<string, string> parameters)
    {
        var template = Get(key);
        
        PopulateDefaultParameters(parameters);
        foreach (var parameter in parameters)
        {
            template = template.Replace("{{" + parameter.Key +"}}", parameter.Value);
        }
        return template;
    }

    private void PopulateDefaultParameters(Dictionary<string, string> parameters)
    {
        var frontUrl = configuration.GetSection(AppConstants.FrontUrlConfigKey).Get<string>();
        var logoIconUrl = $"{frontUrl}/icons/fin.png";
        
        parameters.TryAdd("appName", AppConstants.AppName);
        parameters.TryAdd("logoIconUrl", logoIconUrl);
    }
}