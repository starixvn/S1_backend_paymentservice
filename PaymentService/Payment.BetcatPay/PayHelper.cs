using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.BetcatPay;

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
        var key = input["DepositKey"];
        var appId = input["DepositAppId"];
        var transId = input["TransID"];
    
        try
        {
            var req = new Dictionary<string, string>
            {
                { "appId", appId },
                { "merOrderNo", transId },
                { "currency", input["PayCurrencyId"] },
                { "amount", GetRequestAmount(input["Amount"]) },
                { "notifyUrl", input["CallBackUrl"] }
            };
            
            req.Add("sign", GetSign(req, key));
            
            var (response, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, DepositResponse>($"{baseApiUrl}{Path.Deposit}", req);
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {statusCode}", ((int) statusCode).ToString());
            }
    
            if (response.Code != Definition.SuccessCode)
            {
                throw new CustomException(response.Message, response.Code);
            }
    
            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = response.Data.Params.Url,
                ThirdId = response.Data.ThirdId
            };
        }
        catch (CustomException ex)
        {
            _logger.LogError($"BetcatPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");
    
            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var key = input["DepositKey"];
        var req = FromJson<CallbackRequest>(bodyContent);
        var dic = FromJson<Dictionary<string, string>>(bodyContent);
    
        if (!VerifySign(dic, key, req.Sign))
        {
            return new NotifyDepositResult
            {
                DepositId = req.TransId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }
    
        var transStatus = Definition.GetTransStatus(req.Status);
    
        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = req.Amount,
            ThirdId = req.TransId,
            Comment = req.Message,
            TransStatus = transStatus,
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
        var key = input["PayoutKey"];
        var appId = input["PayoutAppId"];
        var transId = input["TransID"];
        var reqAmount = GetRequestAmount(input["Amount"]);
    
        try
        {
            var req = new WithdrawalRequest
            {
                AppId = appId,
                RequestAmount = reqAmount,
                Currency = input["PayCurrencyId"],
                TransId = transId,
                CallBackUrl = input["CallBackUrl"],
                Data = new WithdrawalRequestData
                {
                    BankCode = input["BankCode"],
                    AccountName = input["AccountName"],
                    AccountNo = input["AccountNo"]
                }
            };
            
            var dic4Sign = new Dictionary<string, string>
            {
                { "appId", appId },
                { "merOrderNo", transId },
                { "currency", input["PayCurrencyId"] },
                { "amount", GetRequestAmount(input["Amount"]) },
                { "notifyUrl", input["CallBackUrl"] },
                { "extra", $"accountName={req.Data.AccountName}&accountNo={req.Data.AccountNo}&bankCode={req.Data.BankCode}" }
            };
            
            req.Sign = GetSign(dic4Sign, key);
    
            var (response, statusCode) = _httpHelper
                .PostJson<WithdrawalRequest, WithdrawalResponse>($"{baseApiUrl}{Path.Withdrawal}", req);
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {statusCode}", ((int) statusCode).ToString());
            }
    
            if (response.Code != Definition.SuccessCode)
            {
                throw new CustomException(response.Message, response.Code);
            }
    
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
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
        var key = payResource["PayoutKey"];
        var req = FromJson<CallbackRequest>(bodyContent);
        var dic = FromJson<Dictionary<string, string>>(bodyContent);
    
        if (!VerifySign(dic, key, req.Sign))
        {
            return new NotifyWithdrawResult
            {
                ThirdId = req.ThirdId,
                WithdrawId = req.TransId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }
    
        var transStatus = Definition.GetTransStatus(req.Status);

        return new NotifyWithdrawResult
        {
            ThirdId = req.TransId,
            TransStatus = transStatus,
            Message = req.Message,
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

    private static string GetRequestAmount(string strAmount)
        => decimal.Parse(strAmount).ToString("F2");
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
    {
        var str = string.Join('&', input
            .Where(x => !x.Key.Equals("sign"))
            .OrderBy(x => key)
            .Select(x => $"{x.Key}={x.Value}") + "&key=" + key);
    
        _logger.LogInformation(str);
        
        var sign = ToSHA256(str);
    
        return sign;
    }
    
    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string key, string target)
    {
        var sign = GetSign(param, key);
        var result = sign.Equals(target);

        if (!result)
        {
            _logger.LogInformation($"Signature dose not match, result={sign}, target sign={target}");
        }
        
        return result;
    }
    
    #endregion
}