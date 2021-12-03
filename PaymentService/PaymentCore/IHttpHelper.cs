using System.Net.Http;

namespace PaymentCore
{
    public interface IPayHttpHelper
    {
        HttpClient GetClient();
    }
}
