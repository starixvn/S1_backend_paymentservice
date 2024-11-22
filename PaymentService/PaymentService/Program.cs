
using PaymentService;
using PaymentService.Extensions;
using PaymentService.Service.Workers;
using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Network;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Services.AddHealthChecks();

var environmentName = builder.Configuration.GetValue<string>("EnvironmentName");
if (!string.IsNullOrEmpty(environmentName))
{
    builder.Environment.EnvironmentName = environmentName;
}

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
        .Enrich.WithExceptionDetails()
        .Enrich.WithProperty("ApplicationName", builder.Configuration.GetValue<string>("ServiceName"))
        .Enrich.WithProperty("Env", builder.Configuration.GetValue<string>("EnvironmentName"))
        .WriteTo.UDPSink(
            builder.Configuration.GetValue<string>("OpenSearch:Uri"),
            builder.Configuration.GetValue<int>("OpenSearch:Port"),
            new Serilog.Formatting.Json.JsonFormatter())
        .WriteTo.Sentry(o =>
        {
            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;   
            o.MinimumEventLevel = LogEventLevel.Error;
            o.Dsn = builder.Configuration.GetValue<string>("Sentry:Dsn");
            o.Environment = builder.Configuration.GetValue<string>("Sentry:Environment");
        });
});


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


Debug.WriteLine("Payment start!");
app.Run();
return;

string[] GetRemoteUrls()
{
    return builder.Configuration.GetSection("ConfigService").Get<string[]>();       
}
