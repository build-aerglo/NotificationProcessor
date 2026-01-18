using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NotificationProcessor.Core.Interfaces;

namespace NotificationProcessor.Infrastructure.Services;

public class TemplateEngine : ITemplateEngine
{
    private readonly ILogger<TemplateEngine> _logger;
    private readonly string _templateBasePath;
    private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public TemplateEngine(ILogger<TemplateEngine> logger, string? templateBasePath = null)
    {
        _logger = logger;
        _templateBasePath = templateBasePath ?? Path.Combine(AppContext.BaseDirectory, "Templates");

        // Ensure template directory exists
        if (!Directory.Exists(_templateBasePath))
        {
            Directory.CreateDirectory(_templateBasePath);
            _logger.LogWarning("Template base path did not exist, created: {TemplatePath}", _templateBasePath);
        }
    }

    public string Render(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogWarning("Empty template provided for rendering");
            return string.Empty;
        }

        try
        {
            var result = PlaceholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;

                if (data.TryGetValue(key, out var value))
                {
                    return value?.ToString() ?? string.Empty;
                }

                _logger.LogWarning("Placeholder {Key} not found in data dictionary", key);
                return match.Value; // Keep the placeholder if no value found
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template");
            throw;
        }
    }

    public async Task<string> LoadTemplateAsync(string templateName, string channel)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            throw new ArgumentException("Template name cannot be empty", nameof(templateName));
        }

        if (string.IsNullOrEmpty(channel))
        {
            throw new ArgumentException("Channel cannot be empty", nameof(channel));
        }

        try
        {
            var channelPath = Path.Combine(_templateBasePath, channel.ToLowerInvariant());

            if (!Directory.Exists(channelPath))
            {
                throw new DirectoryNotFoundException($"Channel directory not found: {channelPath}");
            }

            // Determine file extension based on channel
            var extension = channel.ToLowerInvariant() switch
            {
                "email" => ".html",
                "sms" => ".txt",
                "inapp" => ".txt",
                _ => throw new ArgumentException($"Unsupported channel: {channel}", nameof(channel))
            };

            var templatePath = Path.Combine(channelPath, $"{templateName}{extension}");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templateName}{extension} in {channelPath}");
            }

            _logger.LogInformation("Loading template from: {TemplatePath}", templatePath);
            var content = await File.ReadAllTextAsync(templatePath);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading template {TemplateName} for channel {Channel}", templateName, channel);
            throw;
        }
    }
}
