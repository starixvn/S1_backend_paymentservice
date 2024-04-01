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
        // 這家callback沒有驗簽
        return new NotifyDepositResult
        {
            ActualAmount = decimal.Parse(formData["amount"]),
            DepositId = formData["ref1"],
            ThirdId = formData["Id"],
            TransStatus = Definition.GetTransStatus(formData["status"]),
            Response = Definition.CallbackResponse
        };
    }
    
    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        var payCode = input["PayCode"];
        var apiUrl = input[$"{payCode}ApiUrl"];
        
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
                .PostJson<PayoutReq, PayResp<PayoutData>>(apiUrl + Path.CreatePayout, req);

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

    public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
    {
        return formData["ref1"];
    }

    private string GetAuthToken(IDictionary<string, string> input)
    {
        var payCode = input["PayCode"];
        var apiUrl = input[$"{payCode}ApiUrl"];
        var apiToken = input[$"{payCode}Token"];

        var str = $"{input["Prefix"]}:{apiToken}:{DateTimeOffset.Now:yyyyMMddHHmmss}";
        var token = ToBase64Encode(str);
        _logger.LogInformation("token orig str: " + str);
        
        var (result, statusCode) = _httpHelper
            .WithOAuthBearerToken(token)
            .PostJson<string, PayResp<AuthData>>(apiUrl + Path.Auth, string.Empty);

        if (!statusCode.IsSuccessStatus())
            throw new CustomException(
                $"Get auth token failed, HttpStatus: {(int)statusCode} {statusCode}");

        if (!result.Status.Equals(Definition.SuccessStatus))
            throw new CustomException("Get auth token failed, msg=" + result.Message);

        return result.Data.Token;
    }
    
    private IDepositData CreateDepositAuto(IDictionary<string, string> input)
        => CreateDeposit<DepositDataAuto>(input, Path.CreateDepositAuto);

    
    private IDepositData CreateDepositQr(IDictionary<string, string> input)
        => CreateDeposit<DepositDataQr>(input, Path.CreateDepositQr);
    
    private IDepositData CreateDepositTrue(IDictionary<string, string> input)
        => CreateDeposit<DepositDataTrue>(input, Path.CreateDepositTrue);
    
    private IDepositData CreateDeposit<TResult>(IDictionary<string, string> input, string path)
        where TResult: IDepositData
    {
        var payCode = input["PayCode"];
        var apiUrl = input[$"{payCode}ApiUrl"];
        
        var req = new DepositReq
        {
            Amount = decimal.Parse(input["Amount"]),
            ClientAccount = input["PayAccount"],
            ClientAccountName = input["AccountName"],
            TransId = input["TransID"]
        };
        
        var authToken = GetAuthToken(input);
        
        var (result, statusCode) = _httpHelper
            .WithOAuthBearerToken(authToken)
            .PostJson<DepositReq, PayResp<TResult>>(apiUrl + path, req);

        if (!statusCode.IsSuccessStatus())
            throw new CustomException(
                $"Create deposit failed, HttpStatus: {(int)statusCode} {statusCode}");

        if (!result.Status.Equals(Definition.SuccessStatus))
            throw new CustomException("Create deposit failed, msg=" + result.Message);

        return result.Data;
    }
}