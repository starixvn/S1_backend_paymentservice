using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PaymentCore;
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
        private readonly PayBLL payBLL;
        private readonly CustInfoBLL custInfoBLL;
        private readonly IConfiguration configuration;
        private readonly ILogger<DepositController> logger;
        private readonly PaymentBLL paymentBLL;
        private readonly int mainWalletType;
        private readonly string apiUrl;
        private readonly CourierAdapter courierAdapter;
        private readonly Common common;
        private readonly Log2File log2File;
        private static Dictionary<int, Dictionary<string, Dictionary<int, ExchangeRate>>> ExchangeRateList = new Dictionary<int, Dictionary<string, Dictionary<int, ExchangeRate>>>();
        private Timer timer;

        public DepositController(IConfiguration configuration, ILogger<DepositController> logger, CourierAdapter courierAdapter, PayBLL payBLL, CustInfoBLL custInfoBLL, PaymentBLL paymentBLL, Common common, Log2File log2File)
        {
            this.payBLL = payBLL;
            this.custInfoBLL = custInfoBLL;
            this.configuration = configuration;
            this.logger = logger;
            this.paymentBLL = paymentBLL;
            mainWalletType = configuration.GetValue<int>("MainWalletType");
            apiUrl = configuration.GetValue<string>("TpService");
            this.courierAdapter = courierAdapter;
            this.common = common;
            this.log2File = log2File;
        }

        public class ReqExchangeRate
        {
            public int PayId { get; set; }
            public int CustId { get; set; }
        }

        [HttpPost]
        [Route("Payment/ReqExchangeRate")]
        public async Task<ApiResult<Dictionary<string, ExchangeRate>>> ReqExchangeRateAsync(ReqExchangeRate reqExchangeRate)
        {
            ApiResult<Dictionary<string, ExchangeRate>> apiResult = new ApiResult<Dictionary<string, ExchangeRate>>();
            var custInfo = custInfoBLL.GetCustInfoWithCache(reqExchangeRate.CustId);

            if (!ExchangeRateList.ContainsKey(custInfo.SiteId))
            {
                ExchangeRateList.TryAdd(custInfo.SiteId, null);
            }

            if (ExchangeRateList[custInfo.SiteId] == null)
            {
                var paySetting = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == reqExchangeRate.PayId).FirstOrDefault();
                var exchangerateReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
                var payHelper = (IExchangeHelper)payBLL.GetPayHelper(paySetting.PayName);
                var rExchangeRate = await payHelper.GetExchangeAsync(exchangerateReq);

                timer = new Lazy<Timer>(() => new Timer(DoTaskAsync, reqExchangeRate, TimeSpan.Zero, TimeSpan.Zero)).Value;

                if (rExchangeRate.ErrorCode == "0")
                {
                    apiResult.Data = new Dictionary<string, ExchangeRate>();
                    ExchangeRateList[custInfo.SiteId] = rExchangeRate?.ExchangeRateList;
                    foreach (var a1 in rExchangeRate?.ExchangeRateList)
                    {
                        foreach (var a2 in a1.Value)
                        {
                            if (a2.Key == custInfo.CurrencyId)
                            {
                                apiResult.Data.Add(a1.Key, a2.Value);
                            }
                        }
                    }

                    apiResult.ErrorCode = 0;
                }
                else
                {
                    apiResult.Message = rExchangeRate.Message;
                }
            }
            else
            {
                apiResult.Data = new Dictionary<string, ExchangeRate>();

                foreach (var a1 in ExchangeRateList[custInfo.SiteId])
                {
                    foreach (var a2 in a1.Value)
                    {
                        if (a2.Key == custInfo.CurrencyId)
                        {
                            apiResult.Data.Add(a1.Key, a2.Value);
                        }
                    }
                }

                apiResult.ErrorCode = 0;
            }

            return apiResult;
        }

        private async void DoTaskAsync(object state)
        {
            try
            {
                ReqExchangeRate reqExchangeRate = (ReqExchangeRate)state;

                foreach (var c1 in ExchangeRateList.Keys.ToArray())
                {
                    var paySetting = payBLL.GetPaymentList(c1).Where(x => x.PayId == reqExchangeRate.PayId).FirstOrDefault();
                    var exchangerateReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
                    var payHelper = (IExchangeHelper)payBLL.GetPayHelper(paySetting.PayName);
                    var rExchangeRate = await payHelper.GetExchangeAsync(exchangerateReq);

                    if (rExchangeRate.ErrorCode == "0")
                    {
                        ExchangeRateList[c1] = rExchangeRate.ExchangeRateList;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Method={GetCurrentMethod()}");
                log2File.Error($"Exception={ex.Message}, DoTaskAsync Exception", "DoTaskAsync");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(5);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public class ReqDeposit
        {
            public string TransId { get; set; }
            public int PayId { get; set; }
            public int CustId { get; set; }
            public decimal Amount { get; set; }
            public string ClientIP { set; get; }
            public string ReturnUrl { set; get; }
        }

        public class ReqDepositBank : ReqDeposit
        {
            public int SysId { get; set; }
        }
        public class ReqDepositMobile : ReqDeposit
        {
            public int ChannelId { get; set; }
        }

        [HttpPost]
        [Route("Payment/ReqDepositBank")]
        public ApiResult<PayResult> DepositBank(ReqDepositBank reqDeposit)
        {
            ApiResult<PayResult> apiResult = new ApiResult<PayResult>();
            var custId = reqDeposit.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var onlineBank = payBLL.GetOnlineBankList(custInfo.SiteId).Where(x => x.PayId == reqDeposit.PayId && (reqDeposit.SysId == x.SysId || !x.SelBank) && x.PointLevel <= vipLevel).FirstOrDefault();
            //var PayCurrency = payBLL.GetPayCurrency().;
            
            if (onlineBank == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Bank Not Found");
                apiResult.ErrorCode = (int)ErrorCode.BankNotFound;
                return apiResult;
            }

            if (custInfo.CurrencyId != onlineBank.CurrencyId && onlineBank.CurrencyId != 0)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

            if (reqDeposit.Amount % 1 != 0)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                return apiResult;
            }

            var payLangId = payBLL.GetPayLanguage().Where(x => string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.PayLangId;
            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == reqDeposit.PayId).FirstOrDefault();
            var payCurrency = payBLL.GetPayCurrency().Where(x => x.ChannelId == onlineBank.ChannelId && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();
            if (pay == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} pay Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            //DateTimeOffset
            var payHelper = payBLL.GetPayHelper(pay.PayName);
            if (payHelper == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            DepReq.Add("PayCurrencyId", payCurrency.PayCurrencyId);
            DepReq.Add("ClientIP", reqDeposit.ClientIP);
            DepReq.Add("TransID", reqDeposit.TransId);
            DepReq.Add("Amount", reqDeposit.Amount.ToString());
            DepReq.Add("PayCode", onlineBank.PayCode);
            DepReq.Add("BankName", onlineBank.BankName);
            DepReq.Add("BankCode", onlineBank.BankCode);
            DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
            DepReq.Add("PayLangId", payLangId);
            DepReq.Add("CustId", reqDeposit.CustId.ToString());
            var callBackUrl = configuration.GetValue<string>("CallBack");
            DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
            DepReq.Add("IsTest", common.IsTest());

            logger.LogInformation($"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");
            log2File.Info($"{pay.PayName}\r{JsonConvert.SerializeObject(DepReq)}", "DepositBank");

            var result = payHelper.Debit(DepReq);
            if (result.Success)
            {
                apiResult.ErrorCode = 0;
                PayResult payResult = new PayResult();
                payResult.Url = result.Url;
                payResult.FormData = result.FormData;
                payResult.ThirdId = result.ThirdId;
                payResult.Mode = onlineBank.Mode;
                apiResult.Data = payResult;

                if (result.ThirdId != null)
                {
                    paymentBLL.SetThirdId(reqDeposit.TransId, result.ThirdId);
                }
            }
            else
            {
                DepositRequest depositRequest = new DepositRequest();
                depositRequest.DepositType = (int)DepositType.OnlineBanking;
                depositRequest.TransId = reqDeposit.TransId;
                depositRequest.ActualDeposit = reqDeposit.Amount;
                depositRequest.Amount = reqDeposit.Amount;
                depositRequest.PayName = pay.PayName;
                depositRequest.SiteId = custInfo.SiteId;
                depositRequest.DepositInfo = JsonConvert.SerializeObject(onlineBank);
                var rNotifyDepositResult = new NotifyDepositResult();
                rNotifyDepositResult.Comment = result.Message;
                rNotifyDepositResult.ThirdId = result.ThirdId;
                paymentBLL.DepositFail(depositRequest, rNotifyDepositResult);
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = result.ErrorCode;
                apiResult.RawMessage = result.Message;

                paymentBLL.DepositRejectd(reqDeposit.TransId, 0, result.ErrorCode, User.Identity.Name);
            }

            return apiResult;
        }

        [HttpPost]
        [Route("Payment/ReqDepositMobile")]
        public ApiResult<PayResult> DepositMobile(ReqDepositMobile reqDeposit)
        {
            ApiResult<PayResult> apiResult = new ApiResult<PayResult>();
            var custId = reqDeposit.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var mobilePayInfo = payBLL.GetMobilePayInfoList(custInfo.SiteId).Where(x => reqDeposit.ChannelId == x.ChannelId && x.PointLevel <= vipLevel).FirstOrDefault();
            //var PayCurrency = payBLL.GetPayCurrency().;
           
            if (mobilePayInfo == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Channel Not Found");
                apiResult.ErrorCode = (int)ErrorCode.ChannelNotFound;
                return apiResult;
            }

            if (custInfo.CurrencyId != mobilePayInfo.CurrencyId)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

            if (reqDeposit.Amount % 1 != 0)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                return apiResult;
            }

            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == mobilePayInfo.PayId).FirstOrDefault();
            var payLangId = payBLL.GetPayLanguage().Where(x => string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) && (x.PayId == pay.PayId)).FirstOrDefault()?.PayLangId;

            //DateTimeOffset
            var payHelper = payBLL.GetPayHelper(pay.PayName);
            if (payHelper == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            DepReq.Add("PayCurrencyId", mobilePayInfo.PayCurrencyId);
            DepReq.Add("ClientIP", reqDeposit.ClientIP);
            DepReq.Add("TransID", reqDeposit.TransId);
            DepReq.Add("Amount", reqDeposit.Amount.ToString());
            DepReq.Add("PayCode", mobilePayInfo.PayCode);
            DepReq.Add("BankCode", null);
            DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
            DepReq.Add("PayLangId", payLangId);
            DepReq.Add("CustId", reqDeposit.CustId.ToString());
            var callBackUrl = configuration.GetValue<string>("CallBack");
            DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
            DepReq.Add("IsTest", common.IsTest());

            logger.LogInformation($"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");
            log2File.Info($"{pay.PayName}\r{JsonConvert.SerializeObject(DepReq)}", "DepositMobile");

            var result = payHelper.Debit(DepReq);
            if (result.Success)
            {
                apiResult.ErrorCode = 0;
                PayResult payResult = new PayResult();
                payResult.Url = result.Url;
                payResult.FormData = result.FormData;
                payResult.ThirdId = result.ThirdId;
                payResult.Mode = mobilePayInfo.Mode;
                apiResult.Data = payResult;

                if (result.ThirdId != null)
                {
                    paymentBLL.SetThirdId(reqDeposit.TransId, result.ThirdId);
                }
            }
            else
            {
                DepositRequest depositRequest = new DepositRequest();
                depositRequest.DepositType = (int)DepositType.MobilePayment;
                depositRequest.TransId = reqDeposit.TransId;
                depositRequest.ActualDeposit = reqDeposit.Amount;
                depositRequest.Amount = reqDeposit.Amount;
                depositRequest.PayName = pay.PayName;
                depositRequest.SiteId = custInfo.SiteId;
                depositRequest.DepositInfo = JsonConvert.SerializeObject(mobilePayInfo);
                var rNotifyDepositResult = new NotifyDepositResult();
                rNotifyDepositResult.Comment = result.Message;
                rNotifyDepositResult.ThirdId = result.ThirdId;
                paymentBLL.DepositFail(depositRequest, rNotifyDepositResult);
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = result.ErrorCode;
                apiResult.RawMessage = result.Message;

                paymentBLL.DepositRejectd(reqDeposit.TransId, 0, result.ErrorCode, User.Identity.Name);
            }

            return apiResult;
        }

        public class ReqDepositCrpyto : ReqDeposit
        {
            public int ChannelId { get; set; }
        }

        [HttpPost]
        [Route("Payment/ReqDepositCrpyto")]
        public ApiResult<PayResult> DepositCrpyto(ReqDepositCrpyto reqDeposit)
        {
            ApiResult<PayResult> apiResult = new ApiResult<PayResult>();
            var custId = reqDeposit.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var cryptoPayInfo = payBLL.GetCryptoPayInfoList(custInfo.SiteId).Where(x => reqDeposit.ChannelId == x.ChannelId && x.PointLevel <= vipLevel && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();
            //var PayCurrency = payBLL.GetPayCurrency().;
            
            if (cryptoPayInfo == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Channel Not Found");
                apiResult.ErrorCode = (int)ErrorCode.ChannelNotFound;
                return apiResult;
            }

            if (custInfo.CurrencyId != cryptoPayInfo.CurrencyId)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

            if (reqDeposit.Amount % 1 != 0)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                return apiResult;
            }

            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == cryptoPayInfo.PayId).FirstOrDefault();
            var payLangId = payBLL.GetPayLanguage().Where(x => string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) && (x.PayId == pay.PayId)).FirstOrDefault()?.PayLangId;

            //DateTimeOffset
            var payHelper = payBLL.GetPayHelper(pay.PayName);
            if (payHelper == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
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
            var callBackUrl = configuration.GetValue<string>("CallBack");
            DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
            DepReq.Add("IsTest", common.IsTest());

            logger.LogInformation($"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");
            log2File.Info($"{pay.PayName}\r{JsonConvert.SerializeObject(DepReq)}", "ReqDepositCrpyto");

            var result = payHelper.Debit(DepReq);
            if (result.Success)
            {
                apiResult.ErrorCode = 0;
                PayResult payResult = new PayResult();
                payResult.Url = result.Url;
                payResult.FormData = result.FormData;
                payResult.ThirdId = result.ThirdId;
                payResult.Mode = cryptoPayInfo.Mode;
                apiResult.Data = payResult;

                if (result.ThirdId != null)
                {
                    paymentBLL.SetThirdId(reqDeposit.TransId, result.ThirdId);
                }
            }
            else
            {
                DepositRequest depositRequest = new DepositRequest();
                depositRequest.DepositType = (int)DepositType.CryptoPayPayment;
                depositRequest.TransId = reqDeposit.TransId;
                depositRequest.ActualDeposit = reqDeposit.Amount;
                depositRequest.Amount = reqDeposit.Amount;
                depositRequest.PayName = pay.PayName;
                depositRequest.SiteId = custInfo.SiteId;
                depositRequest.DepositInfo = JsonConvert.SerializeObject(cryptoPayInfo);
                var rNotifyDepositResult = new NotifyDepositResult();
                rNotifyDepositResult.Comment = result.Message;
                rNotifyDepositResult.ThirdId = result.ThirdId;
                paymentBLL.DepositFail(depositRequest, rNotifyDepositResult);
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = result.ErrorCode;
                apiResult.RawMessage = result.Message;
                apiResult.Message = result.Message;

                paymentBLL.DepositRejectd(reqDeposit.TransId, 0, result.ErrorCode, User.Identity.Name);
            }

            return apiResult;
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
            ApiResult<PayResult> apiResult = new ApiResult<PayResult>();
            var custId = reqDeposit.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var loadCardPay = payBLL.GetLoadCardPayInfoList(custInfo.SiteId).Where(x => reqDeposit.ChannelId == x.ChannelId && x.PointLevel <= vipLevel).FirstOrDefault();
            //var PayCurrency = payBLL.GetPayCurrency().;
            
            if (loadCardPay == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Channel Not Found");
                apiResult.ErrorCode = (int)ErrorCode.ChannelNotFound;
                return apiResult;
            }

            if (custInfo.CurrencyId != loadCardPay.CurrencyId)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Invalid Currency");
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

            if (reqDeposit.Amount % 1 != 0)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} Amount Out Of Range");
                apiResult.ErrorCode = (int)ErrorCode.AmountOutOfRange;
                return apiResult;
            }

            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == loadCardPay.PayId).FirstOrDefault();
            var payLangId = payBLL.GetPayLanguage().Where(x => string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase) && (x.PayId == pay.PayId)).FirstOrDefault()?.PayLangId;

            //DateTimeOffset
            var payHelper = payBLL.GetPayHelper(pay.PayName);
            if (payHelper == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqDeposit.TransId} payHelper Setting Not Found");
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
            var callBackUrl = configuration.GetValue<string>("CallBack");
            DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
            DepReq.Add("IsTest", common.IsTest());

            logger.LogInformation($"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(DepReq)}");
            log2File.Info($"{pay.PayName}\r{JsonConvert.SerializeObject(DepReq)}", "ReqDepositLoadCard");
            
            var result = payHelper.Debit(DepReq);
            if (result.Success)
            {
                apiResult.ErrorCode = 0;
                PayResult payResult = new PayResult();
                payResult.Url = result.Url;
                payResult.FormData = result.FormData;
                payResult.ThirdId = result.ThirdId;
                payResult.Mode = loadCardPay.Mode;
                apiResult.Data = payResult;

                if (result.ThirdId != null)
                {
                    paymentBLL.SetThirdId(reqDeposit.TransId, result.ThirdId);
                }
            }
            else
            {
                DepositRequest depositRequest = new DepositRequest();
                depositRequest.DepositType = (int)DepositType.LoadCardPayment;
                depositRequest.TransId = reqDeposit.TransId;
                depositRequest.ActualDeposit = reqDeposit.Amount;
                depositRequest.Amount = reqDeposit.Amount;
                depositRequest.PayName = pay.PayName;
                depositRequest.SiteId = custInfo.SiteId;
                depositRequest.DepositInfo = JsonConvert.SerializeObject(loadCardPay);
                var rNotifyDepositResult = new NotifyDepositResult();
                rNotifyDepositResult.Comment = result.Message;
                rNotifyDepositResult.ThirdId = result.ThirdId;
                paymentBLL.DepositFail(depositRequest, rNotifyDepositResult);
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.RawErrorCode = result.ErrorCode;
                apiResult.RawMessage = result.Message;
                apiResult.Message = result.Message;

                paymentBLL.DepositRejectd(reqDeposit.TransId, 0, result.ErrorCode, User.Identity.Name);
            }

            return apiResult;
        }
        bool IsWhiteList(string clientIP, string PaymentName, string transId)
        {
            logger.LogInformation($"Method={GetCurrentMethod()}, Request: PaymentName={PaymentName} clientIP={clientIP} transId={transId}");
            log2File.Info($"{clientIP},{transId}", $"{PaymentName}_PaymentIPLog");

            string[] officeList = configuration.GetSection("OfficeIP")?.Get<string[]>();

            if (officeList.Any(x => x == clientIP))
            {
                return true;
            }

            var WhiteList = configuration.GetSection("WhiteList")?.Get<Dictionary<string, string[]>>();
            if (WhiteList != null)
            {
                var WhiteListIgnoreCase = new Dictionary<string, string[]>(WhiteList, StringComparer.OrdinalIgnoreCase);
                string[] iplist;
                if (WhiteListIgnoreCase.ContainsKey(PaymentName))
                {
                    WhiteListIgnoreCase.TryGetValue(PaymentName, out iplist);
                    if (!iplist.Any(x => x == clientIP))
                    {
                        logger.LogInformation($"Method={GetCurrentMethod()}, Reject: PaymentName={PaymentName} clientIP={clientIP} transId={transId}");
                        log2File.Info($"{clientIP},{transId}", $"{PaymentName}_PaymentRejectIPLog");
                        return false;
                    }
                }

                return true;
            }

            return true;
        }
        

        [Route("Payment/DepositCallBack/{TransId}")]
        [HttpPost]
        public async Task<IActionResult> DepositCallBackAsync(string transId)
        {
            string bodyText = "";
            using (StreamReader reader = new StreamReader(Request.Body))
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

            Dictionary<string, string> dicParam = new Dictionary<string, string>();
            bool isFormData = false;

            if (Request.ContentType == null && Request.QueryString == null)
            {
                logger.LogError($"Method={GetCurrentAsyncMethod()}, IP={common.GetClientIP(HttpContext)}, DepositCallBack_POST Request.ContentType is null");
                log2File.Error($"IP={common.GetClientIP(HttpContext)}, DepositCallBack_POST Request.ContentType is null", "DepositCallBack");
                return Content("Request data is null");
            }

            if (Request.ContentType != null)
            {
                if (Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) || Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
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

            logger.LogInformation($"Method={GetCurrentAsyncMethod()}, IP={common.GetClientIP(HttpContext)}, Url={Request.Path}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");
            log2File.Info($"IP={common.GetClientIP(HttpContext)},\r\n Url={Request.Path},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}", "DepositCallBack");

            int depositId = 0;

            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);
            string cTransId = $"D{common.GetEnv()}{depositId}";
            if (cTransId != transId)
            {
                var payHelperNoCallBackUrl = (IPayHelperNoCallBackUrl)payBLL.GetPayHelper(transId);
                transId = payHelperNoCallBackUrl.FetchTransId(dicParam, bodyText);
            }

            var responseToPay = await CallBackHandlerAsync(transId, dicParam, bodyText, isFormData);
            return Content(responseToPay);
        }

        private async Task<string> CallBackHandlerAsync(string transId, Dictionary<string, string> dicParam, string bodyText, bool isFormData)
        {
            var sbody = string.Join("&", dicParam.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            DepositRequest depositRequest = null;
            //查詢訂單在不在db
            depositRequest = paymentBLL.GetDepositRequest(transId);
            //根據訂單的廠商決定要用那家金流去解讀回傳的資料
            if (depositRequest != null && depositRequest.depositInfo is IPay)
            {
                var custId = depositRequest.CustId;
                var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
                IPay pay = (IPay)depositRequest.depositInfo;
                var paySetting = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == pay.PayId).FirstOrDefault();
                var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
                DepReq.Add("TransID", depositRequest.TransId);
                //檢查namespace是否存在 
                if (!DepReq.ContainsKey("PayName"))
                {
                    logger.LogError($"Method={GetCurrentAsyncMethod()}, {paySetting.PayName} of TransId={transId} is not exist");
                    log2File.Error($"PayName of TransId={transId} is not exist", "CallBackHandlerAsync");
                    return $"PayName of TransId={transId} is not exist";
                }

                if (!IsWhiteList(common.GetClientIP(HttpContext), paySetting.PayName, transId))
                {
                    logger.LogError($"Method={GetCurrentAsyncMethod()}, {paySetting.PayName} requested of {transId} from {common.GetClientIP(HttpContext)} is Invalid IP!");
                    return "Invalid IP!";
                }

                var payHelper = payBLL.GetPayHelper(paySetting.PayName);
                depositRequest.PayName = paySetting.PayName;
                //((PayHelperBase)payHelper).AddPaymentLog(InsertPaymentLog);

                //取得每家金流的成果 
                var rNotifyDepositResult = payHelper.CallBackDebit(dicParam, bodyText, DepReq);
                string sInput = "";
                if (isFormData)
                {
                    sInput = JsonConvert.SerializeObject(dicParam);
                }
                else
                {
                    sInput = bodyText;
                }

                //oDBLog.InsertPaymentLog("", sInput, JsonConvert.SerializeObject(rNotifyDepositResult));

                if (!string.Equals(transId, rNotifyDepositResult.DepositId, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError($"Method={GetCurrentAsyncMethod()}, {transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");
                    log2File.Error($"{transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}", "CallBackHandlerAsync");
                    return $"{transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName}";
                }

                if (rNotifyDepositResult.RequestAmount == 0)
                    rNotifyDepositResult.RequestAmount = depositRequest.Amount;

                if (rNotifyDepositResult.RequestAmount != depositRequest.Amount)
                {
                    logger.LogError($"Method={GetCurrentAsyncMethod()},Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}, PayName={paySetting.PayName}, DepositId={depositRequest.TransId}");
                    log2File.Error($"Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}, PayName={paySetting.PayName}, DepositId={depositRequest.TransId}", "CallBackHandlerAsync");
                    rNotifyDepositResult.Comment = $"Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}";
                    paymentBLL.DepositFail(depositRequest, rNotifyDepositResult);
                    return $"Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}";
                }

                IDepositResult iDepositResult = rNotifyDepositResult;
                if (rNotifyDepositResult.TransStatus == TransStatus.Success && depositRequest.Amount > 0)
                {
                    #region Transaction is Success
                    try
                    {
                        paymentBLL.DepositSuccess(depositRequest, rNotifyDepositResult);
                    }
                    catch (MySqlException SqlEx)
                    {
                        /*Need Check Transaction Status*/
                        StringBuilder errorMessages = new StringBuilder();
                        //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));

                        errorMessages.AppendLine($"Message: {SqlEx.Message}");
                        errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                        errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                        errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                        errorMessages.AppendLine($"Source: {SqlEx.Source}");
                        logger.LogError(SqlEx, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        log2File.Error(errorMessages.ToString(), "CallBackHandlerAsync");
                        //dblogger.Write(SerialId, errorMessages.ToString());
                        return "Update UserMoney error(SQL)";
                    }
                    catch (Exception exForEachTransaction)
                    {
                        logger.LogError(exForEachTransaction, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        log2File.Error(exForEachTransaction.ToString(), "CallBackHandlerAsync");
                        //dblogger.Write(SerialId, exForEachTransaction.Message);
                        return "Update UserMoney error(Other)";
                    }
                    #endregion
                    /*Temp will be Removed*/
                    if (mainWalletType == 2)
                    {
                        var input = new TpTransfer();
                        input.Amount = custInfoBLL.GetBalance(depositRequest.CustId);
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
                        paymentBLL.DepositFail(depositRequest, rNotifyDepositResult);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Method={GetCurrentAsyncMethod()}, transid={transId}");
                        log2File.Error(ex.ToString(), "CallBackHandlerAsync");
                        return "Update DepositFail error";
                    }
                }

                return rNotifyDepositResult.Response;
            }
            else
            {
                logger.LogError($"Method={GetCurrentAsyncMethod()}, SerialId={transId} is not exist");
                log2File.Error($"SerialId={transId} is not exist", "CallBackHandlerAsync");
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
            decimal amount = inTpTransfer.Amount;
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
            var myUrl = $"{apiUrl}Transfer";
            var data = await courierAdapter.MyPostAsync<TransferResult, TpTransfer>(myUrl, tpTransfer);

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