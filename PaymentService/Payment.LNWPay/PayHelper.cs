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
            {"True", CreateDepositTrue}
        };
    }
    
    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var payCode = inputs["PayCode"];
        
        try
        {
            if (!_depositMapper.ContainsKey(payCode))
                throw new CustomException($"Not support deposit type. PayCode={payCode}");

            var resp = _depositMapper[payCode].Invoke(inputs);

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
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var data = FromJson<CallBackDebitReq>(bodyContent);
        var transId = data.TransId;
        var str = payResource[$"{data.Currency}DepositSecretKey"] + data.TransId;
        var sign = ToMD5(str).ToLower();

        if (!data.Token.Equals(sign))
        {
            _logger.LogError($"{transId}, signature verification failed.");

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
            ActualAmount = decimal.Parse(data.Amount),
            DepositId = transId,
            ThirdId = transId,
            TransStatus = TransStatus.Success,
            Response = CallbackResponse.Success
        };

        return result;
    }
    
    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiHost = inputs["Host"];
        var payCurrency = inputs["PayCurrencyId"];
        
        try
        {
            var req = new Dictionary<string, string>
            {
                {"username", inputs[$"{payCurrency}PayoutUsername"]},
                {"api_key", inputs[$"{payCurrency}PayoutApiKey"]}
            };
            
            var authResp = GetAuthToken(apiHost, req);
            var amount = decimal.Parse(inputs["Amount"]);
            req = new Dictionary<string, string>
            {
                { "auth", authResp.Auth },
                { "amount", amount.ToString("G29") },
                { "currency", inputs["PayCurrencyId"] },
                { "orderid", inputs["TransID"] },
                { "bank_id", inputs["BankCode"] },
                { "bank_branch", "" },
                { "holder_name", inputs["AccountName"] },
                { "account_no", inputs["AccountNo"] }
            };
            
            var (resp, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, WithdrawResp>(apiHost + Path.CreatePayout, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(
                    $"Create payout orders failed, HttpStatus: {(int)statusCode} {statusCode}");

            if (!resp.Status)
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

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var data = FromJson<CallBackCreditReq>(bodyContent);
        var transId = data.TransId;
        var str = payResource[$"{data.Currency}PayoutSecretKey"] + data.TransId;
        var sign = ToMD5(str).ToLower();

        if (!data.Token.Equals(sign))
        {
            _logger.LogError($"{transId}, signature verification failed.");

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
            ThirdId = transId,
            Message = data.Remarks,
            TransStatus = TransStatus.Success,
            Response = CallbackResponse.Success
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

    public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
    {
        var data = FromJson<CallBackDebitReq>(bodyContent);
        return data.TransId;
    }

    private string GetAuthToken(IDictionary<string, string> input)
    {
        var payCode = input["PayCode"];
        var apiUrl = input[$"{payCode}ApiUrl"];
        var apiToken = input[$"{payCode}Token"];
        
        var token = ToBase64Encode($"{input["Prefix"]}:{apiToken}:{DateTimeOffset.Now:yyyyMMddHHmmss}");
        
        var (result, statusCode) = _httpHelper
            .WithOAuthBearerToken(token)
            .Get<PayResp<AuthData>>(apiUrl + Path.Auth, string.Empty);

        if (!statusCode.IsSuccessStatus())
            throw new CustomException(
                $"Get auth token failed, HttpStatus: {(int)statusCode} {statusCode}");

        if (!result.Status.Equals(Definition.SuccessStatus))
            throw new CustomException("Get auth token failed, msg=" + result.Message);

        return result.Data.Token;
    }
    
    private IDepositData CreateDepositAuto(IDictionary<string, string> input)
    {
        var result = CreateDeposit<DepositDataAuto>(input, 
            new DepositReq
            {
                
            });
        
        return result;
    }
    
    private IDepositData CreateDepositQr(IDictionary<string, string> input)
    {
        var result = CreateDeposit<DepositDataQr>(input, 
            new DepositReq
            {
                
            });
        
        return result;
    }
    
    private IDepositData CreateDepositTrue(IDictionary<string, string> input)
    {
        var result = CreateDeposit<DepositDataTrue>(input, 
            new DepositReq
            {
                
            });
        
        return result;
    }
    
    private IDepositData CreateDeposit<TResult>(IDictionary<string, string> input, DepositReq req)
        where TResult: IDepositData
    {
        var payCode = input["PayCode"];
        var apiUrl = input[$"{payCode}ApiUrl"];
        
        var authToken = GetAuthToken(input);
        
        var (result, statusCode) = _httpHelper
            .WithOAuthBearerToken(authToken)
            .PostJson<DepositReq, PayResp<TResult>>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
            throw new CustomException(
                $"Create deposit failed, HttpStatus: {(int)statusCode} {statusCode}");

        if (!result.Status.Equals(Definition.SuccessStatus))
            throw new CustomException("Create deposit failed, msg=" + result.Message);

        return result.Data;
    }
}