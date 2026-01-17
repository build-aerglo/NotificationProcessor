using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Infrastructure.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register services
        services.AddScoped<INotificationConfigService, NotificationConfigService>();
        services.AddScoped<IQueueService, QueueService>();
    })
    .Build();

host.Run();
