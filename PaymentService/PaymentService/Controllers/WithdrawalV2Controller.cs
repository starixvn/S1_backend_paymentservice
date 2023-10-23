using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Packaging;
using PaymentCore;
using PaymentCore.Interface;
using PaymentService.Exceptions;
using PaymentService.Extensions;
using PaymentService.Service.BLL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers;

[ApiController]
public class WithdrawalV2Controller : ControllerBase
{
    private readonly CustInfoBLL _custInfoBll;
    private readonly PayBLL _payBll;
    private readonly string _outgoingIp;
    private readonly ILogger<WithdrawalV2Controller> _logger;
    private readonly Common _common;
    private readonly PaymentBLL _paymentBll;
    private readonly WhitelistBLL _whitelistBll;
    private readonly ICacheHelper _redisHelper;
    private readonly string _callBackUrl;

    private readonly IDictionary<WithdrawalType, Func<WithdrawalRequest, Customer, (IPayInfo, Dictionary<string, string>)>>
        _getWithdrawalInputMapper;

    public WithdrawalV2Controller(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _outgoingIp = configBll.GetConfigValue("OutgoingIp");
        _callBackUrl = configBll.GetConfigValue("CallBack");
        _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
        _payBll = provider.GetRequiredService<PayBLL>();
        _logger = provider.GetRequiredService<ILogger<WithdrawalV2Controller>>();
        _common = provider.GetRequiredService<Common>();
        _paymentBll = provider.GetRequiredService<PaymentBLL>();
        _whitelistBll = provider.GetRequiredService<WhitelistBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        
        #region ActionMapper

        _getWithdrawalInputMapper = new Dictionary<WithdrawalType, Func<WithdrawalRequest, Customer, (IPayInfo, Dictionary<string, string>)>>
        {
            {WithdrawalType.OnlineBanking, GetOnlineBankingInput},
            {WithdrawalType.EWalletPayment, GetEWalletPaymentInput},
            {WithdrawalType.CryptoPayment, GetCryptoPaymentInput}
        };

        #endregion
    }
    
    public class WithdrawalReq
    {
        public string TransId { get; set; }
    }
    
    [Route("Payment/Withdrawal/Create")]
    [HttpPost]
    public ApiResult Create(WithdrawalReq req)
    {
        var withdrawalInfo = _paymentBll.GetWithdrawalRequest(req.TransId);
        var custInfo = _custInfoBll.GetCustInfoWithCache(withdrawalInfo.CustId);

        try
        {
            var (payInfo, input) =
                _getWithdrawalInputMapper[withdrawalInfo.WithdrawalType].Invoke(withdrawalInfo, custInfo);
            
            var (pay, payHelper) = _payBll.GetPayHelper(payInfo, custInfo);
            var payLang = _payBll.GetPayLanguage(payInfo.PayId, custInfo);
            
            input.Add("PayLangId", payLang);
            input.Add("TransID", req.TransId);
            input.Add("CurrencyId", withdrawalInfo.CurrencyId.ToString());
            input.Add("PayCurrencyId", payInfo.PayCurrencyId);
            input.Add("PayCode", payInfo.PayCode);
            input.Add("Amount", withdrawalInfo.Amount.ToString("G29"));
            input.Add("CustId", custInfo.CustId.ToString());
            input.Add("UserName", custInfo.UserName);
            input.Add("AccountName", custInfo.AccountName);
            input.Add("AccountNo", withdrawalInfo.AccountNo);
            input.Add("Lang", custInfo.UserLang);
            input.Add("ClientIP", _common.GetClientIP(HttpContext));
            input.Add("OutgoingIp", _outgoingIp);
            input.Add("PhoneNo", custInfo.PhoneNo);
            input.Add("PhoneVerified", custInfo.PhoneVerified.ToString());
            input.Add("AreaCode", custInfo.AreaCode);
            input.Add("CallBackUrl", $"{_callBackUrl}/Payment/WithdrawalCallBack/{req.TransId}");
            input.Add("IsTest", _common.IsTest());
            input.Add("WithdrawalInfo", withdrawalInfo.WithdrawalInfo);
            input.Add("WithdrawalType", ((int)withdrawalInfo.WithdrawalType).ToString());
            
            var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            input.AddRange(setting);
            
            var result = payHelper.Credit(input);
            
            if (!string.IsNullOrEmpty(result.WithdrawalInfo))
            {
                _paymentBll.UpdateWithdrawalInfo(req.TransId, result.WithdrawalInfo);
            }

            return new ApiResult(result.Success
                ? ErrorCode.Success
                : ErrorCode.PayOutFail, result.Message);
        }
        catch (PaymentException ex)
        {
            _logger.LogInformation($"Method={GetCurrentMethod()}, TransId={req.TransId}, Code={ex.Code}, Message={ex.Message}");
            
            return new ApiResult<PayResult>(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: Method={GetCurrentMethod()}, TransId={req.TransId}, Message={ex.Message}", ex);
            
            return new ApiResult<PayResult>(ErrorCode.Fail);
        }
    }
    
    [Route("Payment/WithdrawalCallBack/{TransId}")]
    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> CallBackAsync(string transId)
    {
        try
        {
            var (headers, bodyContent, dicParams) = await HttpExtension.GetCallbackInfo(Request);
            
            _logger.LogInformation(
                $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, Url={Request.Path}, dicParams={JsonConvert.SerializeObject(dicParams)}, bodyContent={bodyContent}");

            var withdrawalId = _common.GetId(transId);
            var tmpTransId = $"W{_common.GetEnv()}{withdrawalId}";
            if (tmpTransId != transId)
            {
                var payHelperNoCallBackUrl = (IPayHelperNoCallBackUrl)_payBll.GetPayHelper(transId);
                transId = payHelperNoCallBackUrl.FetchTransId(dicParams, bodyContent);
            }
            
            var responseToPay = CallBackHandler(transId, dicParams, bodyContent, headers);
            return Content(responseToPay);
        }
        catch (PaymentException ex)
        {
            _logger.LogInformation($"Method={GetCurrentMethod()}, TransId={transId}, Code={ex.Code}, Message={ex.Message}");
            
            return Content(ErrorCode.Fail.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: Method={GetCurrentMethod()}, TransId={transId}, Message={ex.Message}", ex);
            
            return Content(ErrorCode.SystemError.ToString());
        }
    }

    #region GetWithdrawalInput

    private (IPayInfo, Dictionary<string, string>) GetOnlineBankingInput(WithdrawalRequest withdrawalInfo, Customer custInfo)
    {
        var keys = new[] {"SysId", "IFSCCode"};
        var dicWithdrawalInfo = GetDicWithdrawalInfo(withdrawalInfo.WithdrawalInfo, keys);
        var sysId = int.Parse(dicWithdrawalInfo["SysId"]);
    
        var payInfo = _payBll.GetPayListForPayout(withdrawalInfo.WithdrawalType, custInfo.SiteId)?.FirstOrDefault(x =>
            x.PayId == withdrawalInfo.PayId
            && x.SysId == sysId
            && x.CurrencyId == custInfo.CurrencyId);
        
        if (payInfo == null)
        {
            throw new PaymentException(
                $"WithdrawalType={withdrawalInfo.WithdrawalType}, PayId={withdrawalInfo.PayId}, SysId={sysId}, CurrencyId={custInfo.CurrencyId}, no support channel.",
                ErrorCode.ChannelNotFound);
        }
    
        var input = new Dictionary<string, string>
        {
            {"BankName", payInfo.BankName},
            {"BankCode", payInfo.BankCode},
            {"IFSCCode", dicWithdrawalInfo["IFSCCode"]}
        };
    
        return (payInfo, input);
    }
    
    private (IPayInfo, Dictionary<string, string>) GetEWalletPaymentInput(WithdrawalRequest withdrawalInfo, Customer custInfo)
    {
        var keys = new[] {"ChannelId"};
        var dicWithdrawalInfo = GetDicWithdrawalInfo(withdrawalInfo.WithdrawalInfo, keys);
        var channelId = int.Parse(dicWithdrawalInfo["ChannelId"]);
    
        var payInfo = _payBll.GetEWalletPayListForPayout(custInfo.SiteId)?.FirstOrDefault(x =>
            x.ChannelId == channelId
            && x.CurrencyId == custInfo.CurrencyId);
        
        if (payInfo == null)
        {
            throw new PaymentException(
                $"WithdrawalType={withdrawalInfo.WithdrawalType}, ChannelId={channelId}, CurrencyId={custInfo.CurrencyId}, no support channel.",
                ErrorCode.ChannelNotFound);
        }
    
        var input = new Dictionary<string, string>
        {
            {"BankName", payInfo.EWalletName},
            {"BankCode", payInfo.EWalletCode}
        };
    
        return (payInfo, input);
    }
    
    private (IPayInfo, Dictionary<string, string>) GetCryptoPaymentInput(WithdrawalRequest withdrawalInfo, Customer custInfo)
    {
        var keys = new[] {"ChannelId"};
        var dicWithdrawalInfo = GetDicWithdrawalInfo(withdrawalInfo.WithdrawalInfo, keys);
        var channelId = int.Parse(dicWithdrawalInfo["ChannelId"]);
    
        var payInfo = _payBll.GetCryptoListForPayout(custInfo.SiteId)?.FirstOrDefault(x =>
            x.ChannelId == channelId
            && x.CurrencyId == custInfo.CurrencyId);
            
        if (payInfo == null)
        {
            throw new PaymentException(
                $"WithdrawalType={withdrawalInfo.WithdrawalType}, ChannelId={channelId}, CurrencyId={custInfo.CurrencyId}, no support channel.",
                ErrorCode.ChannelNotFound);
        }
    
        return (payInfo, new Dictionary<string, string>());
    }
    
    #endregion

    #region private methods

    private static IDictionary<string, string> GetDicWithdrawalInfo(string json, IEnumerable<string> keys)
    {
        var withdrawalInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        
        if (withdrawalInfo == null)
        {
            throw new PaymentException("Missing withdrawalInfo.", ErrorCode.Fail);
        }

        foreach (var key in keys)
        {
            if (!withdrawalInfo.ContainsKey(key))
            {
                throw new PaymentException($"WithdrawalInfo missing key: {key}", ErrorCode.Fail);
            }
        }

        return withdrawalInfo;
    }
    
    private string CallBackHandler(
        string transId, Dictionary<string, string> dicParams, string bodyContent, Dictionary<string, string> headers)
    {
        var lockKey = $"WithdrawalCallBackLock:{transId}";
        var lockValue = Guid.NewGuid().ToString();
        var lockExpiry = TimeSpan.FromSeconds(30);
        if (!_redisHelper.TryCreateLock(lockKey, lockValue, lockExpiry))
            return $"TransId={transId} is processing";
        try
        {
            var withdrawalInfo = _paymentBll.GetWithdrawalRequest(transId);
            
            if (withdrawalInfo == null)
            {
                throw new PaymentException($"TransId={transId} is not exist.");
            }

            if (!_whitelistBll.IsWhitelist(_common.GetClientIP(HttpContext), withdrawalInfo.PayName, transId, GetCurrentMethod()))
            {
                throw new PaymentException($"{withdrawalInfo.PayName} requested of {transId} from {_common.GetClientIP(HttpContext)} is Invalid IP!");
            }

            var dic = withdrawalInfo.ISetting ?? new Dictionary<string, string>();
            dic.Add("TransId", withdrawalInfo.TransId);
            dic.Add("WithdrawalType", ((int)withdrawalInfo.WithdrawalType).ToString());
            dic.Add("CurrencyId", withdrawalInfo.CurrencyId.ToString());
            
            var payHelper = _payBll.GetPayHelper(withdrawalInfo.PayName);
            var result = payHelper.CallBackCredit(dicParams, bodyContent, headers, dic);
            _logger.LogInformation($"CallBackCredit result={JsonConvert.SerializeObject(result)}");

            if (!string.Equals(transId, result.WithdrawId, StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentException ($"{transId} is not equal WithdrawId={result.WithdrawId}");
            }

            if (result.RequestAmount == 0)
                result.RequestAmount = withdrawalInfo.Amount;

            if (withdrawalInfo.Status is WithdrawalStatus.Approved or WithdrawalStatus.Rejected)
            {
                var status = result.TransStatus switch 
                {
                    TransStatus.Success => WithdrawalStatus.Approved,
                    TransStatus.Fail => WithdrawalStatus.Verify,
                    _ => WithdrawalStatus.Process
                };
                
                var message = $"{transId} status is currently {withdrawalInfo.Status.ToString()}, cannot change status to {status.ToString()}";
                
                _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {message}");
                _paymentBll.SetWithdrawalLog(transId, withdrawalInfo.SiteId,
                    message, (int)withdrawalInfo.Status, withdrawalInfo.PayName);

                return result.Response;
            }

            switch (result.TransStatus)
            {
                case TransStatus.Success when withdrawalInfo.Amount > 0:
                    _paymentBll.WithdrawalSuccess(withdrawalInfo, result);
                    break;
                
                case TransStatus.Fail:
                    _paymentBll.WithdrawalFail(withdrawalInfo, result);
                    break;

                case TransStatus.Pending:
                default:
                    _paymentBll.WithdrawalPending(withdrawalInfo, result);
                    break;
            }
            
            return result.Response;
        }
        finally
        {
            //key unlock
            _redisHelper.UnLock(lockKey, lockValue);
        }
    }

    #endregion
}