using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Service.Workers;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

namespace PaymentService
{
    public class Program
    {
        private static IConfigurationRoot Configuration => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json")
            .Build();
        private static readonly List<string> Excludes = new List<string>()
        {
            "/health",
            "{HostingRequestStartingLog:l}",
            "Executing endpoint '{EndpointName}'",
            "Route matched with {RouteData}.",
            "Executing ObjectResult, writing value of type '{Type}'",
            "Executed action {ActionName}",
            "Executed endpoint '{EndpointName}'",
            "{HostingRequestFinishedLog:l}"
        };
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSerilog((hostContext, loggerConfiguration) =>
                        {
                            loggerConfiguration
                                .ReadFrom.Configuration(Configuration)
                                .Enrich.FromLogContext()
                                .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("/health")
                                    || Excludes.Any(item =>  c.MessageTemplate.ToString().Contains(item))))
                                .Enrich.WithExceptionDetails()
                                .MinimumLevel.Information().WriteTo.Console(new JsonFormatter());

                        })
                        .UseStartup<Startup>();
                }).ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<WithdrawalWorkerService>();
                });
    }
}
