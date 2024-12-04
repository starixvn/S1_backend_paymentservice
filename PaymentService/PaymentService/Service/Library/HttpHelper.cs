using PaymentCore.Interface;
using PaymentService.Model;
using PaymentService.Service.BLL;
using System.Configuration;

namespace PaymentService.Service.Library
{
    public class HttpHelper : IPayHttpHelper
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly bool _hasProxySetting;
    
        public HttpHelper(IServiceProvider serviceProvider)
        {
            _clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _hasProxySetting = !string.IsNullOrEmpty(configuration.GetValue<string>("ProxyHost"));
            Console.WriteLine("hasProxySetting: " + _hasProxySetting);
        }
        public HttpClient GetClient() => _clientFactory.CreateClient();
        
        public HttpClient GetClientWithProxy() => _hasProxySetting
            ? _clientFactory.CreateClient("with_proxy")
            : _clientFactory.CreateClient(); 
    }
}
