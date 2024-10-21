using Microsoft.AspNetCore.HttpLogging;
using PaymentService;
using PaymentService.Extensions;
using PaymentService.Service.Workers;
using System.Net.Mime;

using System.Net;
using System.Diagnostics;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Services.AddHealthChecks();
// update environmentName ?
var environmentName = builder.Configuration.GetValue<string>("EnvironmentName");
if (!string.IsNullOrEmpty(environmentName))
{
    builder.Environment.EnvironmentName = environmentName;
}

//builder.Services.AddLogging(loggingBuilder =>
//{
//    loggingBuilder.AddConsole();
//    loggingBuilder.AddDebug();
//    loggingBuilder.add
//});
//builder.Services
// .AddHttpLogging(logging =>
// {
//     logging.LoggingFields = HttpLoggingFields.All;
//     logging.MediaTypeOptions.AddText(MediaTypeNames.Application.Json);
//     logging.MediaTypeOptions.AddText(MediaTypeNames.Text.Html);
// });


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
