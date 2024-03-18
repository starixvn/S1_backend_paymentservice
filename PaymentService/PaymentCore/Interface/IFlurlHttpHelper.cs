using System.Net;

namespace PaymentCore.Interface
{
    public interface IFlurlHttpHelper
    {
        // Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>(string url, string query, bool withProxy = false)
        //     where TResult : class;
        
        (TResult Result, HttpStatusCode StatusCode) Get<TResult>(string url, string query, bool withProxy = false)
            where TResult : class;

        // Task<(TResult Result, HttpStatusCode StatusCode)> PostUrlEncodedAsync<TBody, TResult>(string url, TBody body, bool withProxy = false);

        (TResult Result, HttpStatusCode StatusCode) PostUrlEncoded<TBody, TResult>(string url, TBody body, bool withProxy = false);

        (TResult Result, HttpStatusCode StatusCode) PostJson<TBody, TResult>(string url, TBody data, bool withProxy = false)
            where TResult : class;

        // Task<(TResult Result, HttpStatusCode StatusCode)> PostJsonAsync<TBody, TResult>(string url, TBody data, bool withProxy = false)
        //     where TResult : class;

        IFlurlHttpHelper WithOAuthBearerToken(string token);

        IFlurlHttpHelper WithHeader(string key, string value);
    }
}
