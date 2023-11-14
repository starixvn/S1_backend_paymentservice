using System.Web;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PattayaPay;

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
        var apiUrl = input["host"];
        var apiKey = input["key"];
        var reqAmount = decimal.Parse(input["Amount"]);
        var strAmount = reqAmount.ToString("#");
        var transId = input["TransID"];
        var bankCode = input["BankCode"];
        var merchantCode = input["merchantCode"];
        var notifyUrl = input["CallBackUrl"];
        var payCode = input["PayCode"];

        try
        {
            var req = new Dictionary<string, string>
            {
                {"amount", strAmount},
                {"merchant", merchantCode},
                {"paytype", payCode},
                {"outtradeno", transId},
                {"notifyurl", notifyUrl},
                {"returndataformat", "serverjson"}
            };

            req.Add("sign", GetSign(req, apiKey));

            var res = CreateOrder(apiUrl, req);

            var response = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = res.Results.TargetUrl,
                ThirdId = res.Results.TradeNo
            };

            return response;
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
        var apiKey = input["key"];
        var req = FromJson<DepositCallBackRequest>(ToJson(formData));

        if (!VerifySign(formData, apiKey, req.sign))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                Comment = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        var transStatus = PattayaPayDefinition.GetDepositTransStatus(req.status);
        var result = new NotifyDepositResult
        {
            ThirdId = req.tradeno,
            DepositId = req.outtradeno,
            Comment = req.status,
            TransStatus = transStatus,
            ActualAmount = req.ramount,
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

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["host"];
        var apiKey = inputs["key"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("#");
        var transId = inputs["TransID"];
        var accountName = inputs["AccountName"];
        var accountNo = inputs["AccountNo"];
        var bankCode = inputs["BankCode"];
        var merchantCode = inputs["merchantCode"];
        var notifyUrl = inputs["CallBackUrl"];

        try
        {
            var req = new Dictionary<string, string>
            {
                {"amount", strAmount},
                {"merchant", merchantCode},
                {"bankname", bankCode},
                {"cardname", accountName},
                {"cardno", accountNo},
                {"outtransferno", transId},
                {"notifyurl", notifyUrl}
            };

            req.Add("sign", GetSign(req, apiKey));

            var res = Payout(apiUrl, req);
            var response = new WithdrawResult
            {
                Success = true,
                RawContent = JsonConvert.SerializeObject(res)
            };

            return response;
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var apiKey = payResource["key"];
        var req = FromJson<WithdrawalCallBackRequest>(ToJson(formData));

        if (!VerifySign(formData, apiKey, req.sign))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                Response = CallbackResponse.Failed
            };
        }

        var transStatus = PattayaPayDefinition.GetPayoutTransStatus(req.status);
        var result = new NotifyWithdrawResult
        {
            ThirdId = req.transferno,
            TransStatus = transStatus,
            Response = CallbackResponse.Success,
            WithdrawId = req.outtransferno
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

    private PattayaPayResponse<CreatOrderResults> CreateOrder(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePay;

        var (result, statusCode) = _httpHelper
            .PostUrlEncoded<Dictionary<string, string>, PattayaPayResponse<CreatOrderResults>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"PattayaPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != 0)
            throw new CustomException($"PattayaPay CreatOrder response error: {result.Code} {result.Results}");

        return result;
    }

    private PattayaPayResponse<string> Payout(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePayout;
        var (result, statusCode) = _httpHelper
            .PostUrlEncoded<Dictionary<string, string>, PattayaPayResponse<string>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"PattayaPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != 0)
            throw new CustomException(
                $"PattayaPay Payout response error: {result.Code} {result.Results}");

        return result;
    }

    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
            .Where(x => !x.Key.Equals("sign"))
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"))
                .ToLower() + $"&secret={key}";

        _logger.LogInformation(str);
        var sign = base.ToMD5(str).ToLower();
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