using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using System.Web;
using PaymentCore.Extensions;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Text;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Payment.WPPay;

internal class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private readonly List<string> NoSignField = new List<string> { "bankCardNumber", "bankName", "payAmount", "sign" };

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    #region Credit 提款

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["host"];
        var merchantCode = inputs["MerchantCode"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("#.00");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var payCode = inputs["PayCode"];
        var privateKey = inputs["PrivateKey"];
        var publicKey = inputs["PublicKey"];
        var bankCode = inputs["BankCode"];
        var accountNo = inputs["AccountNo"];

        try
        {
            var plainreq = new Dictionary<string, string>
            {
                {"merchantCode", merchantCode},
                {"merchantTradeNo", transId},
                {"amount", strAmount},
                {"channel", payCode},
                {"notifyUrl", notifyUrl},
                {"bankCardNumber", accountNo},
                {"bankCode", bankCode},
                {"bankName", bankCode}
            };

            plainreq.Add("sign", GetSign(plainreq, privateKey));

            var content = RsaEncryptWithPublic(ToJson(plainreq), publicKey);
            _logger.LogInformation($"{transId} plain Content={ToJson(plainreq)}.");

            var req = new Dictionary<string, string>
            {
                {"merchantCode", merchantCode},
                {"content",  content }
            };

            var res = Payout(apiUrl, req).Result;

            if (!VerifyCreateData(res.Content, privateKey, publicKey, out WPPayCreateDataContent decryptDataResult))
            {
                throw new CustomException($"WPPay Payout response error: VerifyCreateData Fail.");
            }

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
        var privateKey = payResource["PrivateKey"];
        var publicKey = payResource["PublicKey"];
        var req = FromJson<WPPayCallBackRequest>(bodyContent);

        if (!VerifyCallBackData(req.Content, privateKey, publicKey, out WPPayCallBackDataContent plainData))
        {
            return new NotifyWithdrawResult
            {
                TransStatus = TransStatus.Pending,
                Message = plainData.TradeStatus,
                Response = CallbackResponse.Failed
            };
        }

        var transStatus = WPPayDefinition.GetTransStatus(plainData.TradeStatus);
        var result = new NotifyWithdrawResult
        {
            ThirdId = plainData.TradeNo,
            WithdrawId = plainData.MerchantTradeNo,
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
        var merchantCode = inputs["MerchantCode"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("#.00");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var payCode = inputs["PayCode"];
        var privateKey = inputs["PrivateKey"];
        var publicKey = inputs["PublicKey"];

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

            plainreq.Add("sign", GetSign(plainreq, privateKey));

            var content = RsaEncryptWithPublic(ToJson(plainreq), publicKey);
            _logger.LogInformation($"{transId} plain Content={ToJson(plainreq)}.");

            var req = new Dictionary<string, string>
            {
                {"merchantCode", merchantCode},
                {"content",  content }
            };

            var res = CreateOrder(apiUrl, req).Result;

            if (!VerifyCreateData(res.Content, privateKey, publicKey, out WPPayCreateDataContent decryptDataResult))
            {
                throw new CustomException($"WPPay CreatOrder response error: VerifyCreateData Fail.");
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
        var privateKey = payResource["PrivateKey"];
        var publicKey = payResource["PublicKey"];
        var req = FromJson<WPPayCallBackRequest>(bodyContent);

        if (!VerifyCallBackData(req.Content, privateKey, publicKey, out WPPayCallBackDataContent plainData))
        {
            return new NotifyDepositResult
            {
                TransStatus = TransStatus.Pending,
                Comment = "Decrypt callback request failed.",
                Response = CallbackResponse.Failed
            };
        }

        var transStatus = WPPayDefinition.GetTransStatus(plainData.TradeStatus);
        var result = new NotifyDepositResult
        {
            ThirdId = plainData.TradeNo,
            DepositId = plainData.MerchantTradeNo,
            Comment = plainData.TradeStatus,
            TransStatus = transStatus,
            ActualAmount = decimal.Parse(plainData.PayAmount),
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

    private async Task<WPPayCreateData> CreateOrder(string baseApiUrl, Dictionary<string, string> req)
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
                $"WPPay CreatOrder response error: result is null. {result.ErrorCode} {HttpUtility.UrlDecode(result.Msg)}");
        }

        var data = JsonConvert.DeserializeObject<WPPayCreateData>(result.Data);

        return data;
    }

    private async Task<WPPayCreateData> Payout(string baseApiUrl, Dictionary<string, string> req)
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

        if (string.IsNullOrWhiteSpace(result.Data))
        {
            throw new CustomException(
                $"WPPay CreatOrder response error: result is null. {result.ErrorCode} {HttpUtility.UrlDecode(result.Msg)}");
        }

        var data = JsonConvert.DeserializeObject<WPPayCreateData>(result.Data);

        return data;
    }

    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string privateKey)
    {
        param = param.Where(x => !string.IsNullOrWhiteSpace(x.Value)).OrderBy(kp => kp.Key).ToDictionary(x => x.Key, x => x.Value);
        var plainSign = string.Join("", param.Where(x => !NoSignField.Contains(x.Key)).Select(kp => kp.Value).ToArray());
        _logger.LogInformation($"Sign plaintext={plainSign}.");

        var sign = RsaSign(plainSign, privateKey, HashAlgorithmName.SHA1);
        var bytesign = Convert.FromBase64String(sign);
        return ByteToHexStr(bytesign).ToLower();
    }

    private bool VerifyCreateData(string content, string privateKey, string publicKey, out WPPayCreateDataContent plainData)
    {
        plainData = null;
        try
        {
            var plainString = RsaDecryptWithPrivate(content, privateKey);
            plainData = JsonConvert.DeserializeObject<WPPayCreateDataContent>(plainString);

            if (plainData == null)
            {
                _logger.LogInformation($"plainData is null.");
                return false;
            }

            if (VerifySign(plainString, publicKey, plainData.Sign))
            {
                _logger.LogInformation($"VerifySign is null.");
                return false;
            }

            _logger.LogInformation($"dataContent={JsonConvert.SerializeObject(plainData)}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Decrypt callback request failed, Message:{ex.Message}");
            return false;
        }
    }

    private bool VerifyCallBackData(string data, string privateKey, string publicKey, out WPPayCallBackDataContent plainData)
    {
        plainData = null;
        try
        {
            var plainString = RsaDecryptWithPrivate(data, privateKey);
            plainData = JsonConvert.DeserializeObject<WPPayCallBackDataContent>(plainString);

            if (plainData == null)
            {
                _logger.LogInformation($"plainData is null.");
                return false;
            }

            if (VerifySign(plainString, publicKey, plainData.Sign))
            {
                _logger.LogInformation($"VerifySign is null.");
                return false;
            }

            _logger.LogInformation($"dataContent={JsonConvert.SerializeObject(plainData)}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Decrypt callback request failed, Message:{ex.Message}");
            return false;
        }
    }

    private bool VerifySign(string plainData, string publicKey, string target)
    {
        target = HexStringToString(target);

        var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(plainData);
        if (dic == null || !dic.Any()) return false;

        dic = dic.Where(x => !string.IsNullOrWhiteSpace(x.Value)).OrderBy(kp => kp.Key).ToDictionary(x => x.Key, x => x.Value);

        RsaKeyParameters publicKeyParam = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
        var xml = string.Format(
        "<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>",
        Convert.ToBase64String(publicKeyParam.Modulus.ToByteArrayUnsigned()),
        Convert.ToBase64String(publicKeyParam.Exponent.ToByteArrayUnsigned())
        );

        RSA rsa = RSA.Create();
        rsa.FromXmlString(xml);
        return rsa.VerifyData(Encoding.UTF8.GetBytes(ToJson(dic)), Convert.FromBase64String(target), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }

    #endregion

}

