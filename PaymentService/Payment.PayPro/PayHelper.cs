using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PayPro;

public class PayHelper :　PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    
    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }
    
    #region Debit
    
    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["Host"];
        var merchantCode = inputs["MerchantCode"];
        var merchantId = inputs["MerchantID"];
        var merchantKey = inputs["MerchantKey"];
        var merchantPass1 = inputs["MerchantPass1"];
        var merchantPass2 = inputs["MerchantPass2"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var payCurrencyCode = inputs["PayCurrencyId"];
        var strAmount = reqAmount.ToString("G29");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var payCode = inputs["PayCode"];
        var bankCode = inputs["BankCode"];
        var merchantIVKey = merchantPass1 + merchantPass2;
            
        try
        {
            var payMethod = payCode switch
            {
                "OnlineBanking" => "offline",
                "EWallet" => "qrpay",
                _ => "offline"
            };

            var plainReq = new Dictionary<string, string>
            {
                {"merchantId", merchantId},
                {"transactionId", transId},
                {"transactionType", "D"},
                {"payMethod", payMethod},
                {"currency", payCurrencyCode},
                {"amount", strAmount},
                {"callback", notifyUrl},
                {"response", notifyUrl},
                {"designatedBankCode", bankCode}
                //{"lang", "vn"}
            };
            
            //先拿銀行清單
            // var plainReq = new Dictionary<string, string>
            // {
            //     { "merchantId", merchantId },
            //     { "currency", "VND" },
            //     { "amount", "100000000" }
            // };
            
            var data = AESEncryptContent(ToJson(plainReq), merchantKey,merchantIVKey);

            var req = new Dictionary<string, string>
            {
                { "merchantId", merchantId } ,
                { "message", data}
            };

            _logger.LogInformation($"plainData={JsonConvert.SerializeObject(plainReq)}.");
            
            var res = CreateOrder(apiUrl, req);

            var response = new DepositResult
            {
                Success = true,
                ErrorCode = res.Status ,
                Url = res.RedirectUrl,
            };

            return response;
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"PayPro Deposit failed, Code:{ex.Code}, Message:{ex.Message}");
            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Credit
    
    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["Host"];
        var merchantCode = inputs["MerchantCode"];
        var merchantId = inputs["MerchantID"];
        var merchantKey = inputs["MerchantKey"];
        var merchantPass1 = inputs["MerchantPass1"];
        var merchantPass2 = inputs["MerchantPass2"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("G29");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var merchantIVKey = merchantPass1 + merchantPass2;

        try
        {
            var plainReq = new Dictionary<string, string>
            {
                { "merchantId", merchantId },
                { "transactionId", transId },
                { "transactionType", "D" },
                { "payMethod", "ebank" },
                { "currency", "VND" },
                { "amount", strAmount },
                { "callback", notifyUrl },
                { "response", notifyUrl },
                { "lang", "vn" }
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"PayPro Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                Message = ex.Message
            };
        }

        throw new NotImplementedException();
    }
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region UnUsed

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Private

    private CreateOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
    { 
        var apiUrl = baseApiUrl + PayProDefinition.Path.CreateOrder;

        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostUrlEncoded<Dictionary<string, string>, CreateOrderResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        { 
            throw new CustomException( 
                $"PayPro creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Status == "fail")
            throw new CustomException("PayPro creat order failed Failed.");

        return result;
    }

    private CreatePayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + PayProDefinition.Path.CreatePayout;
        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostUrlEncoded<Dictionary<string, string>, CreatePayoutResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"PayPro create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.code != 1)
            throw new CustomException("PayPro create payout failed, msg=" + result.msg);

        return result;
    }
    
    private GetBankList GetBankList(string baseApiUrl, Dictionary<string, string> req)
    { 
        var apiUrl = baseApiUrl + "/api/merchant/banklist";

        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostUrlEncoded<Dictionary<string, string>, GetBankList>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        { 
            throw new CustomException(
                $"PayPro creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.status == "fail")
            throw new CustomException("PayPro creat order failed Failed.");

        return result;
    }

    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string secretKey)
    {
        var sortedParams = param.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}={x.Value}");
        var str = string.Join("&", sortedParams) + $"&key={secretKey}";

        _logger.LogInformation(str);
        var sign = base.ToMD5(str).ToUpper();
        _logger.LogInformation(sign);

        return sign;
    }

    private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string secretKey)
    {
        var sign = GetSign(param, secretKey);

        return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
    
}