using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace PaymentService.Extensions.Middlewares
{
    public class ResponseLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public ResponseLogMiddleware(IServiceProvider provider)
        {
            _next = provider.GetRequiredService<RequestDelegate>();
            _logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ResponseLog");
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var bodyStream = context.Response.Body;

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await _next(context);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var recordContent = await new StreamReader(responseBodyStream, Encoding.UTF8, true, 1024, true)
                .ReadToEndAsync();
            using (LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds))
            {
                _logger.LogInformation($"RESPONSE: {recordContent}");
            }

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(bodyStream);
        }
    }
}