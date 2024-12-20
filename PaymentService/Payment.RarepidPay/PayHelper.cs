using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.RarepidPay;

public class PayHelper: PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;

    public PayHelper(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
    }

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var url = inputs["Host"] + Definition.Path.Deposit;
        try
        {
            var req = new PaymentRequest
            {
                Name = inputs["AccountName"],
                Amount = int.Parse(inputs["Amount"]),
                TxnId = inputs["TransID"],
                CallbackUrl = inputs["CallBackUrl"]
            };

            var result = CreateOrder(url, req, inputs["ApiKey"]);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = result.Url
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"RarepidPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var depositReceive = FromJson<DepositReceive>(bodyContent);
        var key = payResource["ApiKey"];
        
        if (!VerifySign(depositReceive, key))
        {
            return new NotifyDepositResult
            {
                DepositId = depositReceive.TxnId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        throw new NotImplementedException();
    }

    private bool VerifySign(DepositReceive receive, string apiKey)
    {
        throw new NotImplementedException();
    }

    private PaymentResponse CreateOrder(string url, PaymentRequest req, string apiKey)
    {
        var (result, statusCode) = _httpHelper
            .WithHeader("Authorization", $"APIKEY {apiKey}")
            .PostJson<PaymentRequest, PaymentResponse>(url, req);
            
        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException($"RarepidPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.StatusCode != Definition.SuccessResponseCode)
        {
            throw new CustomException($"RarepidPay deposit failed, StatusCode:{result.StatusCode} Message: {result.Message}");
        }

        return result;
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }
}