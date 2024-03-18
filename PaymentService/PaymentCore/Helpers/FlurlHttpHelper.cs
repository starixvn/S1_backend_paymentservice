using System.Diagnostics;
using System.Net;
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
        
        public (TResult Result, HttpStatusCode StatusCode) Get<TResult>(string url, string query, bool withProxy = false)
            where TResult : class
        {
            var result = GetAsync<TResult>(url, query, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>(string url, string query, bool withProxy = false)
            where TResult : class
        {
            var tResult = default(TResult);
            var result = await GetDataAsync(url, query, withProxy);

            if (result.StatusCode == HttpStatusCode.OK)
                tResult = JsonExtension.DeserializeObject<TResult>(result.Content);

            return (tResult, result.StatusCode);
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> PostUrlEncodedAsync<TBody, TResult>(
            string url, TBody body, bool withProxy = false)
        {
            var (str, statusCode) = await PostDataAsync(url, body, ContentType.UrlEncoded, withProxy);
            
            if (!JsonExtension.IsJson(str))
            {
                throw new CustomException("Invalid JSON string, Cannot DeserializeObject.");
            }
            
            var result = JsonExtension.DeserializeObject<TResult>(str);
            
            return (result, statusCode);
        }

        public (TResult Result, HttpStatusCode StatusCode) PostUrlEncoded<TBody, TResult>(string url, TBody body, bool withProxy = false)
        {
            var result = PostUrlEncodedAsync<TBody, TResult>(url, body, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }

        public (TResult Result, HttpStatusCode StatusCode) PostJson<TBody, TResult>(string url, TBody body, bool withProxy = false)
            where TResult : class
        {
            var result = PostJsonAsync<TBody, TResult>(url, body, withProxy)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            return result;
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> PostJsonAsync<TBody, TResult>(string url, TBody data, bool withProxy = false)
            where TResult : class
        {
            var (json, statusCode) = await PostDataAsync(url, data, ContentType.Json, withProxy);
            
            if (!JsonExtension.IsJson(json))
            {
                throw new CustomException("Invalid JSON string, Cannot DeserializeObject.");
            }
            
            var result = JsonExtension.DeserializeObject<TResult>(json);
            
            return (result, statusCode);
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

        private IFlurlRequest CreateFlurlRequest(string url)
        {
            var request = new FlurlRequest(url)
                .AllowAnyHttpStatus();

            if (_authToken != string.Empty)
                request.WithOAuthBearerToken(_authToken);

            return request;
        }
        
        private IFlurlRequest CreateFlurlRequestWithProxy(string url)
        {
            var hc = _httpHelper.GetClientWithProxy();
            var fc = new FlurlClient(hc);
            fc.BaseUrl = url;
            
            var request = fc.Request()
                .AllowAnyHttpStatus();
            
            if (_authToken != string.Empty)
                request.WithOAuthBearerToken(_authToken);
            
            return request;
        }

        private void ClearAll()
        {
            _header.Clear();
            _authToken = string.Empty;
        }

        private async Task<(string Result, HttpStatusCode StatusCode)> PostDataAsync<TBody>(string url, TBody data, string postType, bool withProxy)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var request = CreateFlurlRequest(url);
                request = _header.Aggregate(request, (current, item)
                    => current.WithHeader(item.Key, item.Value));
                
                _logger.LogInformation($"POST ({postType}): {url}");
                _logger.LogInformation("Headers:" + JsonConvert.SerializeObject(request.Headers));
                _logger.LogInformation(JsonConvert.SerializeObject(data));

                IFlurlResponse response = default(FlurlResponse);
                switch (postType)
                {
                    case ContentType.Json:
                        response = await request.PostJsonAsync(data);
                        break;
                    case ContentType.Text:
                        {
                            var content = data.ToString();
                            response = await request.PostStringAsync(content);
                            break;
                        }
                    case ContentType.UrlEncoded:
                        response = await request.PostUrlEncodedAsync(data);
                        break;
                }

                LogContext.PushProperty("Url", url);
                LogContext.PushProperty("Elapsed", sw.Elapsed.TotalMilliseconds);

                var result = await response.ResponseMessage.Content.ReadAsStringAsync();

                _logger.LogInformation(result);

                return (result, response.ResponseMessage.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostDataAsync Exception"
                    );
                throw;
            }
            finally
            {
                ClearAll();
            }
        }

        private async Task<(string Content, HttpStatusCode StatusCode)> GetDataAsync(string url, string query, bool withProxy = false)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var urlWithQuery = $"{url}?{query}";
                
                var request = withProxy
                    ? CreateFlurlRequestWithProxy(urlWithQuery)
                    : CreateFlurlRequest(urlWithQuery);
                
                request = _header.Aggregate(request, (current, item)
                    => current.WithHeader(item.Key, item.Value));

                _logger.LogInformation("Headers:" + JsonConvert.SerializeObject(request.Headers));

                var response = request.GetAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
                LogContext.PushProperty("url", urlWithQuery);
                
                var result = await response.ResponseMessage.Content
                    .ReadAsStringAsync();
                
                _logger.LogInformation(result);
                
                return (result, response.ResponseMessage.StatusCode);
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
