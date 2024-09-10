using PaymentService;
using PaymentService.Extensions;
using PaymentService.Service.Workers;

using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

var remoteUrls = GetRemoteUrls();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesResponseEmptyMessage = builder.Configuration.GetValue<string>("ExcludesResponseEmptyMessage");
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item)
                || c.MessageTemplate.ToString().Equals(excludesResponseEmptyMessage))))
        .Enrich.WithExceptionDetails();

    OverrideLoggerWriteToLFile(Configuration);
});

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

app.Run();
return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/payment/payment-{DateTime.Now:yyyyMMddHHmmssfff}.log";
    configuration
        .WriteTo.File(new JsonFormatter(),
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 10485760,
            rollOnFileSizeLimit: true);
}

string[] GetRemoteUrls()
{
    // 如果有 Runtime Environment Variable 優先使用
    // 沒有則使用預設 AppSetting
    return Environment.GetEnvironmentVariable("ConfigServiceUrl") is null
        ? builder.Configuration.GetSection("ConfigService").Get<string[]>()
        : new List<string>() { Environment.GetEnvironmentVariable("ConfigServiceUrl") }.ToArray();
}
