using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PapayaPay;

public class PayHelper: PayHelperBase, IPayHelperNoCallBackUrl
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
        var baseApiUrl = input["Host"];
        var token = input["Token"];
        var transId = input["TransID"];
    
        try
        {
            var req = new Dictionary<string, string>
            {
                { "qrCodeTransactionId", transId },
                { "amount", GetRequestAmount(input["Amount"]) },
                { "currency", input["PayCurrencyId"] },
                { "bankCode", input["BankCode"] },
                { "payMethod", input["PayCode"] },
                { "accountNumber", input["PayAccount"] },
                { "accountName", input["AccountName"] },
                { "description", string.Empty }
            };
            
            var (response, statusCode) = _httpHelper
                .WithHeader("transactiontoken", token)
                .PostJson<Dictionary<string, string>, DepositResponse>($"{baseApiUrl}{Path.Deposit}", req);
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {statusCode}", ((int) statusCode).ToString());
            }
    
            if (response.StatusCode != Definition.SuccessCode)
            {
                throw new CustomException(response.Message, response.StatusCode);
            }
    
            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = response.Url
            };
        }
        catch (CustomException ex)
        {
            _logger.LogError($"PapayaPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");
    
            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var req = FromJson<DepositCallbackRequest>(bodyContent);
        
        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = req.Amount,
            ThirdId = req.TransId,
            Comment = bodyContent,
            TransStatus = Definition.GetDepositTransStatus(req.Status),
            Response = ToJson(new DepositCallbackResponse
            {
                TransId = req.TransId,
                Status = 200,
                Message = "Success"
            })
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
        var baseApiUrl = input["Host"];
        var token = input["Token"];
        var transId = input["TransID"];
    
        try
        {
            var req = new Dictionary<string, string>
            {
                { "currencyCode", input["PayCurrencyId"] },
                { "fundOutPaymentReference", transId },
                { "accountNumber", input["AccountNo"] },
                { "accountName", input["AccountName"] },
                { "bankCode", input["BankCode"] },
                { "bankName", input["BankName"] },
                { "amount", GetRequestAmount(input["Amount"]) }
            };
            
            var (response, statusCode) = _httpHelper
                .WithHeader("transactiontoken", token)
                .PostJson<Dictionary<string, string>, WithdrawalResponse>($"{baseApiUrl}{Path.Withdrawal}", req);
    
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {(int) statusCode}", response.Message);
            }
            
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = response.Message,
                RawContent = JsonConvert.SerializeObject(response)
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");
    
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
        var req = FromJson<PayoutCallbackRequest>(bodyContent);
    

        return new NotifyWithdrawResult
        {
            ThirdId = req.TransId,
            TransStatus = Definition.GetPayoutTransStatus(req.Status),
            Message = req.Status,
            Response = ToJson(new PayoutCallbackResponse
            {
                Status = 200,
                Message = "Success"
            }),
            WithdrawId = req.TransId
        };
    }
    
    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Private

    private static string GetRequestAmount(string strAmount)
        => decimal.Parse(strAmount).ToString("F2");
    
    #endregion

    public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
    {
        if (formData["CurrentCallbackType"].Equals("Deposit"))
        {
            var reqD = FromJson<DepositCallbackRequest>(bodyContent);
            
            return reqD.TransId;
        }
        
        var reqW = FromJson<PayoutCallbackRequest>(bodyContent);
            
        return reqW.TransId;
    }
}