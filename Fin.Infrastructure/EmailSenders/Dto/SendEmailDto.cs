namespace Fin.Infrastructure.EmailSenders.Dto;

public class SendEmailDto
{
    public string ToEmail { get; set; }
    public string ToName { get; set; }
    public string Subject { get; set; }
    public string PlainBody { get; set; }
    public string HtmlBody { get; set; }
    
    
    /// <summary>
    /// Gets or sets the base prefix identifier used to automatically locate email template resources.
    /// </summary>
    /// <remarks>
    /// The system uses this base value to resolve specific template keys by appending standard suffixes.
    /// <br/>
    /// <strong>Example:</strong> If <c>"CreateUser_ConfirmationCode_"</c> is provided, the system will automatically look for:
    /// <list type="bullet">
    ///     <item><description><c>CreateUser_ConfirmationCode_Subject</c></description></item>
    ///     <item><description><c>CreateUser_ConfirmationCode_HTML</c></description></item>
    ///     <item><description><c>CreateUser_ConfirmationCode_Plain</c></description></item>
    /// </list>
    /// </remarks>
    public string BaseTemplatesName { get; set; }

    /// <summary>
    /// Gets or sets the dictionary containing dynamic data for template placeholder replacement.
    /// </summary>
    /// <value>
    /// The dictionary keys must match the placeholders defined in the template (e.g., <c>{{UserName}}</c>), 
    /// and the values represent the content to be rendered.
    /// </value>
    public Dictionary<string, string> TemplateProperties { get; set; }
}