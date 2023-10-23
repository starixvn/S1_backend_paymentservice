using Microsoft.AspNetCore.WebUtilities;
using PaymentCore.Extensions;

namespace PaymentService.Extensions;

public static class HttpExtension
{
    public static async Task<(Dictionary<string, string> header, string bodyContent, Dictionary<string, string> dicParams)>
        GetCallbackInfo(HttpRequest httpRequest)
    {
        var headers = httpRequest.Headers
            .ToDictionary(x => x.Key, x => x.Value.ToString());
            
        using var reader = new StreamReader(httpRequest.Body);
        var bodyContent = await reader.ReadToEndAsync();
            
        var dicParams = new Dictionary<string, string>();
        if (httpRequest.ContentType != null)
        {
            if (httpRequest.ContentType.StartsWithIgnoreCase("multipart/form-data") ||
                httpRequest.ContentType.StartsWithIgnoreCase("application/x-www-form-urlencoded"))
            {
                foreach (var (key, value) in httpRequest.Form)
                {
                    dicParams.Add(key, value);
                }
            }
        }
        else
        {
            var query = QueryHelpers.ParseQuery(httpRequest.QueryString.Value);

            foreach (var (key, value) in query)
            {
                dicParams.Add(key, value);
            }
        }

        return (headers, bodyContent, dicParams);
    }
    
}