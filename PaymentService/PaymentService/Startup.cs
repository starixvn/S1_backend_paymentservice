using System.Net;
using System.Text;
using Flurl.Http;
using PaymentCore;
using PaymentCore.Helpers;
using PaymentService.Extensions;
using PaymentService.Service.BLL;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
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
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Configuration.GetValue<string>("RedisConnection")));
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();

            var proxyHost = Configuration.GetValue<string>("ProxyHost");
            var proxyPort = Configuration.GetValue<int>("ProxyPort");

            if (!string.IsNullOrEmpty(proxyHost))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                .ConfigurePrimaryHttpMessageHandler(() => proxy);

                FlurlHttp.Configure(settings => {
                    settings.HttpClientFactory = new ProxyHttpClientFactory(proxyHost, proxyPort);
                });
            }
            else
            {
                services.AddHttpClient();
            }

            /*Library*/
            services.AddSingleton<Common>();
            services.AddSingleton<CourierAdapter>();
            services.AddSingleton<HttpHelper>();
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


            /*BLL*/
            services.AddSingleton<PayBLL>();
            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<PaymentBLL>();
            
            PaymentCore.PayHelperBase.proxyHost = proxyHost;
            PaymentCore.PayHelperBase.proxyPort = proxyPort;

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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
