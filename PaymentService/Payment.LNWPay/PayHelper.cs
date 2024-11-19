using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.LNWPay;

public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private readonly IDictionary<string, Func<IDictionary<string, string>, IDepositData>> _depositMapper;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        _depositMapper = new Dictionary<string, Func<IDictionary<string, string>, IDepositData>>
        {
            {"Auto", CreateDepositAuto},
            {"QR", CreateDepositQr},
            {"True", CreateDepositTrue},
            {"KHQR", CreateDepositKHQr},
            {"PAYWAY", CreateDepositPayway}
        };
    }
    
    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var (method, _) = SplitPayCode(inputs["PayCode"]);
        
        try
        {
            if (!_depositMapper.ContainsKey(method))
                throw new CustomException($"Not support deposit type. Method={method}");

            var resp = _depositMapper[method].Invoke(inputs);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = resp.Url,
                ThirdId = resp.ThirdId
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"LNWPay create deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        // 這家callback沒有驗簽
        return new NotifyDepositResult
        {
            ActualAmount = decimal.Parse(formData["amount"]),
            DepositId = formData["ref1"],
            ThirdId = formData["id"],
            TransStatus = Definition.GetTransStatus(formData["status"]),
            Response = Definition.CallbackResponse
        };
    }
    
    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        var (method, type) = SplitPayCode(input["PayCode"]);
        var currencyId = input["CurrencyId"];
        var apiHost = input[$"{currencyId}_{method}ApiHost"];
        
        try
        {
            var req = new PayoutReq
            {
                Amount = decimal.Parse(input["Amount"]),
                ClientAccount = input["AccountNo"],
                ClientAccountName = input["AccountName"],
                ClientBank = input["BankCode"],
                TransId = input["TransID"]
            };
        
            var authToken = GetAuthToken(input);
        
            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(authToken)
                .PostJson<PayoutReq, PayResp<PayoutData>>(apiHost + Path.CreatePayout, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(
                    $"Create payout failed, HttpStatus: {(int)statusCode} {statusCode}");

            if (!result.Status.Equals(Definition.SuccessStatus))
                throw new CustomException("Create payout failed, msg=" + result.Message);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = input["TransID"],
                RawContent = JsonConvert.SerializeObject(result)
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

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        // 這家callback沒有驗簽
        return new NotifyWithdrawResult
        {
            WithdrawId = formData["ref1"],
            ThirdId = formData["id"],
            Message = formData["status"],
            TransStatus = Definition.GetTransStatus(formData["status"]),
            Response = Definition.CallbackResponse
        };
    }
    
    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
    {
        return formData["ref1"];
    }

    private string GetAuthToken(IDictionary<string, string> input)
    {
        var (method, type) = SplitPayCode(input["PayCode"]);
        var currencyId = input["CurrencyId"];
        var apiHost = input[$"{currencyId}_{method}ApiHost"];
        var apiToken = input[$"{currencyId}_{method}Token"];

        var str = $"{input[$"{currencyId}_Prefix"]}:{apiToken}:{DateTimeOffset.Now:yyyyMMddHHmmss}";
        var token = ToBase64Encode(str);
        _logger.LogInformation("token orig str: " + str);
        
        var (result, statusCode) = _httpHelper
            .WithHeader("Authorization", "Basic " + token)
            .PostJson<string, PayResp<AuthData>>(apiHost + Path.Auth, string.Empty);

        if (!statusCode.IsSuccessStatus())
            throw new CustomException(
                $"Get auth token failed, HttpStatus: {(int)statusCode} {statusCode}");

        if (!result.Status.Equals(Definition.SuccessStatus))
            throw new CustomException("Get auth token failed, msg=" + result.Message);

        return result.Data.Token;
    }
    
    private IDepositData CreateDepositAuto(IDictionary<string, string> input)
    {
        var extra = new Dictionary<string, string>
        {
            { "clientAccount", input["PayAccount"] }
        };
        
        return CreateDeposit<DepositDataAuto>(input, Path.CreateDepositAuto, extra);
    }

    
    private IDepositData CreateDepositQr(IDictionary<string, string> input)
    {
        var extra = new Dictionary<string, string>
        {
            { "clientAccount", input["PayAccount"] }
        };
        
        return CreateDeposit<DepositDataQr>(input, Path.CreateDepositQr, extra);
    }
    
    private IDepositData CreateDepositTrue(IDictionary<string, string> input)
    {
        var extra = new Dictionary<string, string>
        {
            { "clientAccount", input["PayAccount"] }
        };
        
        return CreateDeposit<DepositDataTrue>(input, Path.CreateDepositTrue, extra);
    }

    private IDepositData CreateDepositPayway(IDictionary<string, string> input)
    {
        var (_, type) = SplitPayCode(input["PayCode"]);
        var extra = new Dictionary<string, string>
        {
            { "paymentType", type }
        };
        
        return CreateDeposit<DepositDataPayway>(input, Path.CreateDepositPayway, extra);
    }
    
    private IDepositData CreateDepositKHQr(IDictionary<string, string> input)
        => CreateDeposit<DepositDataQr>(input, Path.CreateDepositKHQr);
    
    private IDepositData CreateDeposit<TResult>(IDictionary<string, string> input, string path, IDictionary<string, string>? extra = null)
        where TResult: IDepositData
    {
        var (method, _) = SplitPayCode(input["PayCode"]);
        var currencyId = input["CurrencyId"];
        var apiHost = input[$"{currencyId}_{method}ApiHost"];
        
        var req = new Dictionary<string, string>
        {
            { "requestTime", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "amount", input["Amount"] },
            { "clientAccountName", input["AccountName"] },
            { "reference1", input["TransID"] }
        };

        if (extra != null)
        {
            req.AddRange(extra);
        }
        
        var authToken = GetAuthToken(input);
        
        var (result, statusCode) = _httpHelper
            .WithOAuthBearerToken(authToken)
            .PostJson<Dictionary<string, string>, PayResp<TResult>>(apiHost + path, req);

        if (!statusCode.IsSuccessStatus())
            throw new CustomException($"HttpStatus: {(int)statusCode} {statusCode}");

        if (!result.Status.Equals(Definition.SuccessStatus))
            throw new CustomException(result.Message);

        return result.Data;
    }
    
    private static (string method, string type) SplitPayCode(string payCode)
    {
        var result = payCode.Split('_');
        
        return (result[0], result.Length > 1 ? result[1] : string.Empty);
    }
}