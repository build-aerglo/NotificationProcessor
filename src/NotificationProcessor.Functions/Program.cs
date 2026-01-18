using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Core.Models;
using NotificationProcessor.Infrastructure.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configuration models
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var smtpConfig = new SmtpConfiguration();
            config.GetSection("Smtp").Bind(smtpConfig);
            return smtpConfig;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var twilioConfig = new TwilioConfiguration();
            config.GetSection("Twilio").Bind(twilioConfig);
            return twilioConfig;
        });

        // Template engine
        services.AddSingleton<ITemplateEngine>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TemplateEngine>>();
            var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates");
            return new TemplateEngine(logger, templatePath);
        });

        // Notification senders
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<ISmsSender, SmsSender>();

        // Repository
        services.AddSingleton<INotificationRepository>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<NotificationRepository>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetValue<string>("Database:ConnectionString")
                ?? throw new InvalidOperationException("Database connection string not configured");
            return new NotificationRepository(logger, connectionString);
        });

        // Notification processor
        services.AddSingleton<INotificationProcessor, NotificationProcessorService>();
    })
    .Build();

host.Run();
