using System.Net;
using System.Xml.Linq;

namespace PaymentCore.Interface
{
    public interface IFlurlHttpHelper
    {
        Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>(string url, string query)
            where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) Post<TBody, TResult>(string url, TBody body)
            where TResult : class;

        Task<(TResult Result, HttpStatusCode StatusCode)> PostAsync<TBody, TResult>(string url, TBody body)
            where TResult : class;

        Task<(TResult Result, HttpStatusCode StatusCode)> PostUrlEncodedAsync<TBody, TResult>(string url, TBody body);
        
        (TResult Result, HttpStatusCode StatusCode) PostUrlEncoded<TBody, TResult>(string url, TBody body);

        (TResult Result, HttpStatusCode StatusCode) Post<TBody, TResult>(string url, TBody body, Func<XDocument, TResult> selecor)
            where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) PostJson<TBody, TResult>(string url, TBody data)
            where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) PostJson<TResult>(string url, string data, string loggerContent = @"")
            where TResult : class;

        Task<(TResult Result, HttpStatusCode StatusCode)> PostJsonAsync<TBody, TResult>(string url, TBody data)
            where TResult : class;

        IFlurlHttpHelper WithOAuthBearerToken(string token);

        IFlurlHttpHelper WithHeader(string key, string value);
        
    }
}
