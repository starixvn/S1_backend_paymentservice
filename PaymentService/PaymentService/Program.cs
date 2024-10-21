using PaymentService;
using PaymentService.Extensions;
using PaymentService.Service.Workers;

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
app.Run();
return;

string[] GetRemoteUrls()
{
    // 如果有 Runtime Environment Variable 優先使用
    // 沒有則使用預設 AppSetting
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
        
}
