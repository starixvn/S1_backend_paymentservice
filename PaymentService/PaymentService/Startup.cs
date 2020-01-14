using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Service.BLL;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;

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
            /*Library*/
            services.AddSingleton<Common>();
            services.AddSingleton<CourierAdapter>();
            //services.AddSingleton<IBCDBLibrary>();

            /*DAL*/
            services.AddSingleton<PayService>();
            services.AddSingleton<CustInfoService>();
            services.AddSingleton<DepoistService>();
            services.AddSingleton<WalletService>();
            services.AddSingleton<WithdrawalService>();

            /*BLL*/
            services.AddSingleton<PayBLL>();
            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<PaymentBLL>();

            PaymentCore.PayHelperBase.proxyHost = Configuration.GetValue<string>("ProxyHost");
            PaymentCore.PayHelperBase.proxyPort = Configuration.GetValue<int>("ProxyPort");

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

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
