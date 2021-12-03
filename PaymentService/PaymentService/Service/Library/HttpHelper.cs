using Microsoft.Extensions.Configuration;
using PaymentCore;
using System.Net.Http;

namespace PaymentService.Service.Library
{
    public class HttpHelper : IPayHttpHelper
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly IConfiguration configuration;
        public HttpHelper(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            this.clientFactory = clientFactory;
            this.configuration = configuration;
        }
        public HttpClient GetClient() => configuration.GetValue<string>("ProxyHost") != null ? clientFactory.CreateClient("with_proxy") : clientFactory.CreateClient();
    }
}
