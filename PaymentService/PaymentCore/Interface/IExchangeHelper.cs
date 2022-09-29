using PaymentCore.Models.Result;

namespace PaymentCore.Interface;

public interface IExchangeHelper
{
    //ExchangeResult GetExchangeAsync(Dictionary<string, string> payResource);
    //public System.Threading.Tasks.Task<ExchangeResult> GetExchangeAsync(Dictionary<string, string> payResource);
    public ExchangeResult GetExchange(Dictionary<string, string> payResource);
}