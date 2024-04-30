using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Helpers;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PayPlus;

public class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private const decimal AmountFix = 100;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var (payType, payModel) = ParsePayCode(inputs["PayCode"]);
        var reqAmount = decimal.Parse(inputs["Amount"]);

        try
        {
            var req = new PayPlusReq<DepositReq>
            {
                Action = "CardPayMent",
                ActionValue = new DepositReq
                {
                    TransId = inputs["TransID"],
                    PayType = payType,
                    PayModel = payModel,
                    UserName = inputs["UserName"],
                    Currency = inputs["PayCurrencyId"],
                    Amount = (reqAmount * AmountFix).ToString("G29"),
                    CallbackUrl = inputs["CallBackUrl"],
                    MerchantNo = inputs["MerchantNo"]
                }
            };
            
            var (response, statusCode) = _httpHelper
                .PostJson<PayPlusReq<DepositReq>, PayPlusOutResp>(inputs["Host"], req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PayPlus deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Code.Equals(200))
            {
                throw new CustomException(response.Message);
            }

            var resp = FromJson<PayPlusResp<DepositResp>>(response.Data);
            
            if (resp == null)
            {
                throw new CustomException("PayPlus deposit resp is null.");
            }
            
            if (!resp.Meta.Success)
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new DepositResult
            {
                Success = true,
                Url = resp.Data.Url
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var publicKey = payResource["PublicKey"];
        var req = FromJson<PayPlusResp<DepositCallbackReq>>(bodyContent);
        var reqDic = FromJson<PayPlusResp<Dictionary<string, string>>>(bodyContent);
        var str = GetVerifyStr(reqDic.Data, new []{"sign"});
        
        if (!VerifySign(str, req.Data.Sign, publicKey))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                Comment = "Signature does not match.",
                Response = CallbackResponse.Failed
            };
        }
        
        return new NotifyDepositResult
        {
            DepositId = req.Data.TransId,
            ActualAmount = req.Data.Amount / AmountFix,
            ThirdId = req.Data.ThirdId,
            Comment = req.Data.Status,
            TransStatus = Definition.GetPayinTransStatus(req.Data.Status),
            Response = CallbackResponse.Success
        };
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var reqAmount = decimal.Parse(inputs["Amount"]);
        
        try
        {
            var req = new PayPlusReq<PayoutReq>
            {
                Action = "PaymentCreateOrder",
                ActionValue = new PayoutReq
                {
                    TransId = inputs["TransId"],
                    SubjectType = "TO_PRIVATE",
                    Currency = inputs["PayCurrencyId"],
                    Amount = (reqAmount * AmountFix).ToString("G29"),
                    CallbackUrl = inputs["CallBackUrl"],
                    MerchantNo = inputs["MerchantNo"],
                    AccountName = inputs["AccountName"],
                    AccountCode = inputs["AccountNo"]
                }
            };

            var (response, statusCode) = _httpHelper
                .PostJson<PayPlusReq<PayoutReq>, PayPlusOutResp>(inputs["Host"], req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PayPlus withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Code.Equals(200))
            {
                throw new CustomException(response.Message);
            }

            var resp = FromJson<PayPlusResp<object>>(response.Data);
            
            if (resp == null)
            {
                throw new CustomException("PayPlus withdrawal resp is null.");
            }
            
            if (!resp.Meta.Success)
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = inputs["TransId"],
                RawContent = response.Data
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                WithdrawId = inputs["TransId"],
                Message = ex.Message
            };
        }
    }
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var publicKey = payResource["PublicKey"];
        var req = FromJson<PayPlusResp<PayoutCallbackReq>>(bodyContent);
        var reqDic = FromJson<PayPlusResp<Dictionary<string, string>>>(bodyContent);
        var str = GetVerifyStr(reqDic.Data, new []{"sign"});
        
        if (!VerifySign(str, req.Data.Sign, publicKey))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                Message = "Signature does not match.",
                Response = CallbackResponse.Failed
            };
        }
        
        return new NotifyWithdrawResult
        {
            ThirdId = req.Data.ThirdId,
            TransStatus = Definition.GetPayoutTransStatus(req.Data.Status),
            Message = req.Data.Status,
            Response = CallbackResponse.Success,
            WithdrawId = req.Data.TransId
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
    
    private static string GetVerifyStr(IEnumerable<KeyValuePair<string, string>> param, IEnumerable<string> ignoreKeys)
    {
        var str = string.Join("&", param
            .Where(x => ignoreKeys.All(k => !x.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={x.Value}"));
            
        return str;
    }
    
    private static bool VerifySign(string str, string targetBase64Str, string publicKey)
    {
        var result = RsaHelper.RsaVerifyData(str, targetBase64Str, publicKey, HashAlgorithmName.SHA256);
            
        return result;
    }

    private static (string payType, string payModel) ParsePayCode(string payCode)
    {
        var tmp = payCode.Split('-');
        var payModel = tmp.Length > 1 ? tmp[1] : string.Empty;

        return (tmp[0], payModel);
    }
}