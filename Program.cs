using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NotificationProcessor.Application.Interfaces;
using NotificationProcessor.Application.Services;
using NotificationProcessor.Domain.Interfaces;
using NotificationProcessor.Infrastructure.Providers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Application Services
        services.AddScoped<INotificationService, NotificationService>();

        // Infrastructure Providers
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<ISmsProvider, TwilioSmsProvider>();
    })
    .Build();

host.Run();
