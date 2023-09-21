using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Packaging;
using PaymentService.Exceptions;
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
    private readonly ICacheHelper _redisHelper;
    private readonly string _callBackUrl;

    private readonly IDictionary<WithdrawalType, Func<WithdrawalRequest, Customer, (IPayInfo, Dictionary<string, string>)>>
        _getWithdrawalInputMapper;

    public WithdrawalV2Controller(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        _outgoingIp = configuration.GetValue<string>("OutgoingIp");
        _callBackUrl = configuration.GetValue<string>("CallBack");
        _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
        _payBll = provider.GetRequiredService<PayBLL>();
        _logger = provider.GetRequiredService<ILogger<WithdrawalV2Controller>>();
        _common = provider.GetRequiredService<Common>();
        _paymentBll = provider.GetRequiredService<PaymentBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        
        #region ActionMapper

        _getWithdrawalInputMapper = new Dictionary<WithdrawalType, Func<WithdrawalRequest, Customer, (IPayInfo, Dictionary<string, string>)>>
        {
            {WithdrawalType.OnlineBanking, GetGenericInput},
            {WithdrawalType.EWalletPayment, GetGenericInput},
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
            _logger.LogError($"Exception: Method={GetCurrentMethod()}, TransId={req.TransId}, Message={ex.Message}");
            
            return new ApiResult<PayResult>(ErrorCode.Fail);
        }
    }

    #region GetWithdrawalInput

    private (IPayInfo, Dictionary<string, string>) GetGenericInput(WithdrawalRequest withdrawalInfo, Customer custInfo)
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
    
    private (IPayInfo, Dictionary<string, string>) GetCryptoPaymentInput(WithdrawalRequest withdrawalInfo, Customer custInfo)
    {
        var keys = new[] {"ChannelId"};
        var dicWithdrawalInfo = GetDicWithdrawalInfo(withdrawalInfo.WithdrawalInfo, keys);
        var channelId = int.Parse(dicWithdrawalInfo["ChannelId"]);
    
        var payInfo = _payBll.GetCryptoListForPayout(custInfo.SiteId)?.FirstOrDefault(x =>
            x.PayId == withdrawalInfo.PayId
            && x.ChannelId == channelId
            && x.CurrencyId == custInfo.CurrencyId);
            
        if (payInfo == null)
        {
            throw new PaymentException(
                $"WithdrawalType={withdrawalInfo.WithdrawalType}, PayId={withdrawalInfo.PayId}, ChannelId={channelId}, CurrencyId={custInfo.CurrencyId}, no support channel.",
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
    
    /*private (string PayName, Dictionary<string, string> DicParams) GetPayResource(DepositRequest depositRequest)
    {
        var transId = depositRequest.TransId;
        var custId = depositRequest.CustId;
        var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
        var pay = JsonConvert.DeserializeObject<DepositPayInfo>(depositRequest.DepositInfo);
        
        if (pay == null)
        {
            throw new PaymentException($"TransId={transId} pay info is null.");
        }
        
        var paySetting = _payBll.GetPaymentAllList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == pay.PayId);
        
        if (paySetting == null || string.IsNullOrEmpty(paySetting.Setting))
        {
            throw new PaymentException($"TransId={transId} pay setting is null.");
        }
        
        var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
        dic.Add("TransID", depositRequest.TransId);
        dic.Add("DepositInfo", depositRequest.DepositInfo);
        dic.Add("DepositType", ((int)depositRequest.DepositType).ToString());
        dic.Add("RequestAmount", depositRequest.Amount.ToString("G29"));
        dic.Add("CurrencyId", depositRequest.CurrencyId.ToString());

        if (!dic.ContainsKey("PayName"))
        {
            throw new PaymentException($"TransId={transId} PayName is not exist.");
        }

        if (!_common.IsWhiteList(_common.GetClientIP(HttpContext), paySetting.PayName, transId, GetCurrentMethod()))
        {
            throw new PaymentException($"{paySetting.PayName} requested of {transId} from {_common.GetClientIP(HttpContext)} is Invalid IP!");
        }

        return (paySetting.PayName, dic);
    }*/

    #endregion
}