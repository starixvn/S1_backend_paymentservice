using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.WPay;

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
        var reqAmount = input["Amount"];
        var transId = input["TransID"];
        var bankName = input["BankName"];

        try
        {
            var now = DateTimeOffset.UtcNow;
            var req = new CreatOrderRequest
            {
                MerchantNo = input["merchantNo"],
                TransId = transId,
                UserName = input["UserName"],
                ChannelNo = input["PayCode"],
                Amount = reqAmount,
                BankName = bankName,
                DateTime = $"{now: yyyy-MM-dd HH:mm:ss}",
                NotifyUrl = input["CallBackUrl"],
                Timestamp = now.ToUnixTimeSeconds(),
                AppSecret = input["appSecret"]
            };
            
            var res = CreateOrder(apiUrl, req);


            var response = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = res.TargetUrl,
                ThirdId = res.TargetUrl
            };

            return response;
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> input)
    {
        var apiKey = input["key"];
        var req = FromJson<DepositCallBackRequest>(bodyContent);
        var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

        if (!VerifySign(dicReq, apiKey, OrderTypeEnum.Payin, req.sign))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                Comment = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        var transId = input["TransID"];
        var transStatus = WPayDefinition.GetDepositTransStatus(req.status);
        var result = new NotifyDepositResult
        {
            ThirdId = req.tradeNo,
            Comment = req.status,
            TransStatus = transStatus,
            ActualAmount = req.paid,
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
        var reqAmount = inputs["Amount"];
        var transId = inputs["TransID"];

        try
        {
            var now = DateTimeOffset.UtcNow;
            var payoutReq = new Dictionary<string, string>
            {
                { "merchantNo", inputs["merchantNo"] },
                { "orderNo", transId },
                { "amount", reqAmount },
                { "name", inputs["AccountName"] },
                { "bankName", inputs["BankName"] },
                { "bankAccount", inputs["AccountNo"] },
                { "datetime", $"{now: yyyy-MM-dd HH:mm:ss}" },
                { "notifyUrl", inputs["CallBackUrl"] },
                { "time", now.ToUnixTimeSeconds().ToString() },
                { "appSecret", inputs["appSecret"] }
            };

            payoutReq.Add("sign", GetSign(payoutReq, apiKey, OrderTypeEnum.Payout));
           
            var res = Payout(apiUrl, payoutReq);
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

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
    {
        var apiKey = payResource["key"];
        var req = FromJson<WithdrawalCallBackRequest>(bodyContent);
        var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

        if (!VerifySign(dicReq, apiKey, OrderTypeEnum.Payout, req.sign))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                Response = CallbackResponse.Failed
            };
        }

        var transStatus = WPayDefinition.GetPayoutTransStatus(req.status);
        var result = new NotifyWithdrawResult
        {
            ThirdId = req.tradeNo,
            TransStatus = transStatus,
            Response = CallbackResponse.Success
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
    
    private CreatOrderResponse CreateOrder(string baseApiUrl, CreatOrderRequest req)
    {
        var apiUrl = baseApiUrl + Path.CreateOrder;

        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostJson<CreatOrderRequest, CreatOrderResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"WPay GetReceptionToken failed, HttpStatus: {(int)statusCode} {statusCode}" +
                $",errorMsg: {result.ErrorMessage}");
        }
        
        if (result.Code != 0)
            throw new CustomException($"WPay GetReceptionToken response error message: {result.ErrorMessage}");

        return result;
    }

    private CreatPayoutResponse Payout(string apiUrl, Dictionary<string, string> req)
    {
        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostJson<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"WPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}" +
                $", error code: {result.Code} {result.ErrorMessage}");
        }

        if (result.Code != 0)
            throw new CustomException(
                $"WPay Payout response error: {result.Code} {result.ErrorMessage}");

        return result;
    }

    private static readonly Dictionary<OrderTypeEnum, IEnumerable<string>> _ignoreKeys =
        new ()
        {
            {OrderTypeEnum.Payin, new []
            {
                "userName", "channelNo", "amountBeforeFixed", "payeeName", "appSecret", "bankName", "sign"
            }},
            {OrderTypeEnum.Payout, new[]
            {
                "bankBranch", "memo", "VerifyChannelNo", "appSecret", "sign"
            }}
        };
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key, OrderTypeEnum type)
    {
        var str = string.Join("&amp;", param
            .Where(x => _ignoreKeys[type].All(k => !x.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={x.Value}")) + $"&amp;key={key}";

        var sign = base.ToMD5(base.ToSHA256(str))
            .ToUpper();

        return sign;
    }

    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, OrderTypeEnum type, string target)
    {
        var sign = GetSign(param, apiKey, type);
        return sign.Equals(target, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}