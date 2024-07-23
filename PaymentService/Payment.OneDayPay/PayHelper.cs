using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Helpers;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.OneDayPay;

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
        var baseApiUrl = input["Host"];
        var privateKey = input["PrivateKey"];
        var aesKey = input["AESKey"];
        var transId = input["TransID"];
        var merchantCode = input["MerchantCode"];
        var (payCode, bankCode) = SplitPayCode(input["PayCode"]);

        try
        {
            var data =  new Dictionary<string, string>
            {
                { "MerchantCode", merchantCode },
                { "MerchantTransactionId", transId },
                { "MerchantClientId", input["UserName"] },
                { "CurrencyCode", input["PayCurrencyId"] },
                { "Amount", GetRequestAmount(input["Amount"]) },
                { "NotifyUrl", input["CallBackUrl"] },
                { "ReturnUrl", input["ReturnUrl"] },
                { "IPAddress", input["ClientIP"] },
                { "PaymentMethodCode", payCode }
            };

            if (payCode.Equals("BMR"))
            {
                data.Add("SourceClientBankCode", input["BankCode"]);
            }

            if (payCode.Equals("BQR"))
            {
                data.Add("SourceClientBankCode", bankCode);
            }
            
            var jsonData = ToJson(data);
            _logger.LogInformation(jsonData);
            
            var req = new OneDayPayRequest
            {
                MerchantCode = merchantCode,
                Data = AesEncrypt(jsonData, aesKey),
                Signature = RsaSignPkcs8PrivateKey(jsonData, privateKey, HashAlgorithmName.SHA1),
                Version = 0
            };

            var resp = CreateOrder(baseApiUrl, req);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = resp.ThirdId,
                Url = resp.Url
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
        var publicKey = input["PublicKey"];
        var aesKey = input["AESKey"];
        var req = FromJson<CallbackRequest>(bodyContent);
        var jsonData = AesDecrypt(req.Data, aesKey);

        if (!VerifySign(jsonData, publicKey, req.Signature))
        {
            return new NotifyDepositResult
            {
                DepositId = req.TransId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        var data = FromJson<CallbackData>(jsonData);
        var transStatus = Definition.GetDepositTransStatus(data.Status);

        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = data.Amount,
            ThirdId = req.ThirdId,
            Comment = data.Status,
            TransStatus = transStatus,
            Response = CallbackResponse.Success
        };
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Credit

    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        var baseApiUrl = input["Host"];
        var privateKey = input["PrivateKey"];
        var aesKey = input["AESKey"];
        var transId = input["TransID"];
        var merchantCode = input["MerchantCode"];

        try
        {
            var data =  new Dictionary<string, string>
            {
                { "MerchantCode", merchantCode },
                { "MerchantTransactionId", transId },
                { "MerchantClientId", input["UserName"] },
                { "CurrencyCode", input["PayCurrencyId"] },
                { "Amount", GetRequestAmount(input["Amount"]) },
                { "DestinationClientBankCode", input["BankCode"] },
                { "DestinationClientAccountNumber", input["AccountNo"] },
                { "DestinationClientAccountHolderName", input["AccountName"] },
                { "NotifyUrl", input["CallBackUrl"] },
                { "ReturnUrl", input["CallBackUrl"] },
                { "IPAddress", input["ClientIP"] },
                { "PaymentMethodCode", input["PayCode"] }
            };

            var jsonData = ToJson(data);
            _logger.LogInformation(jsonData);
            
            var req = new OneDayPayRequest
            {
                MerchantCode = merchantCode,
                Data = AesEncrypt(jsonData, aesKey),
                Signature = RsaSignPkcs8PrivateKey(jsonData, privateKey, HashAlgorithmName.SHA1),
                Version = 0
            };

            var resp = Payout(baseApiUrl, req);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = resp.ThirdId
            };
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
        var publicKey = payResource["PublicKey"];
        var aesKey = payResource["AESKey"];
        var req = FromJson<CallbackRequest>(bodyContent);
        var jsonData = AesDecrypt(req.Data, aesKey);

        if (!VerifySign(jsonData, publicKey, req.Signature))
        {
            return new NotifyWithdrawResult
            {
                ThirdId = req.TransId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        var data = FromJson<CallbackData>(jsonData);
        var transStatus = Definition.GetPayoutTransStatus(data.Status);

        return new NotifyWithdrawResult
        {
            ThirdId = req.ThirdId,
            TransStatus = transStatus,
            Message = jsonData,
            Response = CallbackResponse.Success,
            WithdrawId = req.TransId
        };
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Private
    
    private CreateDeposit CreateOrder(string baseApiUrl, OneDayPayRequest req)
    {
        var apiUrl = baseApiUrl + Path.Deposit;

        var (result, statusCode) = _httpHelper
            .PostJson<OneDayPayRequest, Response<CreateDeposit>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"OneDayPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (!result.IsSuccess)
            throw new CustomException($"OneDayPay CreatOrder response error: {result.Code} {result.Message}");

        return result.Data;
    }

    private CreatePayout Payout(string baseApiUrl, OneDayPayRequest req)
    {
        var apiUrl = baseApiUrl + Path.Withdrawal;
        var (result, statusCode) = _httpHelper
            .PostJson<OneDayPayRequest, Response<CreatePayout>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"OneDayPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (!result.IsSuccess)
            throw new CustomException(
                $"OneDayPay Payout response error: {result.Code} {result.Message}");

        return result.Data;
    }

    private static bool VerifySign(string str, string publicKey, string target)
    {
        var result = RsaHelper.RsaVerifyData(str, target, publicKey, HashAlgorithmName.SHA1);

        return result;
    }

    private static string GetRequestAmount(string strAmount)
        => decimal.Parse(strAmount).ToString("F2");
    
    private static string AesEncrypt(string str, string secretKey)
    {
        using var aes = Aes.Create();
        aes.Key = new Rfc2898DeriveBytes(secretKey, Encoding.UTF8.GetBytes(secretKey)).GetBytes(256 / 8);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
            
            
        var input = Encoding.UTF8.GetBytes(str);
        aes.GenerateIV();
        var iv = aes.IV;
        
        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var cipherStream = new MemoryStream();
        using (var tCryptoStream = new CryptoStream(cipherStream, encryptor, CryptoStreamMode.Write))
        using (var tBinaryWriter = new BinaryWriter(tCryptoStream))
        {
            cipherStream.Write(iv);
            tBinaryWriter.Write(input);
            tCryptoStream.FlushFinalBlock();
        }
        
        return Convert.ToBase64String(cipherStream.ToArray());
    }

    private static string AesDecrypt(string str, string secretKey)
    {
        using var aes = Aes.Create();
        aes.Key = new Rfc2898DeriveBytes(secretKey, Encoding.UTF8.GetBytes(secretKey)).GetBytes(256 / 8);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
            
        var cipherTextBytes = Convert.FromBase64String(str);
        var iv = new byte[16];
        Array.Copy(cipherTextBytes, 0, iv, 0, iv.Length);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(aes.Key, iv), CryptoStreamMode.Write))
        using (var binaryWriter = new BinaryWriter(cs))
        {
            binaryWriter.Write(
                cipherTextBytes,
                iv.Length,
                cipherTextBytes.Length - iv.Length
            );
        }
        
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static (string Method, string bankCode) SplitPayCode(string payCode)
    {
        var result = payCode.Split('-');

        return (result[0], result.Length > 1 ? result[1] : string.Empty);
    }
    
    #endregion
}