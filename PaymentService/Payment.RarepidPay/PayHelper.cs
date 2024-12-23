using System.Security.Cryptography;
using System.Text;
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
    internal static string? SignSourceString;
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
            var req = new DepositRequest
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
        var receive = FromJson<PaymentReceive>(bodyContent);
        var key = payResource["ApiKey"];
        
        if (!VerifySign(receive, key))
        {
            return new NotifyDepositResult
            {
                DepositId = receive.TxnId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        var depositResult = new NotifyDepositResult
        {
            DepositId = receive.TxnId,
            ActualAmount = decimal.Parse(receive.Amount),
            ThirdId = receive.Token,
            TransStatus = Definition.GetTransStatus(receive.Status),
            Comment = receive.Status,
            Response = Definition.CallBackResponse
        };
        
        return depositResult;
    }

    internal bool VerifySign(PaymentReceive receive, string apiKey)
    {
        SignSourceString = $"{receive.TxnId}{receive.Amount}{receive.Status}{apiKey}";
        var sign = HashSha256(SignSourceString);
        return receive.Signature.Equals(sign, StringComparison.OrdinalIgnoreCase);
    }
    
    private static string HashSha256(string sourceString)
    {
        string hash;
        using (var sha256Hash = SHA256.Create())
        {
            var sourceBytes = Encoding.UTF8.GetBytes(sourceString);
            var hashBytes = sha256Hash.ComputeHash(sourceBytes);
            hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
        }
        return hash;
    }

    private DepositResponse CreateOrder(string url, DepositRequest req, string apiKey)
    {
        var (result, statusCode) = _httpHelper
            .WithHeader("Authorization", $"APIKEY {apiKey}")
            .PostJson<DepositRequest, DepositResponse>(url, req);
            
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
        var url = inputs["Host"] + Definition.Path.Withdrawal;
        var transId = inputs["TransID"];
        try
        {
            var req = new WithdrawalRequest
            {
                Name = inputs["AccountName"],
                BankCode = inputs["BankCode"],
                Receiver = inputs["AccountName"],
                Account = inputs["AccountNo"],
                Ifsc = inputs["IFSCCode"],
                Amount = int.Parse(inputs["Amount"]),
                TxnId = transId,
                CallbackUrl = inputs["CallBackUrl"]
            };
        
            var result = PayOut(url, req, inputs["ApiKey"]);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = result.Token
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"RarepidPay withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                WithdrawId = transId,
                Message = ex.Message
            };
        }
    }

    private WithdrawalResponse PayOut(string url, WithdrawalRequest req, string apiKey)
    {
        var (result, statusCode) = _httpHelper
            .WithHeader("Authorization", $"APIKEY {apiKey}")
            .PostJson<WithdrawalRequest, WithdrawalResponse>(url, req);
        
        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException($"RarepidPay withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.StatusCode != Definition.SuccessResponseCode)
        {
            throw new CustomException($"RarepidPay withdrawal failed, StatusCode:{result.StatusCode} Message: {result.Message}");
        }

        return result;
    }

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var receive = FromJson<PaymentReceive>(bodyContent);
        var key = payResource["ApiKey"];
        var transId = payResource["TransId"];
        
        if (!VerifySign(receive, key))
        {
            return new NotifyWithdrawResult
            {
                WithdrawId = transId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        return new NotifyWithdrawResult
        {
            ThirdId = receive.Token,
            TransStatus = Definition.GetTransStatus(receive.Status),
            Response = Definition.CallBackResponse,
            WithdrawId = transId,
            Message = $"StatusCode={receive.StatusCode}, Message={receive.Message}, Status={receive.Status}"
        };
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


}