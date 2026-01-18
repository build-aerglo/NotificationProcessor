namespace NotificationProcessor.Core.Interfaces;

public interface ITemplateEngine
{
    /// <summary>
    /// Renders a template by replacing {{placeholder}} with values from the data dictionary
    /// </summary>
    /// <param name="template">The template string with {{placeholder}} syntax</param>
    /// <param name="data">Dictionary containing placeholder values</param>
    /// <returns>Rendered template string</returns>
    string Render(string template, Dictionary<string, object> data);

    /// <summary>
    /// Loads a template from the file system
    /// </summary>
    /// <param name="templateName">Name of the template file (without extension)</param>
    /// <param name="channel">Channel type (email, sms, etc.)</param>
    /// <returns>Template content as string</returns>
    Task<string> LoadTemplateAsync(string templateName, string channel);
}
