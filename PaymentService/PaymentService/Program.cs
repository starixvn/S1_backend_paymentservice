using PaymentService.Service.Workers;
using Serilog;
using Serilog.Exceptions;
using PaymentService.Extensions;
using PaymentService;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddRemoteJsonConfig(builder.Configuration.GetSection("ConfigService").Get<string[]>());

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
        .Enrich.WithExceptionDetails();
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