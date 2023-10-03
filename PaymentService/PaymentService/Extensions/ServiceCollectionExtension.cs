using System.Net;
using Flurl.Http;
using Newtonsoft.Json;
using PaymentCore.Helpers;
using PaymentService.Model;
using PaymentService.Service.BLL;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using RabbitMQ.Client;

namespace PaymentService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = config.GetValue<string>("ServiceType");
            
            return services
                .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_MainDB"), logger)
                {
                    DataBase = DataBase.Main
                })
                .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_ReportDB"), logger)
                {
                    DataBase = DataBase.Report
                })
                .AddSingleton(param => new MySqlClient(config.GetConnectionString($"{serviceType}_WagerDB"), logger)
                {
                    DataBase = DataBase.Wager
                });
        }
        public static void AddMessageQueueConnection(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            var config = configBll.GetConfigValueByEnvironment<RabbitMqConfig>("RabbitMQ");
                
            services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
            {
                HostName = config.HostName,
                UserName = config.UserName,
                Password = config.Password
            });
        }
        
        public static void AddProxy(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            var config = configBll.GetConfigValue<ProxySettingConfig>("ProxySetting");
                
            if (!string.IsNullOrEmpty(config.ProxyHost))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(config.ProxyHost, config.ProxyPort),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                    .ConfigurePrimaryHttpMessageHandler(() => proxy);

                FlurlHttp.Configure(settings => {
                    settings.HttpClientFactory = new ProxyHttpClientFactory(config.ProxyHost, config.ProxyPort);
                });
            }
            else
            {
                services.AddHttpClient();
            }
            
            PaymentCore.PayHelperBase.ProxyHost = config.ProxyHost;
            PaymentCore.PayHelperBase.ProxyPort = config.ProxyPort;
        }
        
        public static IServiceCollection AddOptionListByDB<T>(this IServiceCollection services, string sql, object parameters = null)
        {
            var provider = services.BuildServiceProvider();
            var configService = provider.GetRequiredService<ConfigService>();
            var option = configService.Query<T>(sql, parameters);
            services.AddSingleton(typeof(IEnumerable<T>), p => option);
            return services;
        }
        
        public static IServiceCollection AddSiteConfig(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            // 準備ServerInfo
            var serverInfoList = new List<ServerInfo>() { new() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

            // 準備Config
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<Config>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteConfig
                var siteConfig = siteConfigGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId)
                                 ?? siteConfigGroup.First(i => i.Key == 0);
                var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);         
                services.AddSingleton(new SiteConfig(serverInfo, config));
            }

            return services;
        }
    }
}