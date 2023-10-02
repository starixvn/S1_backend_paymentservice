using PaymentService.Model;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using RabbitMQ.Client;

namespace PaymentService.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static string ConnectionString;
        public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            var serviceType = config.GetValue<string>("ServiceType");
            ConnectionString = config.GetConnectionString($"{serviceType}_MainDB");
            
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
        public static void AddMessageQueueConnection(this IServiceCollection services, IConfiguration config)
        {
            var hostName = config.GetSection("RabbitMQ:HostName");
            var userName = config.GetSection("RabbitMQ:UserName");
            var password = config.GetSection("RabbitMQ:Password");
            services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory()
            {
                HostName = hostName.Value,
                UserName = userName.Value,
                Password = password.Value
            });
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
            var serverInfoList = new List<ServerInfo>() { new ServerInfo() { SiteId = 0, SiteName = "default" } };
            serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

            // 準備Config
            var siteConfigGroup = provider.GetRequiredService<IEnumerable<Config>>().GroupBy(i => i.SiteId);

            foreach (var serverInfo in serverInfoList)
            {
                //設定SiteConfig
                var siteConfig = siteConfigGroup.FirstOrDefault(i => i.Key == serverInfo.SiteId) ?? siteConfigGroup.First(i => i.Key == 0);
                var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);         
                services.AddSingleton(new SiteConfig(serverInfo, config));
            }

            return services;
        }
    }
}