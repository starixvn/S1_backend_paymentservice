using System.Diagnostics;
using System.Net;
using System.Xml.Serialization;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models;
using Serilog.Context;
using JsonExtension = PaymentCore.Extensions.JsonExtension;

namespace PaymentCore.Helpers
{
    public sealed class FlurlHttpHelper : IFlurlHttpHelper
    {
        private readonly Dictionary<string, string> _header;
        private readonly IPayHttpHelper _httpHelper;
        private readonly ILogger<FlurlHttpHelper> _logger;
        private string _authToken = string.Empty;

        public FlurlHttpHelper(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<FlurlHttpHelper>>();
            _httpHelper = serviceProvider.GetRequiredService<IPayHttpHelper>();
            _header = new Dictionary<string, string>();
        }
        
        public (TResult Result, HttpStatusCode StatusCode) Get<TResult>
            (string url, string query, bool withProxy = false) where TResult : class
        {
            var result = GetAsync<TResult>(url, query, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }

        private async Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>
            (string url, string query, bool withProxy = false) where TResult : class
        {
            var (content, respMsg) = await GetDataAsync(url, query, withProxy);
            var tResult = DeserializeData<TResult>(content, respMsg.Content.Headers.ContentType?.MediaType);

            return (tResult, respMsg.StatusCode);
        }

        public (TResult Result, HttpStatusCode StatusCode) PostUrlEncoded<TBody, TResult>
            (string url, TBody body, bool withProxy = false) where TResult : class
        {
            var result = PostDataAsync<TBody, TResult>(url, body, ContentType.UrlEncoded, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }

        public (TResult Result, HttpStatusCode StatusCode) PostJson<TBody, TResult>
            (string url, TBody body, bool withProxy = false) where TResult : class
        {
            var result = PostDataAsync<TBody, TResult>(url, body, ContentType.Json, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }
        
        public (TResult Result, HttpStatusCode StatusCode) PostFormData<TBody, TResult>
            (string url, TBody body, bool withProxy = false) where TResult : class
        {
            var result = PostDataAsync<TBody, TResult>(url, body, ContentType.FormData, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }
        
        public IFlurlHttpHelper WithOAuthBearerToken(string token)
        {
            _authToken = token;

            return this;
        }

        public IFlurlHttpHelper WithHeader(string key, string value)
        {
            _header.TryAdd(key, value);

            if (_header.ContainsKey(key) && _header[key] != value)
                _header[key] = value;

            return this;
        }

        #region Private Methods
        
        private async Task<(TResult Result, HttpStatusCode StatusCode)> PostDataAsync<TBody, TResult>
            (string url, TBody body, string reqContentType, bool withProxy) where TResult : class
        {
            var (content, respMsg) = await PostDataAsync(url, body, reqContentType, withProxy);
            var result = DeserializeData<TResult>(content, respMsg.Content.Headers.ContentType?.MediaType);
            
            return (result, respMsg.StatusCode);
        }

        private static T DeserializeData<T>(string content, string contentType)
        {
            switch (contentType)
            {
                case "application/xml":
                case "text/xml":
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    using (var reader = new StringReader(content))
                        return (T)xmlSerializer.Deserialize(reader);
                default:
                    if (!JsonExtension.IsJson(content))
                        throw new CustomException($"DeserializeData unsupported ContentType: {contentType}");
                    return JsonExtension.DeserializeObject<T>(content);
            }
        }

        private IFlurlRequest CreateFlurlRequest(string url, bool withProxy)
        {
            IFlurlRequest request;

            if (withProxy)
            {
                var hc = _httpHelper.GetClientWithProxy();
                var fc = new FlurlClient(hc);
            
                request = fc.Request(new Uri(url));
            }
            else
            {
                
                request = new FlurlRequest(url);
            }
            
            request = request.AllowAnyHttpStatus();

            if (_authToken != string.Empty)
                request.WithOAuthBearerToken(_authToken);

            return request;
        }
        

        private void ClearAll()
        {
            _header.Clear();
            _authToken = string.Empty;
        }

        private async Task<(string Result, HttpResponseMessage responseMessage)> PostDataAsync<TBody>(string url, TBody data, string postType, bool withProxy)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                
                var request =  CreateFlurlRequest(url, withProxy);
                
                request = _header.Aggregate(request, (current, item)
                    => current.WithHeader(item.Key, item.Value));
                
                _logger.LogInformation($"POST ({postType}): {url}");
                _logger.LogInformation("Headers:" + JsonConvert.SerializeObject(request.Headers));
                _logger.LogInformation(JsonConvert.SerializeObject(data));

                var response = postType switch
                {
                    ContentType.Json => await request.PostJsonAsync(data),
                    ContentType.Text => await request.PostStringAsync(data.ToString()),
                    ContentType.UrlEncoded => await request.PostUrlEncodedAsync(data),
                    ContentType.FormData => await request.PostMultipartAsync(mp => mp.AddStringParts(data)),
                    _ => default(FlurlResponse)
                };

                LogContext.PushProperty("Url", url);
                LogContext.PushProperty("Elapsed", sw.Elapsed.TotalMilliseconds);

                if (response == null)
                    throw new CustomException("response is null");
                
                var content = await response.ResponseMessage.Content.ReadAsStringAsync();

                _logger.LogInformation("Response=" + content);

                return (content, response.ResponseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostDataAsync Exception");
                throw;
            }
            finally
            {
                ClearAll();
            }
        }

        private async Task<(string Content, HttpResponseMessage responseMessage)> GetDataAsync(string url, string query, bool withProxy = false)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var urlWithQuery = $"{url}?{query}";
                
                var request =  CreateFlurlRequest(urlWithQuery, withProxy);
                
                request = _header.Aggregate(request, (current, item)
                    => current.WithHeader(item.Key, item.Value));

                _logger.LogInformation("Headers:" + JsonConvert.SerializeObject(request.Headers));

                var response = request.GetAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
                LogContext.PushProperty("url", urlWithQuery);
                
                var content = await response.ResponseMessage.Content
                    .ReadAsStringAsync();
                
                _logger.LogInformation(content);
                
                return (content, response.ResponseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDataAsync Exception");
                throw;
            }
            finally
            {
                ClearAll();
            }
        }

        #endregion
    }
}
