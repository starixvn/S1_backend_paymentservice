using PaymentCore.Interface;
using PaymentService.Model;
using PaymentService.Service.BLL;

namespace PaymentService.Service.Library
{
    public class HttpHelper : IPayHttpHelper
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly bool _hasProxySetting;
        public HttpHelper(IServiceProvider serviceProvider)
        {
            _clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            var configBll = serviceProvider.GetRequiredService<ConfigBLL>();
            _hasProxySetting = !string.IsNullOrEmpty(configBll.GetConfigValueFromDbByEnvironment<ProxySettingConfig>("TokyoProxy").ProxyHost);
        }
        public HttpClient GetClient() => _clientFactory.CreateClient();
        
        public HttpClient GetClientWithProxy() => _hasProxySetting
            ? _clientFactory.CreateClient("with_proxy")
            : _clientFactory.CreateClient(); 
    }
}
