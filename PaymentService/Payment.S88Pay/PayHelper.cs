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
    
        var req = new Dictionary<string, string>
        {
            { "merchant_code", merchantCode },
            { "merchant_api_key", apiKey },
            { "transaction_code", transId },
            { "transaction_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
            { "payment_code", input["PayCode"] },
            { "transaction_amount", input["Amount"]},
            { "user_id", input["UserName"] },
            { "currency_code", input["PayCurrencyId"] },
            { "bank_code", input["BankCode"] },
            { "callback_url", input["CallBackUrl"] }
        };
        
        var key = Encrypt(ToQueryStr(req), apiKey, secretKey);
        var apiUrl = $"{apiHost}/{merchantCode}/{Path.Deposit}?key={key}";
        
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
        var str = formData["key"];
        var decryptStr = Decrypt(str, apiKey, secretKey);
        var req = ParseQuery(decryptStr);
        
        _logger.LogInformation(decryptStr);
        
        var transStatus = Definition.GetDepositTransStatus(req["transaction_status"]);
    
        return new NotifyDepositResult
        {
            DepositId = formData["transaction_code"],
            ActualAmount = decimal.Parse(formData["transaction_actual_amount"]),
            ThirdId = formData["transaction_amount"],
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
                { "merchant_api_key", apiKey },
                { "transaction_code", transId },
                { "transaction_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "payment_code", input["PayCode"] },
                { "transaction_amount", input["Amount"]},
                { "user_id", input["UserName"] },
                { "currency_code", input["PayCurrencyId"] },
                { "bank_code", input["BankCode"] },
                { "callback_url", input["CallBackUrl"] }
            };
    
            var (response, statusCode) = _httpHelper
                .PostJson<object, WithdrawalResponse>($"{apiHost}{Path.Withdrawal}/{merchantCode}",
                    new { key = Encrypt(ToQueryStr(req), apiKey, secretKey) });
    
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
        var str = formData["key"];
        var decryptStr = Decrypt(str, apiKey, secretKey);
        var req = ParseQuery(decryptStr);
        
        _logger.LogInformation(decryptStr);
    
        var transStatus = Definition.GetPayoutTransStatus(req["transaction_status"]);

        return new NotifyWithdrawResult
        {
            ThirdId = req["transaction_no"],
            TransStatus = transStatus,
            Message = decryptStr,
            Response = CallbackResponse.Success,
            WithdrawId = req["transaction_code"]
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

    private string Encrypt(string str, string apikey, string secretKey)
        => AesEncrypt(str, ToSHA256(apikey), ToSHA256(secretKey)[..16]);
    
    
    private string Decrypt(string str, string apikey, string secretKey)
        => AesDecrypt(str, ToSHA256(apikey), ToSHA256(secretKey)[..16]);
    
    #endregion
}