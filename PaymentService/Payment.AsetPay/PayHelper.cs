using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.AsetPay;

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
        var transId = input["TransID"];
        var depositInfo = input["DepositInfo"];
    
        try
        {
            var (method, payType) = SplitPayCode(input["PayCode"]);
            var key = input[$"{method}Key"];
            var req = new Dictionary<string, string>
            {
                { "scode", input[$"{method}MerchantId"] },
                { "orderid", transId },
                { "paytype", payType },
                { "amount", GetRequestAmount(input["Amount"]) },
                { "productname", "productname" },
                { "currency", input["PayCurrencyId"] },
                { "userid", input["UserName"] },
                { "accountname", input["AccountName"] },
                { "noticeurl", input["CallBackUrl"] }
            };
            
            req.Add("sign", GetSign(req, key));
    
            depositInfo = depositInfo.AddOrSetJProperty("Method", method);
            
            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                FormData = req,
                Url = $"{baseApiUrl}{Path.Deposit}",
                DepositInfo = depositInfo
            };
        }
        catch (CustomException ex)
        {
            _logger.LogError($"AsetPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");
    
            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var depositInfo = input["DepositInfo"];
        var transId = input["TransID"];
        var req = FromJson<DepositCallbackRequest>(bodyContent);
        var jObj = JObject.Parse(depositInfo);

        if (!jObj.TryGetValue("Method", StringComparison.OrdinalIgnoreCase, out var jMethod))
        {
            _logger.LogInformation($"{transId} DepositInfo has no PayCode information.");

            return new NotifyDepositResult
            {
                DepositId = transId,
                TransStatus = TransStatus.Pending,
                Response = "Failed."
            };
        }
        
        var method = jMethod.ToObject<string>();
        var key = input[$"{method}Key"];
        
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
                { "scode", input["MerchantId"] },
                { "orderid", transId },
                { "money", GetRequestAmount(input["Amount"]) },
                { "bankname", input["BankName"] },
                { "bankno", input["BankCode"] },
                { "accountno", input["AccountNo"] },
                { "accountname", input["AccountName"] },
                { "branchname", "branch" },
                { "branchcode", "branch" },
                { "notifyurl", input["CallBackUrl"] }
            };
            
            req.Add("sign", GetSign(req, key));
            
            var (response, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, WithdrawalResponse>($"{baseApiUrl}{Path.Withdrawal}", req);
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {(int) statusCode}", response.Message);
            }
            
            if (response.Code != Definition.SuccessCode)
                throw new CustomException(
                    $"OFAPay CreatOrder response error: prc={response.Status}, errcode={response.Code}, msg={response.Message}");
            
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = response.Message,
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
            Message = req.Status,
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
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
                .Where(x => !x.Key.Equals("sign"))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

        _logger.LogInformation(str);
        var sign = ToMD5(str).ToUpper();
        _logger.LogInformation(sign);
        
        return sign;
    }
    
    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
    {
        var sign = GetSign(param, apiKey);
        return sign.Equals(target, StringComparison.OrdinalIgnoreCase);
    }
    
    private static (string method, string payType) SplitPayCode(string payCode)
    {
        var result = payCode.Split('_');

        return (result[0], result[1]);
    }
    
    #endregion
}