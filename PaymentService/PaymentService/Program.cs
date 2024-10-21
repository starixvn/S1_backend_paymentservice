using Microsoft.AspNetCore.HttpLogging;
using PaymentService;
using PaymentService.Extensions;
using PaymentService.Service.Workers;
using System.Net.Mime;
using Microsoft.AspNetCore.HttpLogging;
var builder = WebApplication.CreateBuilder(args);

// update environmentName ?
var environmentName = builder.Configuration.GetValue<string>("EnvironmentName");
if (!string.IsNullOrEmpty(environmentName))
{
    builder.Environment.EnvironmentName = environmentName;
}

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});
builder.Services
 .AddHttpLogging(logging =>
 {
     logging.LoggingFields = HttpLoggingFields.All;
     logging.MediaTypeOptions.AddText(MediaTypeNames.Application.Json);
     logging.MediaTypeOptions.AddText(MediaTypeNames.Text.Html);
 })
  .AddHealthChecks();

builder.WebHost.UseSentry();
builder.Services.AddHealthChecks();
var remoteUrls = GetRemoteUrls();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var serviceType = Environment.GetEnvironmentVariable("ServiceType");
if (!string.IsNullOrEmpty(serviceType) && serviceType.Equals("worker", StringComparison.OrdinalIgnoreCase))
{
    builder.Services
        .AddHostedService<DepositWorkerService>()
        .AddHostedService<WithdrawalWorkerService>()
        .AddHostedService<DepositAutoCancelWorkerService>();
}

var app = builder.Build();
startup.Configure(app, builder.Environment);

const int minWorkers = 16;
const int minIOCPs = 4;
ThreadPool.SetMinThreads(minWorkers, minIOCPs);
app.MapHealthChecks("/health");
app.MapHealthChecks("/");

app.UseHttpLogging();
app.Use(async (context, next) =>
{
    await next();

    var logger = context.RequestServices.GetService<ILoggerFactory>()
        ?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

    if (logger?.IsEnabled(LogLevel.Information) == true)
    {
        using var stream = new StreamReader(context.Request.Body);

        _ = await stream.ReadToEndAsync().ConfigureAwait(false);
    }
});

app.Run();
return;

string[] GetRemoteUrls()
{
    // 如果有 Runtime Environment Variable 優先使用
    // 沒有則使用預設 AppSetting
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
        
}
