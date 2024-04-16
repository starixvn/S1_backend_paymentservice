using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.TengLongPay;

public class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private const decimal AmountFix = 100;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var apiHost = inputs["DHost"];
        var key = inputs["DKey"];
        var amount = decimal.Parse(inputs["Amount"]);

        try
        {
            var req = new Dictionary<string, string>
            {
                {"mch", inputs["DMerchantNo"]},
                {"sn", inputs["TransID"]},
                {"amount", (amount * AmountFix).ToString("#")},
                {"type", inputs["PayCode"]},
                {"date", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
            };
            
            req.Add("sign", GetSign(req, key));
            req.Add("callback", inputs["CallBackUrl"]);
            req.Add("goods", inputs["AccountName"]);
            req.Add("ip", inputs["ClientIP"]);
            
            var (resp, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, DepositResp>(apiHost + Path.CreateOrder, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(
                    $"Generate orders failed, HttpStatus: {(int)statusCode} {statusCode}");

            if (!resp.Code.Equals(200))
                throw new CustomException("Generate orders failed, msg=" + resp.Message);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = resp.Data
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var data = FromJson<Dictionary<string, string>>(bodyContent);
        var transId = data["mch_sn"];
        var key = payResource["DKey"];
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
            ActualAmount = decimal.Parse(data["amount"]) / AmountFix,
            DepositId = transId,
            ThirdId = data["sn"],
            TransStatus = data["status"] == "1" ? TransStatus.Success : TransStatus.Fail,
            Message = bodyContent,
            Response = CallbackResponse.Success
        };

        return result;
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiHost = inputs["WHost"];
        var key = inputs["WKey"];
        var amount = decimal.Parse(inputs["Amount"]);
        
        try
        {
            var req = new Dictionary<string, string>
            {
                {"mchid", inputs["WMerchantNo"]},
                {"out_trade_no", inputs["TransID"]},
                {"money", amount.ToString("F2")},
                {"notifyurl", inputs["CallBackUrl"]},
                {"bankname", inputs["BankName"]},
                {"subbranch", "subbranch"},
                {"accountname", inputs["AccountName"]},
                {"cardnumber", inputs["AccountNo"]}
            };
            
            req.Add("sign", GetSign(req, key));
            
            var (resp, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, WithdrawResp>(apiHost + Path.CreatePayout, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(
                    $"Create payout orders failed, HttpStatus: {(int)statusCode} {statusCode}");

            if (!resp.Status.Equals("success"))
                throw new CustomException("Create payout orders failed, msg=" + resp.Message);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = inputs["TransID"],
                RawContent = JsonConvert.SerializeObject(resp)
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var key = payResource["WKey"];
        var transId = formData["out_trade_no"];
        var keys = new []{ "mchid", "out_trade_no", "amount", "transaction_id", "refCode", "refMsg" };

        if (!VerifySign(formData, key, formData["sign"], keys))
        {
            _logger.LogInformation($"{transId}, signature verification failed.");

            return new NotifyWithdrawResult
            {
                WithdrawId = transId,
                TransStatus = TransStatus.Pending,
                Message = "Signature verification failed.",
                Response = CallbackResponse.Failed
            };
        }
            
        return new NotifyWithdrawResult
        {
            WithdrawId = transId,
            ThirdId = formData["transaction_id"],
            Message = JsonConvert.SerializeObject(formData),
            TransStatus = Definition.GetPayoutStatus(formData["refCode"]),
            Response = CallbackResponse.Success
        };
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