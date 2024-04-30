using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PayPlus;

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
        var (payType, payModel) = ParsePayCode(inputs["PayCode"]);
        var reqAmount = decimal.Parse(inputs["Amount"]);

        try
        {
            var req = new PayPlusReq<DepositReq>
            {
                Action = "CardPayMent",
                ActionValue = new DepositReq
                {
                    TransId = inputs["TransID"],
                    PayType = payType,
                    PayModel = payModel,
                    UserName = inputs["UserName"],
                    Currency = inputs["PayCurrencyId"],
                    Amount = (reqAmount * AmountFix).ToString("G29"),
                    CallbackUrl = inputs["CallBackUrl"],
                    MerchantNo = inputs["MerchantNo"]
                }
            };
            
            var (response, statusCode) = _httpHelper
                .PostJson<PayPlusReq<DepositReq>, PayPlusOutResp>(inputs["Host"], req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PayPlus deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Code.Equals(200))
            {
                throw new CustomException(response.Message);
            }

            var resp = JsonConvert.DeserializeObject<PayPlusResp<DepositResp>>(response.Data);
            
            if (resp == null)
            {
                throw new CustomException("PayPlus deposit resp is null.");
            }
            
            if (!resp.Meta.Success)
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new DepositResult
            {
                Success = true,
                Url = resp.Data.Url
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
        throw new NotImplementedException();
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var reqAmount = decimal.Parse(inputs["Amount"]);
        
        try
        {
            var req = new PayPlusReq<PayoutReq>
            {
                Action = "PaymentCreateOrder",
                ActionValue = new PayoutReq
                {
                    TransId = inputs["TransId"],
                    SubjectType = "TO_PRIVATE",
                    Currency = inputs["PayCurrencyId"],
                    Amount = (reqAmount * AmountFix).ToString("G29"),
                    CallbackUrl = inputs["CallBackUrl"],
                    MerchantNo = inputs["MerchantNo"],
                    AccountName = inputs["AccountName"],
                    AccountCode = inputs["AccountNo"]
                }
            };

            var (response, statusCode) = _httpHelper
                .PostJson<PayPlusReq<PayoutReq>, PayPlusOutResp>(inputs["Host"], req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"PayPlus withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Code.Equals(200))
            {
                throw new CustomException(response.Message);
            }

            var resp = JsonConvert.DeserializeObject<PayPlusResp<object>>(response.Data);
            
            if (resp == null)
            {
                throw new CustomException("PayPlus withdrawal resp is null.");
            }
            
            if (!resp.Meta.Success)
            {
                throw new CustomException(resp.Meta.Message);
            }

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = inputs["TransId"],
                RawContent = response.Data
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                WithdrawId = inputs["TransId"],
                Message = ex.Message
            };
        }
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
    
    private static string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
    {
        var str = string.Join('&', input
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}"));

        var sign = RsaEncryptWithPublic(str, key);

        return sign;
    }

    private static (string payType, string payModel) ParsePayCode(string payCode)
    {
        var tmp = payCode.Split('-');
        var payModel = tmp.Length > 1 ? tmp[1] : string.Empty;

        return (tmp[0], payModel);
    }
}