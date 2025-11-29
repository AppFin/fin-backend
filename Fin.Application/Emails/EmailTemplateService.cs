using Fin.Application.Resources.EmailTemplates;
using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Application.Emails;

public interface IEmailTemplateService
{
    public string Get(string key);
    public string Get(string key, Dictionary<string, string> parameters);
}

public class EmailTemplateService: IEmailTemplateService, IAutoTransient
{
    public string Get(string key)
    {
        return EmailTemplates.ResourceManager.GetString(key);
    }

    public string Get(string key, Dictionary<string, string> parameters)
    {
        var template = Get(key);
        foreach (var parameter in parameters)
        {
            template = template.Replace("{{" + parameter.Key +"}}", parameter.Value);
        }
        return template;
    }
}