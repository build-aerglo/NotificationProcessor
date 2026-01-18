using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NotificationProcessor.Core.Interfaces;
using NotificationProcessor.Infrastructure.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<INotificationConfigService, NotificationConfigService>();
        services.AddSingleton<IQueueService, QueueService>();

    })
    .Build();

host.Run();
