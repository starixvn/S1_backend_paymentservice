using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.IMPay;

public class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }
    
    public override DepositResult Debit(Dictionary<string, string> input)
    {
        try
        {
            var req = new Dictionary<string, string>
            {
                {"client_id", input["IMClientId"]},
                {"client_name", input["IMClientName"]},
                {"user_id", input["CustId"]},
                {"cust_name", input["AccountName"]},
                {"account_number", input["PayAccount"]},
                {"bank_name", input["BankCode"]},
                {"amount", input["Amount"]},
                {"return_success_url", input["ReturnUrl"]}
            };
            
            req.Add("hash_value", GetSign(req, input["Key"]));
            req.Add("payment_action", "qrText");
            req.Add("return_waiting_url", input["ReturnUrl"]);
            req.Add("callback_url", input["CallBackUrl"]);
        
            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, PayResp<DepositData>>(input["Host"] + Path.CreateDeposit, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(JsonConvert.SerializeObject(result.ErrorResp.Data));

            if (!result.Status)
                throw new CustomException(result.Message);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = result.Data.QrText,
                ThirdId = result.Data.ThirdId
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation("Create deposit failed: " + ex.Message);

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var req = FromJson<DepositCallbackReq>(bodyContent);
        
        return new NotifyDepositResult
        {
            ActualAmount = req.Amount,
            DepositId = payResource["TransID"],
            ThirdId = req.ThirdId,
            TransStatus = Definition.GetTransStatus(req.Status),
            Response = Definition.CallbackResponse
        };
    }
    
    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        try
        {
            var req = new Dictionary<string, string>
            {
                {"client_id", input["IMClientId"]},
                {"client_name", input["IMClientName"]},
                {"user_id", input["CustId"]},
                {"cust_name", input["AccountName"]},
                {"account_number", input["AccountNo"]},
                {"bank_name", input["BankCode"]},
                {"amount", input["Amount"]},
                {"callback_url", input["CallBackUrl"]}
            };
        
            req.Add("hash_value", GetSign(req, input["Key"]));
            
            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, PayResp<object>>(input["Host"] + Path.CreatePayout, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(JsonConvert.SerializeObject(result.ErrorResp.Data));

            if (!result.Status)
                throw new CustomException(result.Message);

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = input["TransID"],
                RawContent = JsonConvert.SerializeObject(result)
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation("Create payout failed: " + ex.Message);

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
        var req = FromJson<PayoutCallbackReq>(bodyContent);
        
        return new NotifyWithdrawResult
        {
            WithdrawId = payResource["TransId"],
            ThirdId = req.ThirdId,
            Message = req.Status,
            TransStatus = Definition.GetTransStatus(req.Status),
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
    
    private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
    {
        var str = string.Join("", param
                .Select(x => x.Value)) + key;

        _logger.LogInformation("str: " + str);
        var sign = ToSHA256(str).ToLower();

        return sign;
    }
}