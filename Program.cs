using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NotificationProcessor.Handlers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddScoped<IEmailHandler, EmailHandler>();
        services.AddScoped<ISmsHandler, SmsHandler>();
    })
    .Build();

host.Run();
