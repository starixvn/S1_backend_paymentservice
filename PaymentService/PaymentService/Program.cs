using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Service.Workers;
using Serilog;
using Serilog.Exceptions;
using PaymentService.Extensions;

namespace PaymentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .ConfigureAppConfiguration(
                            (context, builder) =>
                            {
                                builder
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddEnvironmentVariables()
                                    .AddJsonFile($"appsettings.json", false, true)
                                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", false, true);
                                context.Configuration = builder.Build();
                                builder.AddJsonStream(ConfigurationManagerExtension.GetRemoteConfig(context.Configuration.GetSection("ConfigService").Get<string[]>()));
                            })
                        .UseSerilog((hostContext, loggerConfiguration) =>
                        {
                            var excludesMessage = hostContext.Configuration.GetSection("ExcludesMessage").Get<string[]>();
                            var excludesApiEndpoints = hostContext.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

                            loggerConfiguration
                                .ReadFrom.Configuration(hostContext.Configuration)
                                .Enrich.FromLogContext()
                                .Filter.ByExcluding(c =>
                                    c.Properties.Any(p =>
                                        excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                                        || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
                                .Enrich.WithExceptionDetails();
                        })
                        .UseStartup<Startup>();
                }).ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DepositWorkerService>();
                    services.AddHostedService<WithdrawalWorkerService>();
                });
    }
}