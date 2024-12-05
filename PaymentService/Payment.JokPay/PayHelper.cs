using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.JokPay;

public class PayHelper : PayHelperBase
{
    private readonly ILogger<PayHelper> _logger;
    private readonly IFlurlHttpHelper _httpHelper;
    internal string? SignConcatSourceString { get; private set; }

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    #region Debit
    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        try
        {
            var merchantNo = inputs["MerchantNo"];
            var secretKey = inputs["SecretKey"];
            var host = inputs["Host"];
            var time = new DateTimeOffset(DateTime.Now);
            
            var req = new Dictionary<string, string>
            {
                { "version", "V2" },
                { "signType", "MD5" },
                { "merchantNo", merchantNo },
                { "date", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) },
                { "channleType", inputs["PayCode"] },
                { "noticeUrl", inputs["CallBackUrl"] },
                { "orderNo", inputs["TransID"] },
                { "bizAmt", int.Parse(inputs["Amount"]).ToString("F0") }
            };
            req.Add("sign", Sign(req, secretKey));

            var apiUrl = $"{host.TrimEnd('/')}{Definition.Path.Deposit}";
            var res = CreateOrder(apiUrl, req);
            var result = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Message = res.Message,
                Url = res.Detail.PayUrl
            };

            return result;
        }
        catch (CustomException exception)
        {
            _logger.LogInformation($"JokPay deposit failed, Code:{exception.Code}, Message:{exception.Message}");

            return new DepositResult(false, exception.Code, exception.Message);
        }
    }

    private DepositResponse CreateOrder(string apiUrl, Dictionary<string, string> req)
    {
        var response = JokPayRequest<DepositResponse>("CreateOrder", apiUrl, req);

        return response;
    }

    public override NotifyDepositResult CallBackDebit(
        Dictionary<string, string> formData,
        string bodyContent,
        Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var secretKey = payResource["SecretKey"];
        var parameters = FromJson<Dictionary<string, string>>(bodyContent);

        if (!ValidateSign(parameters, parameters["sign"], secretKey))
        {
            return new NotifyDepositResult
            {
                DepositId = parameters["orderNo"],
                TransStatus = TransStatus.Pending,
                Response = "Signature verification failed."
            };
        }

        var transStatus = Definition.GetTransStatus(parameters["status"]);
        var result = new NotifyDepositResult
        {
            DepositId = parameters["orderNo"],
            Comment = parameters["status"],
            TransStatus = transStatus,
            ActualAmount = decimal.Parse(parameters["orderAmt"]),
            Response = Definition.CallbackResponse
        };

        return result;
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Credit
    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        try
        {
            var merchantNo = inputs["MerchantNo"];
            var secretKey = inputs["SecretKey"];
            var host = inputs["Host"];
            var time = new DateTimeOffset(DateTime.Now);

            var req = new Dictionary<string, string>
            {
                { "version", "V2" },
                { "signType", "MD5" },
                { "merchantNo", merchantNo },
                { "date", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) },
                { "channleType", "1" },
                { "orderNo", inputs["TransID"] },
                { "bizAmt", int.Parse(inputs["Amount"]).ToString("F0") },
                { "accName", inputs["AccountName"] },
                { "bankCode", inputs["BankCode"] },
                { "bankBranchName", inputs["BankName"] },
                { "cardNo", inputs["AccountNo"] },
                { "noticeUrl", inputs["CallBackUrl"] }
            };
            req.Add("sign", Sign(req, secretKey));

            var apiUrl = $"{host.TrimEnd('/')}{Definition.Path.Withdrawal}";
            var res = CreatePayout(apiUrl, req);
            var result = new WithdrawResult
            {
                Success = true,
                Message = res.Message
            };

            return result;
        }
        catch (CustomException exception)
        {
            _logger.LogInformation(exception.Message);

            return new WithdrawResult(exception.Message);
        }
    }

    private WithdrawalResponse CreatePayout(string apiUrl, Dictionary<string, string> req)
    {
        var response = JokPayRequest<WithdrawalResponse>("CreatePayout", apiUrl, req);

        return response;
    }

    public override NotifyWithdrawResult CallBackCredit(
        Dictionary<string, string> formData,
        string bodyContent,
        Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var secretKey = payResource["SecretKey"];
        var parameters = FromJson<Dictionary<string, string>>(bodyContent);

        if (!ValidateSign(parameters, parameters["sign"], secretKey))
        {
            return new NotifyWithdrawResult
            {
                WithdrawId = parameters["orderNo"],
                TransStatus = TransStatus.Pending,
                Response = "Signature verification failed."
            };
        }

        var transStatus = Definition.GetTransStatus(parameters["status"]);
        var result = new NotifyWithdrawResult
        {
            WithdrawId = parameters["orderNo"],
            TransStatus = transStatus,
            Response = Definition.CallbackResponse,
            Message = parameters["status"]
        };

        return result;
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    #endregion

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    internal string Sign(Dictionary<string, string> parameters, string secretKey)
    {
        var queryString = GetQueryString(parameters);
        var signConcatString = $"{queryString}{secretKey}";

        SignConcatSourceString = signConcatString;

        var result = ToMD5(signConcatString).ToLower();

        _logger.LogInformation($"SignConcatSourceString:{signConcatString} Sign result:{result}");

        return result;
    }

    internal bool ValidateSign(Dictionary<string,string> parameters, string sign, string secretKey)
    {
        var calculatedSign = Sign(parameters, secretKey);

        return calculatedSign.Equals(sign, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetQueryString(Dictionary<string, string> parameters)
    {
        var sortedParameters = parameters
            .Where(pair => !pair.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            .Where(pair => pair.Value != null && pair.Value.Any())
            .OrderBy(pair => pair.Key);
        var queryString = string.Join("&", sortedParameters
            .Select(param => $"{param.Key}={param.Value}"));
        return queryString;
    }

    private T JokPayRequest<T>(string action, string apiUrl, Dictionary<string,string> req)
        where T : ResponseBase
    {
        var (response, statusCode) = _httpHelper
            .PostJson<Dictionary<string, string>, T>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"JokPay {action} failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (response.Code != Definition.SuccessCode)
        {
            throw new CustomException(
                $"JokPay {action} failed: expected code={Definition.SuccessCode}, actual code={response.Code} msg={response.Message}");
        }

        return response;
    }
}