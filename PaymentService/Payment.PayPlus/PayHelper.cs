using System.Security.Cryptography;
using System.Web;
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
            
            if (!resp.Meta.Success && !resp.Meta.Code.Equals("0000"))
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
        /*var publicKey = payResource["PublicKey"];
        var req = FromJson<DepositCallbackReq>(formData["data"]);
        var reqDic = FromJson<Dictionary<string, string>>(formData["data"]);
        var str = GetVerifyStr(reqDic, new []{"sign"});
        
        if (!VerifySign(str, req.Sign, publicKey))
        {
            return new NotifyDepositResult
            {
                DepositId = req.TransId,
                TransStatus = TransStatus.Pending,
                Comment = "Signature does not match.",
                Response = CallbackResponse.Failed
            };
        }
        
        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = req.Amount / AmountFix,
            ThirdId = req.ThirdId,
            Comment = req.Status,
            TransStatus = Definition.GetPayinTransStatus(req.Status),
            Response = CallbackResponse.Success
        };*/
        
        return new NotifyDepositResult
        {
            TransStatus = TransStatus.Pending,
            Response = CallbackResponse.Success
        };
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var transId = inputs["TransID"];
        
        try
        {
            var req = new PayPlusReq<PayoutReq>
            {
                Action = "PaymentCreateOrder",
                ActionValue = new PayoutReq
                {
                    TransId = transId,
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
            
            if (!resp.Meta.Success && !resp.Meta.Code.Equals("0000"))
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                RawContent = response.Data
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
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        /*var publicKey = payResource["PublicKey"];
        var req = FromJson<PayoutCallbackReq>(formData["data"]);
        var reqDic = FromJson<Dictionary<string, string>>(formData["data"]);
        var str = GetVerifyStr(reqDic, new []{"sign"});
        
        if (!VerifySign(str, req.Sign, publicKey))
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
            ThirdId = req.ThirdId,
            TransStatus = Definition.GetPayoutTransStatus(req.Status),
            Message = req.Status,
            Response = CallbackResponse.Success,
            WithdrawId = req.TransId
        };*/
        
        return new NotifyWithdrawResult
        {
            TransStatus = TransStatus.Pending,
            Response = CallbackResponse.Success
        };
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        try
        {
            var req = new PayPlusReq<Dictionary<string, string>>
            {
                Action = "CardQuery",
                ActionValue = new Dictionary<string, string>
                {
                    {"merchantOrderNo", inputs["TransID"]},
                    {"merchantNo", inputs["MerchantNo"]},
                    {"timeStamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()}
                }
            };
            
            var (response, statusCode) = _httpHelper
                .PostJson<PayPlusReq<Dictionary<string, string>>, PayPlusOutResp>(inputs["Host"], req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PayPlus deposit query failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Code.Equals(200))
            {
                throw new CustomException(response.Message);
            }

            var resp = FromJson<PayPlusResp<DepositCallbackReq>>(response.Data);
            
            if (resp == null)
            {
                throw new CustomException("PayPlus deposit query resp is null.");
            }
            
            if (!resp.Meta.Success && !resp.Meta.Code.Equals("0000"))
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new EnquiryDepositResult
            {
                TransStatus = Definition.GetPayinTransStatus(resp.Data.Status),
                DepositId = resp.Data.TransId,
                ThirdId = resp.Data.ThirdId,
                RawContent = response.Data,
                ActualAmount = resp.Data.Amount / AmountFix,
                Message = resp.Meta.Message,
                Comment = resp.Data.Status
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"PayPlus deposit query failed, Code:{ex.Code}, Message:{ex.Message}");

            throw;
        }
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        try
        {
            var req = new PayPlusReq<Dictionary<string, string>>
            {
                Action = "PaymentQueryOrder",
                ActionValue = new Dictionary<string, string>
                {
                    {"merchantOrderNo", inputs["TransID"]},
                    {"merchantNo", inputs["MerchantNo"]},
                    {"timeStamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()}
                }
            };
            
            var (response, statusCode) = _httpHelper
                .PostJson<PayPlusReq<Dictionary<string, string>>, PayPlusOutResp>(inputs["Host"], req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PayPlus payout query failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Code.Equals(200))
            {
                throw new CustomException(response.Message);
            }

            var resp = FromJson<PayPlusResp<PayoutCallbackReq>>(response.Data);
            
            if (resp == null)
            {
                throw new CustomException("PayPlus payout query resp is null.");
            }
            
            if (!resp.Meta.Success && !resp.Meta.Code.Equals("0000"))
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new EnquiryWithdrawResult
            {
                TransStatus = Definition.GetPayoutTransStatus(resp.Data.Status),
                WithdrawId = resp.Data.TransId,
                ThirdId = resp.Data.ThirdId,
                RawContent = response.Data,
                Message = resp.Data.Status
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"PayPlus deposit query failed, Code:{ex.Code}, Message:{ex.Message}");

            throw;
        }
    }
    
    private static string GetVerifyStr(IEnumerable<KeyValuePair<string, string>> param, IEnumerable<string> ignoreKeys)
    {
        var str = string.Join("&", param
            .Where(x => ignoreKeys.All(k => !x.Key.Equals(k, StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrEmpty(x.Value))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={x.Value}"));
            
        return str;
    }
    
    private static bool VerifySign(string str, string targetBase64Str, string publicKey)
    {
        var target = HttpUtility.HtmlDecode(targetBase64Str);
        var result = RsaHelper.RsaVerifyData(str, target, publicKey, HashAlgorithmName.SHA1);
            
        return result;
    }

    private static (string payType, string payModel) ParsePayCode(string payCode)
    {
        var tmp = payCode.Split('-');
        var payModel = tmp.Length > 1 ? tmp[1] : string.Empty;

        return (tmp[0], payModel);
    }
}