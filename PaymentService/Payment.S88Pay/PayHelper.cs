using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.S88Pay;

public class PayHelper: PayHelperBase
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
        var secretKey = input["SecretKey"];
        var apiKey = input["ApiKey"];
        var transId = input["TransID"];
        var merchantCode = input["MerchantCode"];
        var (paymentCode, bankCode) = GetPayCode(input["PayCode"]);
    
        var req = new Dictionary<string, string>
        {
            { "merchant_code", merchantCode },
            { "merchant_api_key", apiKey },
            { "transaction_code", transId },
            { "transaction_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
            { "payment_code", paymentCode },
            { "transaction_amount", input["Amount"]},
            { "user_id", input["UserName"] },
            { "currency_code", input["PayCurrencyId"] },
            { "bank_code", string.IsNullOrEmpty(bankCode) ? input["BankCode"] : bankCode },
            { "callback_url", input["CallBackUrl"] }
        };

        var key = Encrypt(ToQueryStr(req), apiKey, secretKey);
        var apiUrl = $"{apiHost}/{merchantCode}{Path.Deposit}?key={key}";
        
        return new DepositResult
        {
            Success = true,
            ErrorCode = "0",
            Url = apiUrl
        };
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var secretKey = input["SecretKey"];
        var apiKey = input["ApiKey"];
        var req = FromJson<CallbackRequest>(bodyContent);
        var decryptStr = Decrypt(req.EncryptData, apiKey, secretKey);
        var reqDic = ParseQuery(decryptStr);
        
        _logger.LogInformation("decrypt data: " + ToJson(reqDic));
        
        var transStatus = Definition.GetDepositTransStatus(reqDic["transaction_status"]);
    
        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = decimal.Parse(reqDic["transaction_actual_amount"]),
            ThirdId = req.ThirdId,
            Comment = decryptStr,
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
        var apiHost = input["Host"];
        var secretKey = input["SecretKey"];
        var apiKey = input["ApiKey"];
        var transId = input["TransID"];
        var merchantCode = input["MerchantCode"];
    
        try
        {
            var req = new Dictionary<string, string>
            {
                { "merchant_code", merchantCode },
                { "transaction_code", transId },
                { "transaction_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "payout_code", input["PayCode"] },
                { "transaction_amount", input["Amount"]},
                { "user_id", input["UserName"] },
                { "currency_code", input["PayCurrencyId"] },
                { "bank_account_number", input["AccountNo"] },
                { "bank_code", input["BankCode"] },
                { "bank_name", input["BankName"] },
                { "account_name", input["AccountName"] },
                { "callback_url", input["CallBackUrl"] }
            };
    
            var (response, statusCode) = _httpHelper
                .PostJson<object, WithdrawalResponse>($"{apiHost}{Path.Withdrawal}/{merchantCode}",
                    new { key = Encrypt(ToJson(req), apiKey, secretKey) });
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {statusCode}", ((int) statusCode).ToString());
            }
    
            if (!response.Success)
            {
                throw new CustomException(response.Message);
            }
    
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"S88Pay payout failed, Message:{ex.Message}");
    
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
        var secretKey = payResource["SecretKey"];
        var apiKey = payResource["ApiKey"];
        var req = FromJson<CallbackRequest>(bodyContent);
        var decryptStr = Decrypt(req.EncryptData, apiKey, secretKey);
        var reqDic = FromJson<Dictionary<string, string>>(decryptStr);
        
        _logger.LogInformation("decrypt data: " + ToJson(reqDic));
    
        var transStatus = Definition.GetPayoutTransStatus(reqDic["transaction_status"]);

        return new NotifyWithdrawResult
        {
            ThirdId = req.ThirdId,
            TransStatus = transStatus,
            Message = decryptStr,
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

    private string ToQueryStr(Dictionary<string, string> input)
    {
        var str = string.Join('&', input
            .Select(x => $"{x.Key}={x.Value}"));
    
        _logger.LogInformation(str);
        return str;
    }

    private static Dictionary<string, string> ParseQuery(string str)
        => str.Split('&')
            .Select(item => item.Split('='))
            .ToDictionary(s => s[0], s => s[1]);

    private static (byte[] key, byte[] iv) GetKeys(string apikey, string secretKey)
    {
        using var sha256 = SHA256.Create();
        var iv = HashSha256(secretKey)[..16];
    
        return (sha256.ComputeHash(Encoding.ASCII.GetBytes(apikey)), Encoding.ASCII.GetBytes(iv));
        
        string HashSha256(string payload)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(payload);
            var hashBytes = sha256.ComputeHash(sourceBytes);
    
            return BitConverter.ToString(hashBytes)
                .Replace("-", string.Empty).ToLower();
        }
    }

    private static string Encrypt(string str, string apikey, string secretKey)
    {
        var (key, iv) = GetKeys(apikey, secretKey);
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = key;
        aes.IV = iv;
        
        using var encryptor = aes.CreateEncryptor();
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        var plainBytes = Encoding.ASCII.GetBytes(str);
        
        cryptoStream.Write(plainBytes, 0, plainBytes . Length);
        cryptoStream.FlushFinalBlock();

        var cipherBytes = memoryStream.ToArray();
        var cipherText = Convert.ToBase64String(cipherBytes);
        
        return Uri.EscapeDataString(cipherText);
    }
    
    private static string Decrypt(string str, string apikey, string secretKey)
    {
        var (key, iv) = GetKeys(apikey, secretKey);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = key;
        aes.IV = iv;
        
        using var encryptor = aes.CreateDecryptor();
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
    
        var cipherBytes = Convert.FromBase64String(Uri.UnescapeDataString(str));
        cryptoStream.Write(cipherBytes, 0, cipherBytes . Length);
        cryptoStream.FlushFinalBlock();
        var plainBytes = memoryStream.ToArray();
    
        return Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
    }

    private static (string paymentCode, string bankCode) GetPayCode(string payCode)
    {
        var splitPayCode = payCode.Split('-');

        return (splitPayCode[0], splitPayCode[1]);
    }
    
    #endregion
}