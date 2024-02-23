
using System.Text;

using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.SqlClient;
using Elastic.Apm.StackExchange.Redis;

using PaymentCore.Helpers;
using PaymentCore.Interface;
using PaymentService.Exceptions;
using PaymentService.Extensions;
using PaymentService.Service.BLL;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

using StackExchange.Redis;

namespace PaymentService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddDatabaseConnection(Configuration);
            var redisConnection = ConnectionMultiplexer.Connect(Configuration.GetValue<string>("RedisConnection"));
            redisConnection.UseElasticApm();
            services.AddSingleton<IConnectionMultiplexer>(redisConnection);
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();

            /*Library*/
            services.AddSingleton<Common>();
            services.AddSingleton<CourierAdapter>();
            services.AddSingleton<IPayHttpHelper, HttpHelper>();
            services.AddSingleton<CacheHelper>();
            services.AddSingleton<IFlurlHttpHelper, FlurlHttpHelper>();
            services.AddTransient<JsonNetSerializer, JsonNetSerializer>();
            //services.AddSingleton<IBCDBLibrary>();

            /*DAL*/

            services.AddSingleton<PayService>();
            services.AddSingleton<CustInfoService>();
            services.AddSingleton<WithdrawalLimitService>();
            services.AddSingleton<DepositService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<WithdrawalService>();
            services.AddSingleton<ConfigService>();


            /*BLL*/
            services.AddSingleton<PayBLL>();
            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<PaymentBLL>();
            services.AddSingleton<WhitelistBLL>();

            /*SiteConfig*/
            services.AddOptionListByDB<ServerInfo>("ServerInfoList_Get");
            services.AddOptionListByDB<Config>("SiteConfig_Get", new { ConfigGroup = "PaymentService" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();
            services.AddProxy();
            
            services.AddMvc(config =>
            {
                config.Filters.Add<ExceptionFilter>();
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            if (!env.IsEnvironment("Local"))
            {
                app.UseElasticApm(Configuration,
                    new HttpDiagnosticsSubscriber(),
                    new SqlClientDiagnosticSubscriber());
            }
            app.UseRouting();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            app.UseRequestLogMiddleware();
            app.UseResponseLogMiddleware();
            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                });

                endpoints.MapControllers();
            });
        }
    }
}
