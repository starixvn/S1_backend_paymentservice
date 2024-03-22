using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.SDSPay;

public class PayHelper : PayHelperBase, IExchangeHelper, IPayHelperNoCallBackUrl
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private const decimal AmountFix = 10000;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    #region ExchangeRate

    /// <summary>
    /// 這家匯率幣別資訊是綁商戶，多幣別沒法用
    /// </summary>
    public ExchangeResult GetExchange(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
        
        /*var baseApiUrl = input["host"];
        var type = input["ExchangeType"]; // 0: 提款, 1: 存款

        try
        {
            var req = new Dictionary<string, string>
            {
                {"merchantNo", "MerchantNo"},
                {"type", type.Equals("0") ? "Withdrawal" : "deposit"}
            };

            var rateInfo = GetExchangeRate(baseApiUrl, req);

            return new ExchangeResult("0", "Success", new CryptoExchangeInfo
            {
                Rate = rateInfo.Rate,
                ExchangeAmount = 0,
                Timestamp = 0
            });
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

            return new ExchangeResult(ex.Code, ex.Message);
        }*/
    }

    #endregion

    #region Debit

    public override DepositResult Debit(Dictionary<string, string> input)
    {
        var payCode = input["PayCode"];
        var reqAmount = input["Amount"];
        var depositInfo = input["DepositInfo"];

        try
        {
            if (depositInfo == null)
                throw new CustomException("DepositInfo is empty.");
            
            var rateInfo = JsonConvert.DeserializeObject<CryptoRateInfo>(depositInfo);
                
            if (rateInfo == null)
                throw new CustomException("CryptoRateInfo is empty.");
            
            var rate = rateInfo.ExchangeRate.ToString("G29");
            var amount = rateInfo.ExchangeAmount.ToString("F4");
            
            var formData = new Dictionary<string, string>
            {
                { "DepositAmount", reqAmount },
                { "Rate", rate },
                { "RequestAmount", amount },
                { "Token", payCode }
            };
            
            depositInfo = depositInfo.AddOrSetExchangeInfo(rate, amount);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = GetWalletAddress(input),
                FormData = formData,
                ThirdId = string.Empty,
                DepositInfo = depositInfo
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var key = input["Key"];
        var req = FromJson<CallbackReq>(bodyContent);
        var transId = input["TransID"];

        if (!VerifySign(req.Data, key))
        {
            _logger.LogError($"{transId}, signature verification failed.");

            return new NotifyDepositResult
            {
                DepositId = transId,
                TransStatus = TransStatus.Pending,
                Comment = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        var depositInfo = input["DepositInfo"];
        var rateInfo = JsonConvert.DeserializeObject<CryptoRateInfo>(depositInfo);
                
        if (rateInfo == null)
        {
            _logger.LogError($"{transId}, depositInfo has no exchange rate data.");

            return new NotifyDepositResult
            {
                DepositId = transId,
                TransStatus = TransStatus.Pending,
                Comment = "DepositInfo has no exchange rate data.",
                Response = CallbackResponse.Failed
            };
        }

        var requestAmount = decimal.Parse(input["RequestAmount"]);
        var result = new NotifyDepositResult
        {
            ActualAmount = GetActualAmount(requestAmount, rateInfo.ExchangeAmount, decimal.Parse(req.Data["amount"]), rateInfo.ExchangeRate),
            DepositId = transId,
            ThirdId = req.Data["hash"],
            TransStatus = TransStatus.Success,
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
        var transId = inputs["TransID"];
        var withdrawalInfo = inputs["WithdrawalInfo"];

        try
        {
            if (withdrawalInfo == null)
                throw new CustomException("WithdrawalInfo is empty.");
            
            var rateInfo = JsonConvert.DeserializeObject<CryptoRateInfo>(withdrawalInfo);
                
            if (rateInfo == null)
                throw new CustomException("CryptoRateInfo is empty.");

            var rate = rateInfo.TempExchangeRate.ToString("G29");
            var strAmount = rateInfo.TempExchangeAmount.ToString("F4");
            var reqAmount = decimal.Parse(strAmount) * AmountFix;
            
            var withdrawalResp = Payout(inputs, reqAmount.ToString("G29"));

            withdrawalInfo = withdrawalInfo.AddOrSetExchangeInfo(rate, strAmount);

            var response = new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                RawContent = JsonConvert.SerializeObject(withdrawalResp),
                WithdrawalInfo = withdrawalInfo
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

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var key = input["Key"];
        var req = FromJson<CallbackReq>(bodyContent);
        var transId = input["TransId"];

        if (!VerifySign(req.Data, key))
        {
            _logger.LogError($"{transId}, signature verification failed.");

            return new NotifyWithdrawResult
            {
                WithdrawId = transId,
                TransStatus = TransStatus.Pending,
                Message = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        return new NotifyWithdrawResult
        {
            WithdrawId = transId,
            TransStatus = Definition.GetPayoutTransStatus(req.Data["status"]),
            Message = bodyContent,
            Response = CallbackResponse.Success
        };
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion
    
    public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
    {
        var req = FromJson<CallbackReq>(bodyContent);

        return req.Data["loginName"];
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #region Private Methods

    /*private ExchangeRate GetExchangeRate(string baseApiUrl, Dictionary<string, string> input)
    {
        var apiUrl = baseApiUrl + Path.ExchangeRate;
        var q = string.Join("&", input.Select(x => $"{x.Key}={x.Value}"));

        var response = _httpHelper
            .Get<ExchangeRate>(apiUrl, q);

        if (!response.StatusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"GetExchangeRate failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}");
        }

        return response.Result;
    }*/
    
    private string GetWalletAddress(IDictionary<string, string> input)
    {
        var baseApiUrl = input["Host"];
        var payCode = input["PayCode"];
        var merchant = input["MerchantNo"];
        var transId = input["TransID"];
        var key = input["Key"];

        var req = new Dictionary<string, string>
        {
            { "merchantNo", merchant },
            { "applyDate", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "loginName", transId },
            { "userType", "ACCOUNT" },
            { "type", payCode }
        };

        req.Add("sign", GetSign(req, key));

        var (result, statusCode) = _httpHelper
            .WithHeader("accept", ContentType.Json)
            .PostUrlEncoded<IDictionary<string, string>, GetAddressData>(baseApiUrl + Path.Deposit, req,  true);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"GetWalletAddress failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (!result.Code.Equals(Definition.SuccessCode))
            throw new CustomException(
                $"GetWalletAddress response error code: {result.Code}, msg: {result.Message}");

        return result.Address;
    }
    
    private string Payout(IDictionary<string, string> input, string reqAmount)
    {
        var baseApiUrl = input["Host"];
        var payCode = input["PayCode"];
        var merchant = input["MerchantNo"];
        var transId = input["TransID"];
        var key = input["Key"];

        var req = new Dictionary<string, string>
        {
            { "amount", reqAmount },
            { "merchantNo", merchant },
            { "type", payCode },
            { "loginName", input["UserName"] },
            { "address", input["AccountNo"] },
            { "applyDate", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "outTradeno", transId },
            { "notifyUrl", input["CallBackUrl"] }
        };

        req.Add("sign", GetSign(req, key));

        var (result, statusCode) = _httpHelper
            .PostUrlEncoded<IDictionary<string, string>, GetAddressData>(baseApiUrl + Path.Payout, req, true);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (!result.Code.Equals(Definition.SuccessCode))
            throw new CustomException(
                $"Payout response error code: {result.Code}, msg: {result.Message}");

        return result.Address;
    }

    private readonly IEnumerable<string> _exceptKeys = new [] { "sign", "memo" };
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
            .Where(x => !_exceptKeys.Contains(x.Key))
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

        _logger.LogInformation("str: " + str);
        
        var sign = ToMD5(str).ToLower();

        return sign;
    }

    private bool VerifySign(IDictionary<string, string> param, string key)
    {
        var target = param["sign"];
        
        return target.Equals(GetSign(param, key));
    }
    
    private static decimal GetActualAmount(decimal requestAmount,
        decimal requestCryptoAmount,
        decimal receivedCryptoAmount,
        decimal exchangeRate)
    {
        var amount = receivedCryptoAmount / AmountFix;
        
        if (amount == requestCryptoAmount)
            return requestAmount;

        // 使用匯率回推平台金額，保留小數 2 位
        var actualAmount = decimal.Round(amount / exchangeRate, 2);
        return actualAmount;
    }

    #endregion
}