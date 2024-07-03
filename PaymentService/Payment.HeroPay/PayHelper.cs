using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.HeroPay;


public class PayHelper : PayHelperBase
{
    private const decimal AmountFix = 100;
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
        var apiHost = input["DepositHost"];
        var key = input["Key"];

        try
        {
            var req = new Dictionary<string, string>
            {
                {"mchId", input["MerchantId"]},
                {"appId", input["AppId"]},
                {"productId", input["PayCode"]},
                {"mchOrderNo", input["TransID"]},
                {"currency", input["PayCurrencyId"]},
                {"amount", GetRequestAmount(input["Amount"]).ToString("F0")},
                {"clientIp", input["ClientIP"]},
                {"notifyUrl", input["CallBackUrl"]},
                {"subject", "subject"},
                {"body", "body"},
            };

            req.Add("sign", GetSign(req, key));

            var res = CreateOrder(apiHost, req);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = res.Data.Url,
                ThirdId = res.DepositThirdId
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var key = input["Key"];
        var req = FromJson<DepositCallbackRequest>(bodyContent);

        if (!VerifySign(formData, key, req.Sign))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                DepositId = req.TransId,
                Comment = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        var result = new NotifyDepositResult
        {
            ThirdId = req.ThirdId,
            DepositId = req.TransId,
            Comment = bodyContent,
            TransStatus = Definition.GetDepositTransStatus(req.Status),
            ActualAmount = decimal.Parse(req.Amount) / AmountFix,
            Response = CallbackResponse.Success
        };

        return result;
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Credit

    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        var apiHost = input["PayoutHost"];
        var key = input["Key"];
        var transId = input["TransID"];

        try
        {
            var req = new Dictionary<string, string>
            {
                {"mchId", input["MerchantId"]},
                {"appId", input["AppId"]},
                {"mchTransOrderNo", transId},
                {"currency", input["PayCurrencyId"]},
                {"amount", GetRequestAmount(input["Amount"]).ToString("F0")},
                {"notifyUrl", input["CallBackUrl"]},
                {"bankCode", input["BankCode"]},
                {"bankName", input["BankName"]},
                {"accountType", input["PayCode"]},
                {"accountName", input["AccountName"]},
                {"accountNo", input["AccountNo"]},
                {"province", "province"},
                {"city", "city"},
                {"param2", input["PayKey"]}
            };

            req.Add("sign", GetSign(req, key));

            var res = Payout(apiHost, req);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                RawContent = ToJson(res.Data)
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

        if (!VerifySign(formData, key, req.Sign))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                Response = CallbackResponse.Failed,
                WithdrawId = req.TransId
            };
        }

        var result = new NotifyWithdrawResult
        {
            ThirdId = req.ThirdId,
            TransStatus = Definition.GetPayoutTransStatus(req.Status),
            Response = CallbackResponse.Success,
            WithdrawId = req.TransId
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

    #region Private Methods

    
    private static decimal GetRequestAmount(string strAmount)
        => decimal.Parse(strAmount) * AmountFix;
    
    private HeroPayResponse<CreateDeposit> CreateOrder(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePay;

        var (result, statusCode) = _httpHelper
            .PostUrlEncoded<object, HeroPayResponse<CreateDeposit>>(apiUrl, new
            {
                @params = ToJson(req)
            });

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"HeroPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != Definition.SuccessCode)
            throw new CustomException($"HeroPay CreatOrder response error: {result.Code} {result.Message}");

        return result;
    }

    private HeroPayResponse<object> Payout(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePayout;
        var (result, statusCode) = _httpHelper
            .PostUrlEncoded<object, HeroPayResponse<object>>(apiUrl, new
            {
                @params = ToJson(req)
            });

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"HeroPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != Definition.SuccessCode)
            throw new CustomException(
                $"HeroPay Payout response error: {result.Code} {result.Message}");

        return result;
    }

    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
            .Where(x => !x.Key.Equals("sign") && !string.IsNullOrEmpty(x.Value))
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
        return sign.Equals(target);
    }

    #endregion
}