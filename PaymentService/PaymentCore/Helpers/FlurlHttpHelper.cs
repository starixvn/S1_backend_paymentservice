using System.Diagnostics;
using System.Net;
using System.Xml.Linq;

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
        private readonly JsonNetSerializer _serializer;
        private readonly ILogger<FlurlHttpHelper> _logger;
        private string _authToken = string.Empty;

        public FlurlHttpHelper(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<FlurlHttpHelper>>();
            _serializer = serviceProvider.GetRequiredService<JsonNetSerializer>();
            _header = new Dictionary<string, string>();
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>(string url, string query)
            where TResult : class
        {
            var tResult = default(TResult);
            var result = await GetDataAsync(url, query);

            if (result.StatusCode == HttpStatusCode.OK)
                tResult = _serializer.DeserializeFormStream<TResult>(result.ContentStream);

            return (tResult, result.StatusCode);
        }

        public (TResult Result, HttpStatusCode StatusCode) Post<TBody, TResult>(string url, TBody body)
            where TResult : class
        {
            var returnObj = PostData(url, body, ContentType.Text);

            if (!JsonExtension.IsJson(returnObj.Result))
            {
                throw new CustomException("Invalid JSON string, Cannot DeserializeObject.");
            }

            var result = JsonConvert.DeserializeObject<TResult>(returnObj.Result);

            return (result, returnObj.StatusCode);
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> PostAsync<TBody, TResult>(string url, TBody body)
            where TResult : class
        {
            try
            {
                var returnObj = await PostDataAsync(url, body, ContentType.Text);
                var result = _serializer.DeserializeFormStream<TResult>(returnObj.Result);

                return (result, returnObj.StatusCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> PostUrlEncodedAsync<TBody, TResult>(string url, TBody body)
        {
            var (stream, statusCode) = await PostDataAsync(url, body, ContentType.UrlEncoded);
            var result = _serializer.DeserializeFormStream<TResult>(stream);

            return (result, statusCode);
        }

        public (TResult Result, HttpStatusCode StatusCode) PostUrlEncoded<TBody, TResult>(string url, TBody body)
        {
            var (stream, statusCode) = PostData(url, body, ContentType.UrlEncoded);
            var result = JsonConvert.DeserializeObject<TResult>(stream, new JsonSerializerSettings
            {
                FloatParseHandling = FloatParseHandling.Decimal
            });

            return (result, statusCode);
        }

        public (TResult Result, HttpStatusCode StatusCode) Post<TBody, TResult>(string url, TBody body, Func<XDocument, TResult> selecor)
            where TResult : class
        {
            var returnObj = PostData(url, body, ContentType.Text);
            var xml = XDocument.Parse(returnObj.Result);
            var result = selecor(xml);

            return (result, returnObj.StatusCode);
        }

        public (TResult Result, HttpStatusCode StatusCode) PostJson<TBody, TResult>(string url, TBody data)
            where TResult : class
        {
            var (json, statusCode) = PostData(url, data, ContentType.Json);

            if (!JsonExtension.IsJson(json))
            {
                throw new CustomException("Invalid JSON string, Cannot DeserializeObject.");
            }

            var result = JsonConvert.DeserializeObject<TResult>(json);
            return (result, StatusCode: statusCode);
        }

        public async Task<(TResult Result, HttpStatusCode StatusCode)> PostJsonAsync<TBody, TResult>(string url, TBody data)
            where TResult : class
        {
            LogContext.PushProperty("url", url);
            _logger.LogInformation(_serializer.SerializeToString(data));
            var returnObj = await PostDataAsync(url, data, ContentType.Json);
            var result = _serializer.DeserializeFormStream<TResult>(returnObj.Result);
            return (result, returnObj.StatusCode);
        }


        public IFlurlHttpHelper WithOAuthBearerToken(string token)
        {
            _authToken = token;

            return this;
        }

        public IFlurlHttpHelper WithHeader(string key, string value)
        {
            if (!_header.ContainsKey(key))
                _header.Add(key, value);

            if (_header.ContainsKey(key) && _header[key] != value)
                _header[key] = value;

            return this;
        }

        #region Private Methods



        #endregion

        private FlurlRequest CreateFlurlRequest(string url)
        {
            var request = new FlurlRequest(url)
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


        private (string Result, HttpStatusCode StatusCode) PostData<TBody>(string url, TBody data, string postType)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var request = CreateFlurlRequest(url);
                request = _header.Aggregate(request, (current, item)
                    => current.WithHeader(item.Key, item.Value));
                _logger.LogInformation($"POST ({postType}): {url}");
                _logger.LogInformation("Headers:"
                                       + JsonConvert.SerializeObject(
                                           request.Headers.ToDictionary(x => x.Name, x => x.Value)));
                _logger.LogInformation(JsonConvert.SerializeObject(data));

                IFlurlResponse response = default(FlurlResponse);
                switch (postType)
                {
                    case ContentType.Json:
                        response = request
                            .PostJsonAsync(data)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
                        break;
                    case ContentType.Text:
                        {
                            var content = data.ToString();
                            response = request
                                .PostStringAsync(content)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();
                            break;
                        }
                    case ContentType.UrlEncoded:
                        response = request
                            .PostUrlEncodedAsync(data)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
                        break;
                }


                LogContext.PushProperty("Url", url);
                LogContext.PushProperty("Elapsed", sw.Elapsed.TotalMilliseconds);

                var result = response.ResponseMessage.Content
                    .ReadAsStringAsync()
                    .GetAwaiter()
                    .GetResult();

                _logger.LogInformation(result);
                ClearAll();

                return (Result: result, response.ResponseMessage.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("PostData Exception", ex);
                throw;
            }
            finally
            {
                ClearAll();
            }
        }

        private async Task<(Stream Result, HttpStatusCode StatusCode)> PostDataAsync<TBody>(string url, TBody data, string postType)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var request = CreateFlurlRequest(url);
                request = _header.Aggregate(request, (current, item) => current.WithHeader(item.Key, item.Value));

                _logger.LogInformation($"POST ({postType}): {url}");
                _logger.LogInformation("Headers:"
                                       + JsonConvert.SerializeObject(request.Headers.ToDictionary(x => x.Name, x => x.Value)));
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

                _logger.LogInformation(await response.ResponseMessage.Content.ReadAsStringAsync());

                var result = await response.ResponseMessage.Content.ReadAsStreamAsync();
                ClearAll();

                return (Result: result, response.ResponseMessage.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("PostData Exception", ex);
                throw;
            }
            finally
            {
                ClearAll();
            }
        }

        private async Task<(Stream ContentStream, HttpStatusCode StatusCode)> GetDataAsync(string url, string query)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var urlWithQuery = $"{url}?{query}";
                var request = CreateFlurlRequest(urlWithQuery);
                request = _header.Aggregate(request, (current, item)
                    => current.WithHeader(item.Key, item.Value));

                _logger.LogInformation(
                    $"header, Keys: {string.Join(",", request.Headers.ToList())}, Values: {string.Join(",", request.Headers.ToList())}");

                var response = await request.GetAsync();

                LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
                LogContext.PushProperty("url", urlWithQuery);
                _logger.LogInformation(await response.ResponseMessage.Content.ReadAsStringAsync());
                var stream = response.ResponseMessage.Content.ReadAsStreamAsync();
                ClearAll();
                return (await stream, response.ResponseMessage.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("PostData Exception", ex);
                throw;
            }
            finally
            {
                ClearAll();
            }
        }
    }
}
