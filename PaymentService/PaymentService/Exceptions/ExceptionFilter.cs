using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using PaymentService.Service.Model.enums;

namespace PaymentService.Exceptions;

public class ExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ExceptionFilter>>();
    }

    public void OnException(ExceptionContext context)
    {
        var actionPath = $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}";

        var code = HttpStatusCode.InternalServerError;

        var exception = context.Exception;
        var result = JsonConvert.SerializeObject(new
        {
            ErrorCode = ErrorCode.SystemError,
            Message = ErrorCode.SystemError.ToString(),
            RawErrorCode = exception.HResult.ToString(),
            RawMessage = exception.Message,
            exception.StackTrace
        });

        if (context.Exception.GetType() == typeof(PaymentException))
        {
            code = HttpStatusCode.OK;

            var paymentException = context.Exception as PaymentException;
            var message = paymentException.Message;

            result = JsonConvert.SerializeObject(new
            {
                paymentException.Code,
                Message = message
            });
            _logger.LogInformation(exception, message);
        }
        else
        {
            _logger.LogError(context.Exception, actionPath);
        }

        var httpContext = context.HttpContext;

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)code;
        httpContext.Response.WriteAsync(result);

        context.ExceptionHandled = true;

    }
}