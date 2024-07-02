using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.KzPay;


public class PayHelper : PayHelperBase
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
        var apiHost = input["Host"];
        var key = input["Key"];

        try
        {
            var req = new Dictionary<string, string>
            {
                {"mchNo", input["MerchantNo"]},
                {"mchMemberNo", input["UserName"]},
                {"mchOrderNo", input["TransID"]},
                {"paymentMethod", input["PayCode"]},
                //{"bankCode", input["BankCode"]},
                {"amount", GetRequestAmount(input["Amount"])},
                {"changePrice", "1"}, // 允許改變金額
                {"notifyUrl", input["CallBackUrl"]},
                {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
            };

            req.Add("sign", GetSign(req, key));

            var res = CreateOrder(apiHost, req);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = res.Url,
                ThirdId = res.ThirdId
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
        var apiKey = input["Key"];
        var req = FromJson<CallbackRequest>(bodyContent);

        if (!VerifySign(FromJson<Dictionary<string, string>>(bodyContent), apiKey, req.Sign))
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
            TransStatus = Definition.GetTransStatus(req.Status),
            ActualAmount = decimal.Parse(req.Amount),
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
        var apiHost = input["Host"];
        var key = input["Key"];
        var transId = input["TransID"];

        try
        {
            var req = new Dictionary<string, string>
            {
                {"mchNo", input["MerchantNo"]},
                {"mchMemberNo", input["UserName"]},
                {"mchOrderNo", transId},
                {"paymentMethod", input["PayCode"]},
                {"bankCode", input["BankCode"]},
                {"customerName", input["AccountName"]},
                {"accountNum", input["AccountNo"]},
                {"extra", string.Empty},
                {"amount", GetRequestAmount(input["Amount"])},
                {"notifyUrl", input["CallBackUrl"]},
                {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
            };

            req.Add("sign", GetSign(req, key));

            var res = Payout(apiHost, req);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                RawContent = JsonConvert.SerializeObject(res)
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
        var apiKey = payResource["Key"];
        var req = FromJson<CallbackRequest>(bodyContent);

        if (!VerifySign(FromJson<Dictionary<string, string>>(bodyContent), apiKey, req.Sign))
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
            TransStatus = Definition.GetTransStatus(req.Status),
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

    
    private static string GetRequestAmount(string strAmount)
        => decimal.Parse(strAmount).ToString("F2");
    
    private CreateDeposit CreateOrder(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePay;

        var (result, statusCode) = _httpHelper
            .WithHeader("version", "1")
            .PostJson<Dictionary<string, string>, KzPayResponse<CreateDeposit>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"KzPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != Definition.SuccessCode)
            throw new CustomException($"KzPay CreatOrder response error: {result.Code} {result.Message}");

        return result.Data;
    }

    private KzPayResponse<string> Payout(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePayout;
        var (result, statusCode) = _httpHelper
            .WithHeader("version", "1")
            .PostJson<Dictionary<string, string>, KzPayResponse<string>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"KzPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != Definition.SuccessCode)
            throw new CustomException(
                $"KzPay Payout response error: {result.Code} {result.Message}");

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