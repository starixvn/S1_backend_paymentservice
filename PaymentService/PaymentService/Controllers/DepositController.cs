using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;
using PaymentService.Service.BLL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers
{
    [ApiController]
    public class DepositController : ControllerBase
    {
        private readonly PayBLL _payBll;
        private readonly CustInfoBLL _custInfoBll;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DepositController> _logger;
        private readonly PaymentBLL _paymentBll;
        private readonly int _mainWalletType;
        private readonly string _apiUrl;
        private readonly CourierAdapter _courierAdapter;
        private readonly Common _common;
        private static Dictionary<int, Dictionary<string, Dictionary<int, ExchangeRate>>> ExchangeRateList = new Dictionary<int, Dictionary<string, Dictionary<int, ExchangeRate>>>();

        public DepositController(IServiceProvider provider)
        {
            _payBll = provider.GetRequiredService<PayBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _logger = provider.GetRequiredService<ILogger<DepositController>>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _mainWalletType = _configuration.GetValue<int>("MainWalletType");
            _apiUrl = _configuration.GetValue<string>("TpService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _common = provider.GetRequiredService<Common>();
        }

        public class ReqDeposit
        {
            public string TransId { get; set; }
            public int PayId { get; set; }
            public int CustId { get; set; }
            public decimal Amount { get; set; }
            public string ClientIP { set; get; }
            public string ReturnUrl { set; get; }
            public string Lang { set; get; }
        }

        public class ReqDepositBank : ReqDeposit
        {
            public int SysId { get; set; }
        }
        public class ReqDepositPay : ReqDeposit
        {
            public int ChannelId { get; set; }
            public string PayAccount { set; get; }
        }

        [HttpPost]
        [Route("Payment/BankAutoChannel")]
        [Route("Payment/ReqDepositBank")]
        public ApiResult<PayResult> DepositBank(ReqDepositBank reqDeposit)
        {
            var apiResult = new ApiResult<PayResult>();
            try
            {
                var depositType = Request.Path.Value.IndexOf("BankAutoChannel", StringComparison.OrdinalIgnoreCase) > 1
                   ? DepositType.BankAutoChannel : DepositType.OnlineBanking;
                var custId = reqDeposit.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var vipLevel = _custInfoBll.GetVipLevelWithCache(custId);

                var onlineBankList = depositType == DepositType.OnlineBanking ?
                    _payBll.GetOnlineBankList(custInfo.SiteId) :
                    _payBll.GetAutoChannelBankInfoList(custInfo.SiteId, depositType);

                var onlineBank = onlineBankList?.FirstOrDefault(x =>
                    x.PayId == reqDeposit.PayId && (reqDeposit.SysId == x.SysId || !x.SelBank) &&
                    x.PointLevel <= vipLevel &&
                    x.CurrencyId == custInfo.CurrencyId);

                if (onlineBank == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Bank Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.BankNotFound;
                    return apiResult;
                }

                if (custInfo.CurrencyId != onlineBank.CurrencyId)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                if (reqDeposit.Amount % 1 != 0)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                    apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                    return apiResult;
                }

                var payLangId = _payBll.GetPayLanguage()?.FirstOrDefault(x =>
                    string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) && x.PayId == reqDeposit.PayId)?.PayLangId;
                var pay = _payBll.GetPaymentList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == reqDeposit.PayId);
                
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                //DateTimeOffset
                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                DepReq.Add("PayCurrencyId", onlineBank.PayCurrencyId);
                DepReq.Add("ClientIP", reqDeposit.ClientIP);
                DepReq.Add("TransID", reqDeposit.TransId);
                DepReq.Add("Amount", reqDeposit.Amount.ToString());
                DepReq.Add("PayCode", onlineBank.PayCode);
                DepReq.Add("BankName", onlineBank.BankName);
                DepReq.Add("BankCode", onlineBank.BankCode);
                DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
                DepReq.Add("PayLangId", payLangId);
                DepReq.Add("CustId", reqDeposit.CustId.ToString());
                var callBackUrl = _configuration.GetValue<string>("CallBack");
                DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
                DepReq.Add("IsTest", _common.IsTest());

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");

                var result = payHelper.Debit(DepReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    var payResult = new PayResult();

                    apiResult.Data = payResult;
                    payResult.Url = result.Url;
                    payResult.FormData = result.FormData;
                    payResult.ThirdId = result.ThirdId;
                    payResult.Mode = onlineBank.Mode;
                    payResult.Message = result.Message;

                    if (result.ThirdId != null)
                    {
                        _paymentBll.SetThirdId(reqDeposit.TransId, result.ThirdId);
                    }
                }
                else
                {
                    var depositRequest = new DepositRequest();
                    depositRequest.DepositType = DepositType.OnlineBanking;
                    depositRequest.TransId = reqDeposit.TransId;
                    depositRequest.ActualDeposit = reqDeposit.Amount;
                    depositRequest.Amount = reqDeposit.Amount;
                    depositRequest.PayName = pay.PayName;
                    depositRequest.SiteId = custInfo.SiteId;
                    depositRequest.DepositInfo = JsonConvert.SerializeObject(onlineBank);
                    var rNotifyDepositResult = new NotifyDepositResult();
                    rNotifyDepositResult.Comment = result.Message;
                    rNotifyDepositResult.ThirdId = result.ThirdId;
                    _paymentBll.DepositFail(depositRequest, rNotifyDepositResult);
                    apiResult.ErrorCode = (int)ErrorCode.Fail;
                    apiResult.RawErrorCode = result.ErrorCode;
                    apiResult.RawMessage = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = ((int)ErrorCode.Fail).ToString();
                return apiResult;
            }
        }

        [HttpPost]
        [Route("Payment/ReqDepositQR")]
        [Route("Payment/ReqDepositMobile")]
        public ApiResult<PayResult> DepositPay(ReqDepositPay reqDeposit)
        {
            var apiResult = new ApiResult<PayResult>();
            try
            {
                var depositType = Request.Path.Value.IndexOf("QR", StringComparison.OrdinalIgnoreCase) > 1
                    ? DepositType.QRPayPayment : DepositType.MobilePayment;
                var custId = reqDeposit.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var vipLevel = _custInfoBll.GetVipLevelWithCache(custId);
                var payInfo = _payBll.GetPayInfoList(custInfo.SiteId, depositType)?
                    .FirstOrDefault(x => reqDeposit.ChannelId == x.ChannelId && x.PointLevel <= vipLevel);

                if (payInfo == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Channel Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.ChannelNotFound;
                    return apiResult;
                }

                if (custInfo.CurrencyId != payInfo.CurrencyId)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                if (reqDeposit.Amount % 1 != 0)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                    apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                    return apiResult;
                }

                var pay = _payBll.GetPaymentList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == payInfo.PayId);
                var payLangId = _payBll.GetPayLanguage()?.FirstOrDefault(x =>
                    string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) &&
                    (x.PayId == pay?.PayId))?.PayLangId;
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                //DateTimeOffset
                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                DepReq.Add("PayCurrencyId", payInfo.PayCurrencyId);
                DepReq.Add("ClientIP", reqDeposit.ClientIP);
                DepReq.Add("TransID", reqDeposit.TransId);
                DepReq.Add("Amount", reqDeposit.Amount.ToString());
                DepReq.Add("PayCode", payInfo.PayCode);
                DepReq.Add("BankCode", null);
                DepReq.Add("AccountName", custInfo.AccountName);
                DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
                DepReq.Add("PayLangId", payLangId);
                DepReq.Add("CustId", reqDeposit.CustId.ToString());
                var callBackUrl = _configuration.GetValue<string>("CallBack");
                DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
                DepReq.Add("IsTest", _common.IsTest());
                DepReq.Add("PayAccount", reqDeposit.PayAccount);

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");

                var result = payHelper.Debit(DepReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Data = new PayResult
                    {
                        Url = result.Url,
                        FormData = result.FormData,
                        ThirdId = result.ThirdId,
                        Mode = payInfo.Mode
                    };

                    if (result.ThirdId != null)
                    {
                        _paymentBll.SetThirdId(reqDeposit.TransId, result.ThirdId);
                    }
                }
                else
                {
                    var depositRequest = new DepositRequest
                    {
                        DepositType = depositType,
                        TransId = reqDeposit.TransId,
                        ActualDeposit = reqDeposit.Amount,
                        Amount = reqDeposit.Amount,
                        PayName = pay.PayName,
                        SiteId = custInfo.SiteId,
                        DepositInfo = JsonConvert.SerializeObject(payInfo)
                    };
                    var rNotifyDepositResult = new NotifyDepositResult
                    {
                        Comment = result.Message,
                        ThirdId = result.ThirdId
                    };
                    _paymentBll.DepositFail(depositRequest, rNotifyDepositResult);
                    apiResult.ErrorCode = (int)ErrorCode.Fail;
                    apiResult.RawErrorCode = result.ErrorCode;
                    apiResult.RawMessage = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = ((int)ErrorCode.Fail).ToString();
                return apiResult;
            }
        }

        public class ReqDepositCrpyto : ReqDeposit
        {
            public int ChannelId { get; set; }
            public string DepositInfo { get; set; }
        }

        [HttpPost]
        [Route("Payment/ReqDepositCrpyto")]
        public ApiResult<PayResult> DepositCrpyto(ReqDepositCrpyto reqDeposit)
        {
            var apiResult = new ApiResult<PayResult>();
            try
            {
                var custId = reqDeposit.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var vipLevel = _custInfoBll.GetVipLevelWithCache(custId);
                var cryptoPayInfo = _payBll.GetCryptoPayInfoList(custInfo.SiteId)?.FirstOrDefault(x =>
                    reqDeposit.ChannelId == x.ChannelId && x.PointLevel <= vipLevel &&
                    x.CurrencyId == custInfo.CurrencyId);
                //var PayCurrency = payBLL.GetPayCurrency().;

                if (cryptoPayInfo == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Channel Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.ChannelNotFound;
                    return apiResult;
                }

                if (custInfo.CurrencyId != cryptoPayInfo.CurrencyId)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                if (reqDeposit.Amount % 1 != 0)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                    apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                    return apiResult;
                }

                var pay = _payBll.GetPaymentList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == cryptoPayInfo.PayId);
                var payLangId = _payBll.GetPayLanguage()?.FirstOrDefault(x =>
                    string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) &&
                    (x.PayId == pay?.PayId))?.PayLangId;
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                //DateTimeOffset
                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                var currency = (CurrencyMap)custInfo.CurrencyId;
                DepReq.Add("CurrencyId", currency.ToString());
                DepReq.Add("PayCurrencyId", cryptoPayInfo.PayCurrencyId);
                DepReq.Add("ClientIP", reqDeposit.ClientIP);
                DepReq.Add("TransID", reqDeposit.TransId);
                DepReq.Add("Amount", reqDeposit.Amount.ToString());
                DepReq.Add("PayCode", cryptoPayInfo.PayCode);
                DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
                DepReq.Add("PayLangId", payLangId);
                DepReq.Add("CustId", reqDeposit.CustId.ToString());
                var callBackUrl = _configuration.GetValue<string>("CallBack");
                DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
                DepReq.Add("IsTest", _common.IsTest());
                DepReq.Add("DepositInfo", reqDeposit.DepositInfo);

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");

                var result = payHelper.Debit(DepReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    var payResult = new PayResult();
                    payResult.Url = result.Url;
                    payResult.FormData = result.FormData;
                    payResult.ThirdId = result.ThirdId;
                    payResult.Mode = cryptoPayInfo.Mode;
                    apiResult.Data = payResult;

                    if (result.ThirdId != null)
                    {
                        _paymentBll.SetThirdId(reqDeposit.TransId, result.ThirdId);
                    }

                    if (!string.IsNullOrEmpty(result.DepositInfo))
                    {
                        _paymentBll.UpdateDepositInfo(reqDeposit.TransId, result.DepositInfo);
                    }
                }
                else
                {
                    var depositRequest = new DepositRequest();
                    depositRequest.DepositType = DepositType.CryptoPayPayment;
                    depositRequest.TransId = reqDeposit.TransId;
                    depositRequest.ActualDeposit = reqDeposit.Amount;
                    depositRequest.Amount = reqDeposit.Amount;
                    depositRequest.PayName = pay.PayName;
                    depositRequest.SiteId = custInfo.SiteId;
                    depositRequest.DepositInfo = JsonConvert.SerializeObject(cryptoPayInfo);
                    var rNotifyDepositResult = new NotifyDepositResult();
                    rNotifyDepositResult.Comment = result.Message;
                    rNotifyDepositResult.ThirdId = result.ThirdId;
                    _paymentBll.DepositFail(depositRequest, rNotifyDepositResult);
                    apiResult.ErrorCode = (int)ErrorCode.Fail;
                    apiResult.RawErrorCode = result.ErrorCode;
                    apiResult.RawMessage = result.Message;
                    apiResult.Message = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = ((int)ErrorCode.Fail).ToString();
                return apiResult;
            }
        }

        public class ReqDepositLoadCard : ReqDeposit
        {
            public int ChannelId { get; set; }
            public string Code { get; set; }
            public string Serial { get; set; }
        }

        [HttpPost]
        [Route("Payment/ReqDepositLoadCard")]
        public ApiResult<PayResult> DepositLoadCard(ReqDepositLoadCard reqDeposit)
        {
            var apiResult = new ApiResult<PayResult>();
            try
            {
                var custId = reqDeposit.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var vipLevel = _custInfoBll.GetVipLevelWithCache(custId);
                var loadCardPay = _payBll.GetLoadCardPayInfoList(custInfo.SiteId)?
                    .FirstOrDefault(x => reqDeposit.ChannelId == x.ChannelId && x.PointLevel <= vipLevel);

                if (loadCardPay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Channel Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.ChannelNotFound;
                    return apiResult;
                }

                if (custInfo.CurrencyId != loadCardPay.CurrencyId)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                if (reqDeposit.Amount % 1 != 0)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                    apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                    return apiResult;
                }

                var pay = _payBll.GetPaymentList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == loadCardPay.PayId);
                var payLangId = _payBll.GetPayLanguage()?.FirstOrDefault(x =>
                    string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) &&
                    (x.PayId == pay?.PayId))?.PayLangId;
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                //DateTimeOffset
                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                DepReq.Add("PayCurrencyId", loadCardPay.PayCurrencyId);
                DepReq.Add("ClientIP", reqDeposit.ClientIP);
                DepReq.Add("TransID", reqDeposit.TransId);
                DepReq.Add("Amount", reqDeposit.Amount.ToString());
                DepReq.Add("PayCode", loadCardPay.PayCode);
                DepReq.Add("Code", reqDeposit.Code);
                DepReq.Add("Serial", reqDeposit.Serial);
                DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
                DepReq.Add("PayLangId", payLangId);
                DepReq.Add("CustId", reqDeposit.CustId.ToString());
                var callBackUrl = _configuration.GetValue<string>("CallBack");
                DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
                DepReq.Add("Lang", custInfo.UserLang);
                DepReq.Add("IsTest", _common.IsTest());

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");

                var result = payHelper.Debit(DepReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    var payResult = new PayResult();
                    payResult.FormData = result.FormData;
                    payResult.ThirdId = result.ThirdId;
                    payResult.Mode = loadCardPay.Mode;
                    apiResult.Data = payResult;
                    apiResult.Message = result.Message;

                    if (result.ThirdId != null)
                    {
                        _paymentBll.SetThirdId(reqDeposit.TransId, result.ThirdId);
                    }
                }
                else
                {
                    var depositRequest = new DepositRequest();
                    depositRequest.DepositType = DepositType.LoadCardPayment;
                    depositRequest.TransId = reqDeposit.TransId;
                    depositRequest.ActualDeposit = reqDeposit.Amount;
                    depositRequest.Amount = reqDeposit.Amount;
                    depositRequest.PayName = pay.PayName;
                    depositRequest.SiteId = custInfo.SiteId;
                    depositRequest.DepositInfo = JsonConvert.SerializeObject(loadCardPay);
                    var rNotifyDepositResult = new NotifyDepositResult();
                    rNotifyDepositResult.Comment = result.Message;
                    rNotifyDepositResult.ThirdId = result.ThirdId;
                    _paymentBll.DepositFail(depositRequest, rNotifyDepositResult);
                    //apiResult.ErrorCode = (int)ErrorCode.Fail;
                    apiResult.ErrorCode = 0;
                    var payResult = new PayResult();
                    payResult.FormData = result.FormData;
                    payResult.ThirdId = result.ThirdId;
                    payResult.Mode = loadCardPay.Mode;
                    apiResult.Data = payResult;
                    apiResult.RawErrorCode = ((int)ErrorCode.Fail).ToString();
                    apiResult.RawMessage = result.Message;
                    apiResult.Message = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = ((int)ErrorCode.Fail).ToString();
                return apiResult;
            }
        }

        
        [Route("Payment/GetClientIp")]
        [HttpGet, HttpPost]
        public string GetClientIp()
        {
            var clientIp = _common.GetClientIP(HttpContext);
            return clientIp;
        }

        [Route("Payment/getheaders")]
        [HttpGet]
        public ActionResult<Dictionary<string, StringValues>> GetHeaders()
        {
            return Ok(Request.Headers.ToDictionary(d => d.Key));
        }

        [Route("Payment/DepositManual")]
        [HttpPost]
        public ActionResult<DepositManualResponse> DepositManual(DepositManualRequest request)
        {

            try
            {
                var response = new DepositManualResponse
                {
                    ErrorCode = (int)ErrorCode.Fail,
                    Message = ErrorCode.Fail.ToString(),
                    Amount = 0
                };
                const string payName = "Manual";
                var clientIp = _common.GetClientIP(HttpContext);
                if (! _common.IsWhiteList(clientIp, payName, request.TransId, GetCurrentMethod()))
                {
                    _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {payName} requested of {request.TransId} from {clientIp} is Invalid IP!");
                    response.ErrorCode = (int)ErrorCode.Fail;
                    response.Message = "Invalid IP!";
                    response.Amount = 0;
                    return response;
                }
                
                var depositInfo = _paymentBll.GetDepositInfo(request.TransId);
                if (depositInfo == null)
                {
                    response.ErrorCode = (int)ErrorCode.Fail;
                    response.Message = $"{request.TransId} is not exist";
                    response.Amount = 0;
                    return response;
                }

                if (depositInfo.Status == (int)DepositStatus.Approved)
                {
                    response.ErrorCode = (int)ErrorCode.Fail;
                    response.Message = $"{request.TransId} status is already approved and cannot be changed";
                    response.Amount = 0;
                    return response;
                }

                depositInfo.ActualDeposit = (request.Amount == 0) ? depositInfo.Amount : request.Amount;
                depositInfo.Comment = "Manual Approved";
                depositInfo.Status = (int)DepositStatus.Approved;
                depositInfo.RefId = string.IsNullOrEmpty(request.ThirdId) ? null : request.ThirdId;
                depositInfo.Operator = string.IsNullOrEmpty(request.Operator) ? "Manual" : request.Operator;
                
                var result = _paymentBll.DepositManualSuccess(depositInfo);
                if (result > 0)
                {
                    response.ErrorCode = (int)ErrorCode.Success;
                    response.Message = ErrorCode.Success.ToString();
                    response.Amount = depositInfo.ActualDeposit;
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentAsyncMethod());
                return new DepositManualResponse
                {
                    ErrorCode = (int)ErrorCode.SystemError,
                    Message = ErrorCode.SystemError.ToString(),
                    Amount = 0
                };
            }
        }

        [Route("Payment/DepositCallBack/{TransId}")]
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> DepositCallBackAsync(string transId)
        {
            try
            {
                var bodyText = "";
                using (var reader = new StreamReader(Request.Body))
                {
                    bodyText = await reader.ReadToEndAsync();
                }

                var absoluteUri = string.Concat(
                    Request.Scheme,
                    "://",
                    Request.Host.ToUriComponent(),
                    Request.PathBase.ToUriComponent(),
                    Request.Path.ToUriComponent(),
                    Request.QueryString.ToUriComponent());

                var dicParam = new Dictionary<string, string>();
                var isFormData = false;

                if (Request.ContentType != null)
                {
                    if (Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ||
                        Request.ContentType.StartsWith("application/x-www-form-urlencoded",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var vFormData in Request.Form)
                        {
                            dicParam.Add(vFormData.Key, vFormData.Value);
                        }

                        isFormData = true;
                    }
                }
                else
                {
                    var query = QueryHelpers.ParseQuery(Request.QueryString.Value);

                    foreach (var vFormData in query)
                    {
                        dicParam.Add(vFormData.Key, vFormData.Value);
                    }

                    isFormData = true;
                }

                _logger.LogInformation(
                    $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, Url={Request.Path}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");

                var depositId = _common.GetId(transId);
                var cTransId = $"D{_common.GetEnv()}{depositId}";
                if (cTransId != transId)
                {
                    var payHelperNoCallBackUrl = (IPayHelperNoCallBackUrl)_payBll.GetPayHelper(transId);
                    transId = payHelperNoCallBackUrl.FetchTransId(dicParam, bodyText);
                }

                var responseToPay = await CallBackHandlerAsync(transId, dicParam, bodyText, isFormData);
                return Content(responseToPay);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentAsyncMethod());
                return Content(ErrorCode.Fail.ToString());
            }
        }

        private async Task<string> CallBackHandlerAsync(string transId, Dictionary<string, string> dicParam, string bodyText, bool isFormData)
        {
            DepositRequest depositRequest = null;
            //查詢訂單在不在db
            depositRequest = _paymentBll.GetDepositRequest(transId);
            //根據訂單的廠商決定要用那家金流去解讀回傳的資料
            if (depositRequest != null && depositRequest.depositInfo is IPay)
            {
                var custId = depositRequest.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var pay = (IPay)depositRequest.depositInfo;
                var paySetting = _payBll.GetPaymentAllList(custInfo.SiteId)?.FirstOrDefault(x => x.PayId == pay.PayId);
                var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
                DepReq.Add("TransID", depositRequest.TransId);
                DepReq.Add("BankCode", pay.BankCode);
                DepReq.Add("DepositInfo", depositRequest.DepositInfo);
                DepReq.Add("RequestAmount", depositRequest.Amount.ToString());

                //DepReq.Add("Amount", depositRequest.Amount.ToString());
                //檢查namespace是否存在 
                if (!DepReq.ContainsKey("PayName"))
                {
                    _logger.LogError($"Method={GetCurrentAsyncMethod()}, {paySetting.PayName} of TransId={transId} is not exist");
                    return $"PayName of TransId={transId} is not exist";
                }

                if (!_common.IsWhiteList(_common.GetClientIP(HttpContext), paySetting.PayName, transId, GetCurrentMethod()))
                {
                    _logger.LogError($"Method={GetCurrentAsyncMethod()}, {paySetting.PayName} requested of {transId} from {_common.GetClientIP(HttpContext)} is Invalid IP!");
                    return "Invalid IP!";
                }

                var payHelper = _payBll.GetPayHelper(paySetting.PayName);
                depositRequest.PayName = paySetting.PayName;

                //取得每家金流的成果 
                var rNotifyDepositResult = payHelper.CallBackDebit(dicParam, bodyText, DepReq);

                if (!string.Equals(transId, rNotifyDepositResult.DepositId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError($"Method={GetCurrentAsyncMethod()}, {transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");
                    return $"{transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName}";
                }

                if (rNotifyDepositResult.RequestAmount == 0)
                    rNotifyDepositResult.RequestAmount = depositRequest.Amount;

                if (rNotifyDepositResult.RequestAmount != depositRequest.Amount)
                {
                    _logger.LogInformation($"Method={GetCurrentAsyncMethod()},Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}, PayName={paySetting.PayName}, DepositId={depositRequest.TransId}");
                    rNotifyDepositResult.Comment = $"Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}";
                }

                if ((rNotifyDepositResult.TransStatus == TransStatus.Success &&
                     (depositRequest.Status != DepositStatus.Pending &&
                      depositRequest.Status != DepositStatus.Rejected)) ||
                    (rNotifyDepositResult.TransStatus != TransStatus.Success &&
                     depositRequest.Status != DepositStatus.Pending))
                {
                    var status = rNotifyDepositResult.TransStatus switch 
                    {
                        TransStatus.Success => DepositStatus.Approved,
                        TransStatus.Fail => DepositStatus.Rejected,
                        _ => DepositStatus.Pending
                    };
                    var message = $"{transId} status is currently {depositRequest.Status.ToString()}, cannot change status to {status.ToString()}";
                    _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {message}");
                    
                    _paymentBll.SetDepositLog(transId, depositRequest.SiteId,
                        message,
                        (int)depositRequest.Status, rNotifyDepositResult.ActualAmount, depositRequest.PayName);

                    return rNotifyDepositResult.Response;
                }

                if (rNotifyDepositResult.TransStatus == TransStatus.Success && depositRequest.Amount > 0)
                {
                    #region Transaction is Success
                    try
                    {
                        _paymentBll.DepositSuccess(depositRequest, rNotifyDepositResult);
                    }
                    catch (MySqlException SqlEx)
                    {
                        /*Need Check Transaction Status*/
                        var errorMessages = new StringBuilder();
                        //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));

                        errorMessages.AppendLine($"Message: {SqlEx.Message}");
                        errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                        errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                        errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                        errorMessages.AppendLine($"Source: {SqlEx.Source}");
                        _logger.LogError(SqlEx, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        //dblogger.Write(SerialId, errorMessages.ToString());
                        return "Update UserMoney error(SQL)";
                    }
                    catch (Exception exForEachTransaction)
                    {
                        _logger.LogError(exForEachTransaction, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        //dblogger.Write(SerialId, exForEachTransaction.Message);
                        return "Update UserMoney error(Other)";
                    }
                    #endregion
                    /*Temp will be Removed*/
                    if (_mainWalletType == 2)
                    {
                        var input = new TpTransfer();
                        input.Amount = _custInfoBll.GetBalance(depositRequest.CustId);
                        input.CustId = depositRequest.CustId;
                        input.direction = TpDirection.ToTp;
                        input.TpId = 1;
                        var transferToSabaResult = await TransferToSabaAsync(input);
                    }
                    /*Temp*/
                }
                else if (rNotifyDepositResult.TransStatus == TransStatus.Fail)
                {
                    try
                    {
                        _paymentBll.DepositFail(depositRequest, rNotifyDepositResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        return "Update DepositFail error";
                    }
                }
                else
                {
                    try
                    {
                        _paymentBll.DepositPending(depositRequest, rNotifyDepositResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        return "Update DepositPending error";
                    }
                }

                return rNotifyDepositResult.Response;
            }
            else
            {
                _logger.LogError($"Method={GetCurrentAsyncMethod()}, SerialId={transId} is not exist");
                return $"SerialId={transId} is not exist";
            }
        }
        public class TpTransfer
        {
            public int TpId { set; get; }
            public int CustId { set; get; }
            public decimal Amount { set; get; }
            public TpDirection direction { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        public enum TpDirection
        {
            ToTp = 1,
            ToMain = 2
        }
        private async Task<ApiResult<TransferResult>> TransferToSabaAsync(TpTransfer inTpTransfer)
        {
            var amount = inTpTransfer.Amount;
            if (amount.ToString().IndexOf(".") > -1)
            {
                decimal.TryParse(amount.ToString("#.##"), out amount);
            }

            var tpTransfer = new TpTransfer
            {
                TpId = inTpTransfer.TpId,
                CustId = inTpTransfer.CustId,
                Amount = amount,
                direction = inTpTransfer.direction,
                inputs = null
            };
            var myUrl = $"{_apiUrl}Transfer";
            var data = await _courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);

            return data;
        }
        public class TransferResult
        {
            public TransferResult()
            {
                Status = TransferStatus.Pending;
            }
            public TransferStatus Status { get; set; }
            public string ThirdId { get; set; }
            public decimal Difference { get; set; }
        }
        public enum TransferStatus
        {
            Success = 0, Fail = 1, Pending = 2, PendingD = 3
        }
    }
}
