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
        var privKey = "MIICXAIBAAKBgQCC56IcQidwXVpflei0CINes+yJM/d7y19eNcaE+TGn4vIlUtlAVDuR6jouv6JCQeluB8VMiHdo4PMvrRwe8iMWBfFr2ejIzTjP5GguOfzVxhK6Ev3VyeX0Pj0DD9Emd/ZsuUXL36w2sUZxs7ELFy7xWkS2k5dy4GwKGM+TQOq+pQIDAQABAoGAB3B7jYiS7jdX9fuyhFzBfoXSKP/7jh0CN/vtYsw4rrZelZFuSFIhKos5sENyyLovNnIZm95bjunN2EVJxlZzeOyxnKab0na2LdJqhCM0zl+ltpl3A2vU8YkWX8mnyP5axm5WFdpiNoF1BlR2l+lld8qsMLw9C+UsaXUX8PNtcfECQQCHuEgrS0k/Km2iAS5b04tPVxSSSA33B3uUAq4hGomww8PSeGW5h564latmRArhcAEg4HFwdpbQgHPemY+hl3VJAkEA9uruQrRhuNVVDlAkSBWX08Ixkg7DvqLaikXRpfzZL41VsPuBrdqTHS+sHZeXFPfNiccDS32BAkFkbd8hmXSqfQJASv/TIdmgyH8xxmzQqX3lZhrBC3U2AmToLYqtny2DGWOVOyG9VSFU3LwY05KWUK4McZaWGYfvH5lcbo59iNahwQJBAMj3y/b00/jZgcIFjpIxKQMMssX7SGy4JgGW9B0SoD7o3Q8QlarIFScTLxZi1HAXhO4X0eG2g7MvQCuBCdrZrw0CQCaG8lGGxHBC7DI4tYfQhYjMYtc4vnB/ZqLie/m/upN1940iaq7Oq3KhGC2x1xEV4Ohfo7FFrsqxGugsrif7iK4=";
        //var publicKey = inputs["PublicKey"];
        var publicKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCC56IcQidwXVpflei0CINes+yJM/d7y19eNcaE+TGn4vIlUtlAVDuR6jouv6JCQeluB8VMiHdo4PMvrRwe8iMWBfFr2ejIzTjP5GguOfzVxhK6Ev3VyeX0Pj0DD9Emd/ZsuUXL36w2sUZxs7ELFy7xWkS2k5dy4GwKGM+TQOq+pQIDAQAB";
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
            var plainSign = string.Join("", plainreq.Select(kp => kp.Value).ToArray()).ToLower();


            //var prikeyXml = RSAPrivateKeyJava2DotNet(privateKeyContent);
            //var sign = RsaSign(plainSign, prikeyXml, HashAlgorithmName.SHA1);
            //var bytesign = Convert.FromBase64String(sign);
            //var hex = ByteToHexStr(bytesign);
            //hex = CorrectionCiphertext(hex);
            //plainreq.Add("sign", hex);

            var sign = string.Empty;
            using (RSA rsa = new RSACng())
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(privKey), out _);

                var data = Encoding.UTF8.GetBytes(plainSign);

                var signatureBytes = rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

                sign = Convert.ToBase64String(signatureBytes);
            }
            sign = CorrectionCiphertext(sign);
            var bytesign = Convert.FromBase64String(sign);
            var hex = ByteToHexStr(bytesign);
            plainreq.Add("sign", hex);

            if (VerifySign(plainreq, publicKey, sign, HashAlgorithmName.SHA1))
            {
                _logger.LogInformation($"wrong!");
            }
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
            .WithHeader("Content-Type", MediaType.Json)
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

