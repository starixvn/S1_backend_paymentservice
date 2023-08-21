using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using System.Web;
using PaymentCore.Extensions;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace Payment.WPPay;

internal class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;

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
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("#");
        var currencyId = inputs["PayCurrencyId"];
        var transId = inputs["TransID"];
        var accountName = inputs["AccountName"];
        var bankCode = inputs["BankCode"];
        var merchantCode = inputs["merchantCode"];
        var notifyUrl = inputs["CallBackUrl"];

        try
        {
            var req = new Dictionary<string, string>
                {
                    {"merchantCode", merchantCode},
                    {"out_trade_no", transId},
                    {"account_code", bankCode}, //
                    {"account_name", accountName},
                    {"agent_amount", strAmount},
                    {"currency", currencyId}, //
                    {"notify_url", notifyUrl},
                    {"nonce_str", Guid.NewGuid().ToString("N")},
                };

            req.Add("sign", GetSign(req, apiKey));

            var res = Payout(apiUrl, req).Result;
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
        throw new NotImplementedException();
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
        var merchantCode = inputs["MerchantCode"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("#.00");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var payCode = inputs["PayCode"];
        //var privKey = inputs["PrivateKey"];
        var privKey = @"MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJdAgEAAoGBANbQHyaUCJn2nW2E
O4KVQ3GCHZZWp8GRLYF6y1Uk4Cr7g4e+gJSIL+JtgHxFoWwwuSjiwoZ40dH4MYxI
UqfdrAAn4L6T2IYZ1RLJTRlwGmItRgYMMEK9r4dGChHj0kQepBCZOMJvNtUQkADY
h3NVhmTQjVhG5wrsAAHqx+U3eyLHAgMBAAECgYEA1se7DcCfpQmOuGypb9Q8XebC
HIpCK2kz/a1AVr6LII7gIT6Zo2lTr2rbN+YLE3jX+YhZBUkSSYWGTQQXGN6sVJqM
UfFKcvs/wuGnvbww2a2CAvME7dQNl5YwAKpJCX7Ztts3QlK19fU0qpX1LlOOahub
lu7nv/bcqS9Q0KUU5rkCQQDt9lLswIHGPoTnU4pm5nLX/SOrS4w1y2yOTuCScds0
mGBBT2D9NBzqZ+4xV/fDJajbfVX2qjFJUzW7877c7zALAkEA5xiVyPbQ4xTbMUa5
W8JAd8doXpLAzUUffR6K9T13569lZFiz0e/O0MlyiO6nAirsZr5O5m9yi3BbAt+a
y75htQJAT6cBMPBrQCGTOR90l2cSEAxS0TazkxoplyT99OcZmexbY+EfNQIpn6j0
SR4J+A92CypUCQIFB2dL+ELpo2iI6wJAQ3lDKpUd8l+BjEapEiiKaC6Zw5s3nfeD
tSV4zxfubYAhq4XWowJQsVdQELkVnCVqNql0IgYCPMJGYHVnHfvtTQJBAMGVHihP
ngJjpaCHQXOiyDtKYiWwQkefq4Ak188eIYX1POwcz0LfIc++QQPvKP5wyxnEq0dm
LhNE3bFc3zehnHY=";
        //var publicKey = inputs["PublicKey"];
        var publicKey = @"MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDW0B8mlAiZ9p1thDuClUNxgh2W
VqfBkS2BestVJOAq+4OHvoCUiC/ibYB8RaFsMLko4sKGeNHR+DGMSFKn3awAJ+C+
k9iGGdUSyU0ZcBpiLUYGDDBCva+HRgoR49JEHqQQmTjCbzbVEJAA2IdzVYZk0I1Y
RucK7AAB6sflN3sixwIDAQAB";

        privKey = privKey.Replace("\n", "").Replace("\r", "").Replace("\t", "");
        publicKey = publicKey.Replace("\n", "").Replace("\r", "").Replace("\t", "");
        try
        {
            var plainreq = new Dictionary<string, string>
            {
                {"merchantCode", merchantCode},
                {"merchantTradeNo", transId},
                {"amount", strAmount},
                {"notifyUrl", notifyUrl},
                {"channel", payCode}
            };

            plainreq = plainreq.Where(x => !string.IsNullOrWhiteSpace(x.Value)).OrderBy(kp => kp.Key).ToDictionary(x => x.Key, x => x.Value);
            var plainSign = string.Join("", plainreq.Select(kp => kp.Value).ToArray());

            #region sign test

            var sign = RsaSign(plainSign, RSAPrivateKeyJava2DotNet(privKey), HashAlgorithmName.SHA1);
            //sign = sign.ToLower();
            var bytesign = Convert.FromBase64String(sign);
            var hex = ByteToHexStr(bytesign);

            plainreq.Add("sign", hex.ToLower());

            if (Verify(plainSign, sign, publicKey)) //驗簽
            {
                _logger.LogInformation($"sign wrong!");
            }

            #endregion

            // content加密沒有問題
            var content = RsaEncryptWithPublic(ToJson(plainreq), publicKey);

            var req = new Dictionary<string, string>
            {
                {"merchantCode", merchantCode},
                {"content",  ByteToHexStr(Convert.FromBase64String(content)) }
            };

            _logger.LogInformation($"plainData={JsonConvert.SerializeObject(plainreq)}.");

            var res = CreateOrder(apiUrl, req).Result;

            if (!DecryptData(res.Content, privKey, publicKey, out WPPayCreateDataContent decryptDataResult))
            {
                throw new CustomException(
                $"WPPay CreatOrder response error: ");
            }

            var response = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = decryptDataResult.PayUrl,
                ThirdId = decryptDataResult.TradeNo
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
        throw new NotImplementedException();
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

    private async Task<WPPayCreateDataResponse> CreateOrder(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePay;

        var (result, statusCode) = await _httpHelper
            .PostJsonAsync<Dictionary<string, string>, WPPayCreateResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"WPPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Status != "SUCCESS")
        {
            throw new CustomException(
                $"WPPay CreatOrder response error: {result.ErrorCode} {HttpUtility.UrlDecode(result.Msg)}");
        }

        if (string.IsNullOrWhiteSpace(result.Data))
        {
            throw new CustomException(
                $"WPPay CreatOrder response error: {result.ErrorCode} {HttpUtility.UrlDecode(result.Msg)}");
        }

        var data = JsonConvert.DeserializeObject<WPPayCreateDataResponse>(result.Data);

        return data;
    }

    private async Task<WPPayCreateResponse> Payout(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + Path.CreatePayout;
        var (result, statusCode) = await _httpHelper
            .PostJsonAsync<Dictionary<string, string>, WPPayCreateResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"WPPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Status != "SUCCESS")
            throw new CustomException(
                $"WPPay Payout response error: {result.ErrorCode} {HttpUtility.UrlDecode(result.Msg)}");

        return result;
    }


    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
            .Where(x => !x.Key.Equals("sign"))
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

        _logger.LogInformation(str);
        var sign = ToMD5(str).ToUpper();
        _logger.LogInformation(sign);
        return sign;
    }

    private bool DecryptData(string content, string privateKey, string publicKey, out WPPayCreateDataContent dataContent)
    {
        dataContent = null;
        try
        {
            var plainString = UrlEncode(RSADecryptWithPrivateKey(RSAPrivateKeyJava2DotNet(privateKey), content));
            var obj = JObject.Parse(plainString);
            if (!obj.ContainsKey("merchantCode") || !obj.ContainsKey("merchantTradeNo")
                || !obj.ContainsKey("tradeNo") || !obj.ContainsKey("payUrl")
                || !obj.ContainsKey("sign"))
                return false;

            dataContent = JsonConvert.DeserializeObject<WPPayCreateDataContent>(plainString);

            if (dataContent == null)
            {
                _logger.LogInformation($"plainData is null.");
                return false;
            }

            _logger.LogInformation($"plainData={JsonConvert.SerializeObject(dataContent)}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Decrypt callback request failed, Message:{ex.Message}");
            return false;
        }
    }

    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target, HashAlgorithmName sHA1)
    {
        var sign = GetSign(param, apiKey);
        return sign.Equals(target);
    }

    #endregion

}

