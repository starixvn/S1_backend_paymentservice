using System.Net;

namespace PaymentCore.Interface
{
    public interface IFlurlHttpHelper
    {
        (TResult Result, HttpStatusCode StatusCode) Get<TResult>(string url, string query, bool withProxy = false)
            where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) PostUrlEncoded<TBody, TResult>(string url, TBody body, bool withProxy = false)
            where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) PostJson<TBody, TResult>(string url, TBody data, bool withProxy = false)
            where TResult : class;
        
        (TResult Result, HttpStatusCode StatusCode) PostFormData<TBody, TResult>(string url, TBody data, bool withProxy = false)
            where TResult : class;

        IFlurlHttpHelper WithOAuthBearerToken(string token);

        IFlurlHttpHelper WithHeader(string key, string value);
    }
}
