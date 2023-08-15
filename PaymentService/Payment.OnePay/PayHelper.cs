using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Tsp;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using Serilog.Core;
using System.Web;

namespace Payment.OnePay;

public class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private readonly Dictionary<string, string> PayNameDic = new Dictionary<string, string>() { { "0", "bkash" }, { "2", "nagad" } };

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    #region Credit 提款

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["host"];
        var apiKey = inputs["key"];
        var AesKey = inputs["AESKey"];
        var reqAmount = decimal.Parse(inputs["Amount"]) * 100;
        var strAmount = reqAmount.ToString("#");
        var currencyId = inputs["PayCurrencyId"];
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var merchantPayCode = inputs["MerchantPayCode"];
        var payCode = inputs["PayCode"];
        var accountName = inputs["AccountName"];
        var accountNo = inputs["AccountNo"];

        try
        {
            var plainreq = new Dictionary<string, string>
            {
                {"orderNo", PrefixTransId(currencyId,transId)},
                {"payCode", merchantPayCode},
                {"amount", strAmount},
                {"notifyUrl", notifyUrl},
                {"payeeType", payCode},
                {"payeeName", accountName},
                {"payeeFirstInfo", accountNo},
                {"payeeSecondInfo", PayNameDic[payCode]},
                {"payeeThirdInfo", ""},
                {"payeeFourInfo", ""}
            };

            var data = AesEncrypt(ToJson(plainreq), AesKey);

            var req = new Dictionary<string, string> { { "data", data } };

            var res = Payout(apiUrl, apiKey, req).Result;

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

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var aesKey = payResource["AESKey"];
        var req = FromJson<OnePayCallBackRequest>(bodyContent);

        if (!VerifyCallBackData(req.data, aesKey, OrderType.Credit, out PlainCallBackData plainData))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                Response = CallbackResponse.Failed

            };
        }

        var transStatus = OnePayDefinition.GetTransStatus(plainData.status);
        var result = new NotifyWithdrawResult
        {
            ThirdId = plainData.channelNo,
            WithdrawId = RemovePrefixTransId(plainData.merchantNo),
            TransStatus = transStatus,
            Response = CallbackResponse.Success
        };

        return result;
    }

    /// <summary>
    /// NoUse
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Debit 存款

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["host"];
        var apiKey = inputs["key"];
        var AesKey = inputs["AESKey"];
        var reqAmount = decimal.Parse(inputs["Amount"]) * 100;
        var strAmount = reqAmount.ToString("#");
        var currencyId = inputs["PayCurrencyId"];
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var payCode = inputs["MerchantPayCode"];
        var phoneNo = inputs["PhoneNo"];

        try
        {
            var plainreq = new Dictionary<string, string>
            {
                {"orderNo", PrefixTransId(currencyId,transId)},
                {"payCode", payCode},
                {"amount", strAmount},
                {"notifyUrl", notifyUrl},
                {"payerName", phoneNo}
            };

            var data = AesEncrypt(ToJson(plainreq), AesKey);

            var req = new Dictionary<string, string> { { "data", data } };

            var res = CreateOrder(apiUrl, apiKey, req).Result;

            var response = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = res.Data.PaymentUrl,
                ThirdId = res.Data.ChannelNo
            };

            return response;
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var aesKey = payResource["AESKey"];
        var req = FromJson<OnePayCallBackRequest>(bodyContent);

        if (!VerifyCallBackData(req.data, aesKey, OrderType.Debit, out PlainCallBackData plainData))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                Comment = "Decrypt callback request failed.",
                Response = CallbackResponse.Failed
            };
        }

        var transStatus = OnePayDefinition.GetTransStatus(plainData.status);
        var result = new NotifyDepositResult
        {
            ThirdId = plainData.channelNo,
            DepositId = RemovePrefixTransId(plainData.merchantNo),
            Comment = plainData.remark,
            TransStatus = transStatus,
            ActualAmount = plainData.amount / 100,
            Response = CallbackResponse.Success
        };

        return result;
    }

    /// <summary>
    /// NoUse
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #region Private Methods

    private async Task<OnePayCreateResponse<OnePayCreateData>> CreateOrder(string baseApiUrl, string key, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePay;
        var (result, statusCode) = await _httpHelper
            .WithHeader("Authorization", key)
            .WithHeader("Content-Type", MediaType.Json)
            .PostJsonAsync<Dictionary<string, string>, OnePayCreateResponse<OnePayCreateData>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"OnePay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != "200")
            throw new CustomException($"OnePay CreatOrder response error: {result.Code} {HttpUtility.UrlDecode(result.Message)}");

        return result;
    }

    private async Task<OnePayCreateResponse<OnePayCreateData>> Payout(string baseApiUrl, string key, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePayout;
        var (result, statusCode) = await _httpHelper
            .WithHeader("Authorization", key)
            .WithHeader("Content-Type", MediaType.Json)
            .PostJsonAsync<Dictionary<string, string>, OnePayCreateResponse<OnePayCreateData>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"OnePay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Code != "200")
            throw new CustomException(
                $"OnePay Payout response error: {result.Code} {HttpUtility.UrlDecode(result.Message)}");

        return result;
    }

    private bool VerifyCallBackData(string data, string apiKey, OrderType orderType, out PlainCallBackData plainData)
    {
        plainData = null;
        try
        {
            var plainString = AesDecrypt(data, apiKey);
            var obj = JObject.Parse(plainString);
            if (!obj.ContainsKey("status") || !obj.ContainsKey("channelNo")
                || !obj.ContainsKey("merchantNo") || !obj.ContainsKey("amount")
                || !obj.ContainsKey("orderType"))
                return false;

            plainData = JsonConvert.DeserializeObject<PlainCallBackData>(plainString);

            if (plainData == null)
            {
                _logger.LogInformation($"plainData is null.");
                return false;
            }

            if (plainData.orderType != (int)orderType)
            {
                _logger.LogInformation($"plainData.orderType is Invaild. orderType={orderType.ToString()}. \n " +
                    $"plainData={JsonConvert.SerializeObject(plainData)}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Decrypt callback request failed, Message:{ex.Message}");
            return false;
        }
    }

    private string PrefixTransId(string currencyId, string transId)
    {
        if (transId.Length < 10)
            return $"{currencyId}-{transId}";
        return transId;
    }

    private string RemovePrefixTransId(string transId)
    {
        if (transId.Contains("-"))
            return transId.Split("-")[1];
        return transId;
    }

    #endregion
}

