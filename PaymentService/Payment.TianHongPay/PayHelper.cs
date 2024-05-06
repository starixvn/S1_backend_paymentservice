using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.TianHongPay;

public class PayHelper: PayHelperBase
{
    private readonly ILogger<PayHelper> _logger;
    private const decimal AmountFix = 100;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
    }

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var amount = decimal.Parse(inputs["Amount"]) * AmountFix;
        var req = new Dictionary<string, string>
        {
            {"merId", inputs["MerchantNo"]},
            {"orderId", inputs["TransID"]},
            {"orderType", inputs["PayCode"]},
            {"goodsName", inputs["UserName"]},
            {"amount", amount.ToString("G29")},
            {"bankCode", string.Empty},
            {"notifyUrl", inputs["CallBackUrl"]},
            {"frontUrl", inputs["ReturnUrl"]}
        };
        
        req.Add("sign", GetSign(req, inputs["Key"]));
            
        return new DepositResult
        {
            Success = true,
            ErrorCode = "0",
            FormData = req,
            Url = inputs["Host"] + Path.CreateOrder
        };
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
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

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var transId = formData["orderId"];
        var sign = GetSign(formData, payResource["Key"]);

        if (!formData["sign"].Equals(sign, StringComparison.OrdinalIgnoreCase))
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
            ActualAmount = decimal.Parse(formData["amount"]) / AmountFix,
            DepositId = transId,
            ThirdId = transId,
            TransStatus = Definition.GetTransStatus(formData["orderStatus"]),
            Response = CallbackResponse.Success
        };

        return result;
    }

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("&", param
                .Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

        _logger.LogInformation(str);
        var sign = base.ToMD5(str).ToUpper();
        _logger.LogInformation(sign);

        return sign;
    }
}