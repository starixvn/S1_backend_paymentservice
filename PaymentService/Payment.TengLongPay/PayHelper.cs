using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.TengLongPay;

public class PayHelper : PayHelperBase
{
    //private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private readonly decimal _amountFix = 100;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        //_httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var apiHost = inputs["Host"];
        var key = inputs["Key"];
        var amount = decimal.Parse(inputs["Amount"]);
       
        var req = new Dictionary<string, string>
        {
            {"mch", inputs["MerchantNo"]},
            {"sn", inputs["TransID"]},
            {"amount", (amount * _amountFix).ToString("#")},
            {"type", inputs["PayCode"]},
            {"date", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
        };
        
        req.Add("sign", GetSign(req, key));
        req.Add("callback", inputs["CallBackUrl"]);
        req.Add("goods", inputs["AccountName"]);
        
        return new DepositResult
        {
            Success = true,
            ErrorCode = "0",
            Url = apiHost + "?" + ToQueryStr(req)
        };
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var data = FromJson<Dictionary<string, string>>(bodyContent);
        var transId = data["sn"];
        var key = payResource["Key"];
        var keys = new []{ "mch", "sn", "amount", "mch_sn", "type", "date" };
        
        if (!VerifySign(data, key, data["sign"], keys))
        {
            _logger.LogInformation($"{transId}, signature verification failed.");

            return new NotifyDepositResult
            {
                DepositId = transId,
                TransStatus = TransStatus.Pending,
                Comment = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }

        var result = new NotifyDepositResult
        {
            ActualAmount = decimal.Parse(data["amount"]) / _amountFix,
            DepositId = transId,
            ThirdId = data["mch_sn"],
            TransStatus = data["status"] == "1" ? TransStatus.Success : TransStatus.Fail,
            Message = bodyContent,
            Response = CallbackResponse.Success
        };

        return result;
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }

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
    
    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> input, string key, string target, IEnumerable<string> signKeys)
    {
        var signKvp = input.Where(x => signKeys.Contains(x.Key));

        return GetSign(signKvp, key).Equals(target);
    }
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
    {
        var str = ToQueryStr(input) + $"&key={key}";
        var sign = ToMD5(str).ToLower();

        return sign;
    }
    
    private static string ToQueryStr(IEnumerable<KeyValuePair<string, string>> input)
        => string.Join('&', input
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={x.Value}"));
}