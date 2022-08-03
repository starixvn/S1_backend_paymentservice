using PaymentService.Service.Workers;
using Serilog;
using Serilog.Exceptions;
using PaymentService.Extensions;
using PaymentService;
using PaymentService.Service.Library;
using PaymentCore;

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
        .AddHostedService<WithdrawalWorkerService>();
}

var app = builder.Build();
startup.Configure(app, builder.Environment);

app.Run();