using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PaymentService.Extensions.Middlewares
{
    public class ResponseLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public ResponseLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("ResponseLog");
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path.EndsWith("home.html"))
            {
                await _next(context);
            }
            else
            {
                var bodyStream = context.Response.Body;

                var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var recordContent = new StreamReader(responseBodyStream, Encoding.UTF8, true, 1024, true).ReadToEnd();
                _logger.LogInformation($"RESPONSE: {recordContent}");
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(bodyStream);
            }
        }
    }
}