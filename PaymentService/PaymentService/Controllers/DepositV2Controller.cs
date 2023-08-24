using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PaymentCore.Interface;
using PaymentService.Exceptions;
using PaymentService.Service.BLL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers;

[ApiController]
[Route("Payment/Deposit")]
public class DepositV2Controller : ControllerBase
{
    private readonly PayBLL _payBll;
    private readonly CustInfoBLL _custInfoBll;
    private readonly ILogger<DepositController> _logger;
    private readonly PaymentBLL _paymentBll;
    private readonly Common _common;
    private readonly string _callBackUrl;

    private readonly IDictionary<DepositType, Func<DepositInfoModel, Customer, (IPayInfo, Dictionary<string, string>)>>
        _getDepositInputMapper;
    
    public DepositV2Controller(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        _payBll = provider.GetRequiredService<PayBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
        _logger = provider.GetRequiredService<ILogger<DepositController>>();
        _paymentBll = provider.GetRequiredService<PaymentBLL>();
        _common = provider.GetRequiredService<Common>();
        _callBackUrl = configuration.GetValue<string>("CallBack");

        #region ActionMapper

        _getDepositInputMapper = new Dictionary<DepositType, Func<DepositInfoModel, Customer, (IPayInfo, Dictionary<string, string>)>>
        {
            {DepositType.OnlineBanking, GetOnlineBankingDepositInput},
            {DepositType.BankAutoChannel, GetOnlineBankingDepositInput},
            {DepositType.MobilePay, GetOnlineBankingDepositInput},
            {DepositType.CryptoPay, GetOnlineBankingDepositInput},
            {DepositType.LoadCard, GetOnlineBankingDepositInput},
            {DepositType.QrPay, GetOnlineBankingDepositInput}
        };

        #endregion
        
    }

    public class DepositReq
    {
        public string TransId { get; set; }
        public string ClientIP { get; set; }
        public string ReturnUrl { get; set; }
    }

    [HttpPost]
    public ApiResult<PayResult> Create(DepositReq req)
    {
        var depositInfo = _paymentBll.GetDepositInfo(req.TransId);
        var custInfo = _custInfoBll.GetCustInfoWithCache(depositInfo.CustId);

        try
        {
            if (depositInfo.Amount % 1 != 0)
            {
                throw new PaymentException($"Invalid deposit amount: {depositInfo.Amount}", ErrorCode.AmountOutOfRange);
            }
            
            var (payInfo, input) =
                _getDepositInputMapper[depositInfo.DepositType].Invoke(depositInfo, custInfo);
            
            var (pay, payHelper) = GetPayHelper(payInfo, custInfo);
            var payLang = GetPayLanguage(payInfo.PayId, custInfo);
            
            input.Add("PayLangId", payLang);
            input.Add("TransID", req.TransId);
            input.Add("CurrencyId", depositInfo.CurrencyId.ToString());
            input.Add("PayCurrencyId", payInfo.PayCurrencyId);
            input.Add("PayCode", payInfo.PayCode);
            input.Add("Amount", depositInfo.Amount.ToString("G29"));
            input.Add("CustId", custInfo.CustId.ToString());
            input.Add("UserName", custInfo.UserName);
            input.Add("AccountName", custInfo.AccountName);
            input.Add("ClientIP", req.ClientIP);
            input.Add("ReturnUrl", req.ReturnUrl);
            input.Add("CallBackUrl", $"{_callBackUrl}/Payment/Deposit/CallBack/{req.TransId}");
            input.Add("IsTest", _common.IsTest());
            input.Add("DepositInfo", depositInfo.DepositInfo);
            
            var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            input.Append<>(setting);
            
            var result = payHelper.Debit(input);
            
            if (result.Success)
            {
                var payResult = new PayResult
                {
                    Url = result.Url,
                    FormData = result.FormData,
                    ThirdId = result.ThirdId,
                    Mode = payInfo.Mode,
                    Message = result.Message
                };

                if (!string.IsNullOrEmpty(result.ThirdId))
                {
                    _paymentBll.SetThirdId(req.TransId, result.ThirdId);
                }

                return new ApiResult<PayResult>(payResult);
            }
            
            _paymentBll.DepositFail(, );
            
            return new ApiResult<PayResult>(ErrorCode.Fail)
            {
                RawErrorCode = result.ErrorCode,
                RawMessage = result.Message
            };
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
    
    [Route("CallBack/{TransId}")]
    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> CallBackAsync(string transId)
    {
        try
        {
            return Content(ErrorCode.Success.ToString());
        }
        catch (PaymentException ex)
        {
            _logger.LogInformation($"Method={GetCurrentMethod()}, TransId={transId}, Code={ex.Code}, Message={ex.Message}");
            
            return Content(ErrorCode.Fail.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: Method={GetCurrentMethod()}, TransId={transId}, Message={ex.Message}");
            
            return Content(ErrorCode.SystemError.ToString());
        }
    }

    #region GetDepositInput

   
    
    private (IPayInfo, Dictionary<string, string>) GetOnlineBankingDepositInput(DepositInfoModel depositInfo, Customer custInfo)
    {
        var onlineBankList = _payBll.GetOnlineBankList(custInfo.SiteId, depositInfo.DepositType);
        var vipLevel = _custInfoBll.GetPointLevelWithCache(custInfo.CustId);
        var keys = new[] {"PayId", "SysId"};
        var di = GetDicDepositInfo(depositInfo.DepositInfo, keys);
        var payId = int.Parse(di["PayId"]);
        var sysId = int.Parse(di["SysId"]);

        var payInfo = onlineBankList?.OrderByDescending(x => x.CurrencyId)
            .FirstOrDefault(x =>
                x.PayId == payId && (sysId == x.SysId || !x.SelBank) &&
                x.PointLevel <= vipLevel &&
                x.CurrencyId == custInfo.CurrencyId);
        
        if (payInfo == null)
        {
            throw new PaymentException($"PayId={payId}, SysId={sysId}, no support bank.", ErrorCode.BankNotFound);
        }

        var input = new Dictionary<string, string>
        {
            {"BankName", payInfo.BankName},
            {"BankCode", payInfo.BankCode},
        };

        return (payInfo, input);
    }
    

    #endregion

    #region Private
    
    

    private (Pay, IPayHelper) GetPayHelper(IPayInfo payInfo, Customer custInfo)
    {
        var pay = _payBll.GetPaymentList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == payInfo.PayId);
        
        if (pay == null)
        {
            throw new PaymentException($"{payInfo.PayName} not found pay setting.", ErrorCode.SettingNotFound);
        }
        
        var payHelper = _payBll.GetPayHelper(pay.PayName);
        
        if (payHelper == null)
        {
            throw new PaymentException($"{payInfo.PayName} not found PayHelper setting.", ErrorCode.SettingNotFound);
        }

        return (pay, payHelper);
    }

    private string GetPayLanguage(int payId, Customer custInfo)
    {
        var payLangId = _payBll.GetPayLanguage()
            ?.FirstOrDefault(x => 
                string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) 
                && x.PayId == payId)?.PayLangId;

        return payLangId;
    }
    
    private static IDictionary<string, string> GetDicDepositInfo(string json, IEnumerable<string> keys)
    {
        var depositInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        
        if (depositInfo == null)
        {
            throw new PaymentException("Missing depositInfo.", ErrorCode.Fail);
        }

        foreach (var key in keys)
        {
            if (!depositInfo.ContainsKey(key))
            {
                throw new PaymentException($"DepositInfo missing key: {key}", ErrorCode.Fail);
            }
        }

        return depositInfo;
    }

    #endregion
}