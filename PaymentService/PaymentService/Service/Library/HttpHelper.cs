using PaymentCore.Interface;

namespace PaymentService.Service.Library
{
    public class HttpHelper : IPayHttpHelper
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }
        public HttpClient GetClient() => _clientFactory.CreateClient();
        
        public HttpClient GetClientWithProxy() => _configuration.GetValue<string>("TokyoProxy") != null
            ? _clientFactory.CreateClient("with_proxy")
            : _clientFactory.CreateClient(); 
    }
}
