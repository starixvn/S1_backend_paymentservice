using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PPPay;

public class PayHelper: PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    
    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }
    
    #region Debit
    
    public override DepositResult Debit(Dictionary<string, string> input)
    {
        var baseApiUrl = input["Host"];
        var key = input["Key"];
        var transId = input["TransID"];
    
        try
        {
            var req = new Dictionary<string, string>
            {
                { "merchantCode", input["MerchantCode"] },
                { "merchantOrderId", transId },
                { "serviceId", input["PayCode"] },
                { "applyAmount", input["Amount"] },
                { "applyUserName", input["UserName"] },
                { "callbackUrl", input["CallBackUrl"] }
            };
            
            req.Add("sign", GetSign(req, key));
            
            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, Response<CreateDeposit>>(baseApiUrl + Path.Deposit, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"PPPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"PPPay CreatOrder response error: {result.Code}");

    
            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = result.Data.Url,
                ThirdId = result.Data.ThirdId
            };
        }
        catch (CustomException ex)
        {
            _logger.LogError($"PPPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");
    
            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var key = input["Key"];
        var req = FromJson<DepositCallbackRequest>(bodyContent);
        
        if (!VerifySign(FromJson<Dictionary<string, string>>(bodyContent), key, req.Sign))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                DepositId = req.TransId,
                Comment = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }
        
        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = req.Amount,
            ThirdId = req.TransId,
            Comment = bodyContent,
            TransStatus = Definition.GetDepositTransStatus(req.Status),
            Response = CallbackResponse.Success
        };
    }
    
    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    
    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Credit
    
    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        var baseApiUrl = input["Host"];
        var key = input["Key"];
        var transId = input["TransID"];
    
        try
        {
            var req = new Dictionary<string, string>
            {
                { "merchantCode", input["MerchantCode"] },
                { "merchantOrderId", transId },
                { "serviceId", input["PayCode"] },
                { "applyAmount", input["Amount"] },
                { "applyUserName", input["AccountName"] },
                { "applyAccount", input["AccountNo"] },
                { "applyBankName", input["BankName"] },
                { "applyBankCode", input["BankCode"] },
                { "callbackUrl", input["CallBackUrl"] }
            };
            
            req.Add("sign", GetSign(req, key));
            
            var (response, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, Response<CreateWithdrawal>>($"{baseApiUrl}{Path.Withdrawal}", req);
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PPPay CreatPayout Failed, HttpStatus: {(int)statusCode} {statusCode}");
            }
            
            if (response.Code != Definition.SuccessCode)
                throw new CustomException($"PPPay CreatOrder response error, Code={response.Code}");
            
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = response.Data.ThirdId,
                RawContent = JsonConvert.SerializeObject(response)
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");
    
            return new WithdrawResult
            {
                Success = false,
                WithdrawId = transId,
                Message = ex.Message
            };
        }
    }
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var key = payResource["Key"];
        var req = FromJson<PayoutCallbackRequest>(bodyContent);
    
        if (!VerifySign(FromJson<Dictionary<string, string>>(bodyContent), key, req.Sign))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                WithdrawId = req.TransId,
                Message = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        return new NotifyWithdrawResult
        {
            ThirdId = req.TransId,
            TransStatus = Definition.GetPayoutTransStatus(req.Status),
            Message = bodyContent,
            Response = CallbackResponse.Success,
            WithdrawId = req.TransId
        };
    }
    
    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Private
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
                .Where(x => !x.Key.Equals("sign"))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

        _logger.LogInformation(str);
        var sign = ToMD5(str);
        _logger.LogInformation(sign);
        
        return sign;
    }
    
    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
    {
        var sign = GetSign(param, apiKey);
        return sign.Equals(target, StringComparison.OrdinalIgnoreCase);
    }
    
    #endregion
}