using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentService.Exceptions;
using PaymentService.Extensions;
using PaymentService.Service.BLL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

using Serilog.Context;

using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers;

[ApiController]
public class DepositV2Controller : ControllerBase
{
    private readonly PayBLL _payBll;
    private readonly CustInfoBLL _custInfoBll;
    private readonly ILogger<DepositController> _logger;
    private readonly PaymentBLL _paymentBll;
    private readonly WhitelistBLL _whitelistBll;
    private readonly Common _common;
    private readonly ICacheHelper _redisHelper;
    private readonly string _callBackUrl;

    private readonly IDictionary<DepositType, Func<DepositInfoModel, Customer, (IPayInfo, Dictionary<string, string>)>>
        _getDepositInputMapper;

    public DepositV2Controller(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _payBll = provider.GetRequiredService<PayBLL>();
        _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
        _logger = provider.GetRequiredService<ILogger<DepositController>>();
        _paymentBll = provider.GetRequiredService<PaymentBLL>();
        _whitelistBll = provider.GetRequiredService<WhitelistBLL>();
        _common = provider.GetRequiredService<Common>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _callBackUrl = configBll.GetConfigValue("CallBack");

        #region ActionMapper

        _getDepositInputMapper = new Dictionary<DepositType, Func<DepositInfoModel, Customer, (IPayInfo, Dictionary<string, string>)>>
        {
            {DepositType.OnlineBanking, GetOnlineBankDepositInput},
            {DepositType.BankAutoChannel, GetOnlineBankDepositInput},
            {DepositType.QuickTransfer, GetOnlineBankDepositInput},
            {DepositType.MobilePay, GetMobilePayDepositInput},
            {DepositType.CryptoPay, GetGenericDepositInput},
            {DepositType.LoadCard, GetLoadCardDepositInput},
            {DepositType.QrPay, GetGenericDepositInput}
        };

        #endregion
    }

    public class DepositReq
    {
        public string TransId { get; set; }
        public string ClientIP { get; set; }
        public string ReturnUrl { get; set; }
        
        public DeviceTypeEnum DeviceType { get; set; }
    }

    [Route("Payment/Deposit/Create")]
    [HttpPost]
    public ApiResult<PayResult> Create(DepositReq req)
    {
        LogContext.PushProperty("TransId", req.TransId);
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

            var (pay, payHelper) = _payBll.GetPayHelper(payInfo, custInfo);
            var payLang = _payBll.GetPayLanguage(payInfo.PayId, custInfo);

            input.Add("PayLangId", payLang);
            input.Add("TransID", req.TransId);
            input.Add("CurrencyId", depositInfo.CurrencyId.ToString());
            input.Add("PayCurrencyId", payInfo.PayCurrencyId);
            input.Add("PayCode", payInfo.PayCode);
            input.Add("Amount", depositInfo.Amount.ToString("G29"));
            input.Add("CustId", custInfo.CustId.ToString());
            input.Add("UserName", custInfo.UserName);
            input.Add("AccountName", custInfo.AccountName);
            input.Add("Lang", custInfo.UserLang);
            input.Add("ClientIP", req.ClientIP);
            input.Add("ReturnUrl", req.ReturnUrl);
            input.Add("CallBackUrl", $"{_callBackUrl}/Payment/DepositCallBack/{req.TransId}");
            input.Add("IsTest", _common.IsTest());
            input.Add("DepositInfo", depositInfo.DepositInfo);
            input.Add("DeviceType", ((int) req.DeviceType).ToString());

            if (!input.ContainsKey("BankCode"))
            {
                input.Add("BankCode", string.Empty);
            }

            var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            input.AddRange(setting);

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

                if (!string.IsNullOrEmpty(result.DepositInfo))
                {
                    _paymentBll.UpdateDepositInfo(req.TransId, result.DepositInfo);
                }

                return new ApiResult<PayResult>(payResult);
            }

            _paymentBll.SimpleDepositFail(depositInfo.TransId, depositInfo.SiteId, payInfo, result.Message);

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
            _logger.LogError(ex, $"Exception: Method={GetCurrentMethod()}, TransId={req.TransId}, Message={ex.Message}");

            return new ApiResult<PayResult>(ErrorCode.Fail);
        }
    }

    [Route("Payment/DepositCallBack/{TransId}")]
    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> CallBackAsync(string transId)
    {
        try
        {
            var (headers, bodyContent, dicParams) = await HttpExtension.GetCallbackInfo(Request);

            _logger.LogInformation(
                $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, Url={Request.Path}, dicParams={JsonConvert.SerializeObject(dicParams)}, bodyContent={bodyContent}");

            var depositId = _common.GetId(transId);
            var tmpTransId = $"D{_common.GetEnv()}{depositId}";
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
            _logger.LogError(ex, $"Exception: Method={GetCurrentMethod()}, TransId={transId}, Message={ex.Message}");

            return Content(ErrorCode.SystemError.ToString());
        }
    }

    #region GetDepositInput

    private (IPayInfo, Dictionary<string, string>) GetOnlineBankDepositInput(DepositInfoModel depositInfo, Customer custInfo)
    {
        var onlineBankList = _payBll.GetOnlineBankList(custInfo.SiteId, depositInfo.DepositType);
        var vipLevel = _custInfoBll.GetPointLevelWithCache(custInfo.CustId);
        var keys = new[] { "PayId", "SysId" };
        var dicDepositInfo = GetDicDepositInfo(depositInfo.DepositInfo, keys);
        var payId = int.Parse(dicDepositInfo["PayId"]);
        var sysId = int.Parse(dicDepositInfo["SysId"]);

        var payInfo = onlineBankList?.OrderByDescending(x => x.CurrencyId)
            .FirstOrDefault(x => x.PayId == payId
                && (sysId == x.SysId || !x.SelBank)
                && x.PointLevel <= vipLevel
                && x.CurrencyId == custInfo.CurrencyId);

        if (payInfo == null)
        {
            throw new PaymentException(
                $"PayId={payId}, SysId={sysId}, VipLevel={vipLevel}, CurrencyId={custInfo.CurrencyId}, no support bank.",
                ErrorCode.BankNotFound);
        }

        var input = new Dictionary<string, string>
        {
            {"BankName", payInfo.BankName},
            {"BankCode", payInfo.BankCode},
        };

        return (payInfo, input);
    }

    private (IPayInfo, Dictionary<string, string>) GetGenericDepositInput(DepositInfoModel depositInfo,
        Customer custInfo)
        => GenericDepositInput(depositInfo, custInfo, Array.Empty<string>());

    private (IPayInfo, Dictionary<string, string>) GetMobilePayDepositInput(DepositInfoModel depositInfo,
        Customer custInfo)
        => GenericDepositInput(depositInfo, custInfo, new[] { "PayAccount" });

    private (IPayInfo, Dictionary<string, string>) GetLoadCardDepositInput(DepositInfoModel depositInfo,
        Customer custInfo)
        => GenericDepositInput(depositInfo, custInfo, new[] { "Serial", "Code" });

    private (IPayInfo, Dictionary<string, string>) GenericDepositInput(DepositInfoModel depositInfo, Customer custInfo, string[] extraKeys)
    {
        var payInfoList = _payBll.GetPayInfoList(custInfo.SiteId, depositInfo.DepositType);
        var vipLevel = _custInfoBll.GetPointLevelWithCache(custInfo.CustId);
        var keys = new[] { "ChannelId" };
        keys.AddRange(extraKeys);
        var dicDepositInfo = GetDicDepositInfo(depositInfo.DepositInfo, keys);
        var channelId = int.Parse(dicDepositInfo["ChannelId"]);

        var payInfo = payInfoList?.OrderByDescending(x => x.CurrencyId)
            .FirstOrDefault(x => x.ChannelId == channelId
                && x.PointLevel <= vipLevel
                && x.CurrencyId == custInfo.CurrencyId);

        if (payInfo == null)
        {
            throw new PaymentException(
                $"DepositType={depositInfo.DepositType}, ChannelId={channelId}, VipLevel={vipLevel}, CurrencyId={custInfo.CurrencyId}, no support channel.",
                ErrorCode.ChannelNotFound);
        }

        var input = extraKeys
            .ToDictionary(key => key, key => dicDepositInfo[key]);

        return (payInfo, input);
    }

    #endregion

    #region Private

    private string CallBackHandler(
        string transId, Dictionary<string, string> dicParams, string bodyContent, Dictionary<string, string> headers)
    {
        var lockKey = $"DepositCallBackLock:{transId}";
        var lockValue = Guid.NewGuid().ToString(); // 每次取鎖使用唯一值，確保為同一個執行序所執行
        var lockExpiry = TimeSpan.FromSeconds(30); //lock 30 sec
        if (!_redisHelper.TryCreateLock(lockKey, lockValue, lockExpiry))
            return $"TransId={transId} is processing";
        try
        {
            var depositRequest = _paymentBll.GetDepositRequest(transId);

            if (depositRequest == null)
            {
                throw new PaymentException($"TransId={transId} is not exist.");
            }

            var (payName, dic) = GetPayResource(depositRequest);
            var payHelper = _payBll.GetPayHelper(payName);
            depositRequest.PayName = payName;

            var result = payHelper.CallBackDebit(dicParams, bodyContent, headers, dic);
            _logger.LogInformation($"CallBackDebit result={JsonConvert.SerializeObject(result)}");

            if (!string.Equals(transId, result.DepositId, StringComparison.OrdinalIgnoreCase))
            {
                throw new PaymentException($"{transId} is not equal DepositId={result.DepositId}");
            }

            if (result.RequestAmount == 0)
                result.RequestAmount = depositRequest.Amount;

            if (!CanChangeStatus(result.TransStatus, depositRequest.Status))
            {
                var status = result.TransStatus switch
                {
                    TransStatus.Success => DepositStatus.Approved,
                    TransStatus.Fail => DepositStatus.Rejected,
                    _ => DepositStatus.Pending
                };

                var message = $"{transId} status is currently {depositRequest.Status.ToString()}, cannot change status to {status.ToString()}";

                _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {message}");
                _paymentBll.SetDepositLog(transId, depositRequest.SiteId,
                    message, (int)depositRequest.Status, result.ActualAmount, depositRequest.PayName);

                return result.Response;
            }

            switch (result.TransStatus)
            {
                case TransStatus.Success when depositRequest.Amount > 0:
                    _paymentBll.DepositSuccess(depositRequest, result);
                    break;

                case TransStatus.Fail:
                    _paymentBll.DepositFail(depositRequest, result);
                    break;

                case TransStatus.Pending:
                default:
                    _paymentBll.DepositPending(depositRequest, result);
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

    private static bool CanChangeStatus(TransStatus transStatus, DepositStatus depositStatus)
    {
        if (transStatus == TransStatus.Success)
        {
            return depositStatus is DepositStatus.Pending or DepositStatus.Rejected or DepositStatus.Canceled;
        }

        return depositStatus == DepositStatus.Pending;
    }

    private (string PayName, Dictionary<string, string> DicParams) GetPayResource(DepositRequest depositRequest)
    {
        var transId = depositRequest.TransId;
        var custId = depositRequest.CustId;
        var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
        var pay = JsonConvert.DeserializeObject<TpPayInfo>(depositRequest.DepositInfo);

        if (pay == null)
        {
            throw new PaymentException($"TransId={transId} pay info is null.");
        }

        var paySetting = _payBll.GetPaymentAllList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == pay.PayId);

        if (paySetting == null || string.IsNullOrEmpty(paySetting.Setting))
        {
            throw new PaymentException($"TransId={transId} pay setting is null.");
        }

        var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting)
            ?? new Dictionary<string, string>();
        dic.Add("TransID", depositRequest.TransId);
        dic.Add("DepositInfo", depositRequest.DepositInfo);
        dic.Add("DepositType", ((int)depositRequest.DepositType).ToString());
        dic.Add("RequestAmount", depositRequest.Amount.ToString("G29"));
        dic.Add("CurrencyId", depositRequest.CurrencyId.ToString());

        if (!dic.ContainsKey("PayName"))
        {
            throw new PaymentException($"TransId={transId} PayName is not exist.");
        }

        if (!_whitelistBll.IsWhitelist(_common.GetClientIP(HttpContext), paySetting.PayName, transId, GetCurrentMethod()))
        {
            throw new PaymentException($"{paySetting.PayName} requested of {transId} from {_common.GetClientIP(HttpContext)} is Invalid IP!");
        }

        return (paySetting.PayName, dic);
    }

    #endregion
}