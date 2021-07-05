using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class WithdrawalController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly PayBLL payBLL;
        private readonly string outgoingIp;
        private readonly IConfiguration configuration;
        private readonly ILogger<WithdrawalController> logger;
        private readonly Common common;
        private readonly PaymentBLL paymentBLL;
        private readonly Log2File log2File;

        public WithdrawalController(IConfiguration configuration, ILogger<WithdrawalController> logger, CustInfoBLL custInfoBLL, PayBLL payBLL, Common common, PaymentBLL paymentBLL, Log2File log2File)
        {
            this.custInfoBLL = custInfoBLL;
            this.payBLL = payBLL;
            outgoingIp = configuration.GetValue<string>("OutgoingIp");
            this.configuration = configuration;
            this.logger = logger;
            this.common = common;
            this.paymentBLL = paymentBLL;
            this.log2File = log2File;
        }
        public class ReqWithdrawal
        {
            public int SysId { get; set; }
            public string TransId { get; set; }
            public int PayId { get; set; }
            public int CustId { get; set; }
            public decimal Amount { get; set; }
            //public string ClientIP { set; get; }
            public string AccountName { set; get; }
            public string AccountNo { set; get; }
            public string ReturnUrl { set; get; }
        }

        [HttpPost]
        [Route("Payment/Withdrawal")]
        public ApiResult Withdrawal(ReqWithdrawal reqWithdrawal)
        {
            ApiResult apiResult = new ApiResult();
            var custId = reqWithdrawal.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var onlineBank = payBLL.GetOnlineBankListForPayout().Where(x => x.PayId == reqWithdrawal.PayId && reqWithdrawal.SysId == x.SysId).FirstOrDefault();

            if (custInfo.CurrencyId != onlineBank.CurrencyId)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} Invalid Currency");
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

      
            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == reqWithdrawal.PayId).FirstOrDefault();
            var payCurrency = payBLL.GetPayCurrency().Where(x => x.ChannelId == onlineBank.ChannelId && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();
            if (pay == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} pay Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var payHelper = payBLL.GetPayHelper(pay.PayName);
            if (payHelper == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} payHelper Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var WithReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            WithReq.Add("PayCurrencyId", payCurrency?.PayCurrencyId);
            //WithReq.Add("ClientIP", reqWithdrawal.ClientIP);
            WithReq.Add("OutgoingIp", outgoingIp);
            WithReq.Add("CustId", custInfo.CustId.ToString());
            WithReq.Add("TransID", reqWithdrawal.TransId);
            WithReq.Add("Amount", reqWithdrawal.Amount.ToString());
            WithReq.Add("PayCode", onlineBank.PayCode);
            WithReq.Add("BankCode", onlineBank.BankCode);
            WithReq.Add("BankName", onlineBank.BankName); 
            WithReq.Add("AccountName", reqWithdrawal.AccountName);
            WithReq.Add("AccountNo", reqWithdrawal.AccountNo);
            WithReq.Add("IsTest", common.IsTest());
            var callBackUrl = configuration.GetValue<string>("CallBack");
            WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");

            logger.LogInformation($"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(WithReq)}");
            log2File.Info($"{pay.PayName}\r{JsonConvert.SerializeObject(WithReq)}", "Withdrawal");
            
            var result = payHelper.Credit(WithReq);
            if (result.Success)
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = result.Message;
            }
            else
            {
                apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                apiResult.Message = result.Message;
            }

            return apiResult;
        }

        [HttpPost]
        [Route("Payment/CryptoWithdrawal")]
        public ApiResult CryptoWithdrawal(ReqWithdrawal reqWithdrawal)
        {
            ApiResult apiResult = new ApiResult();
            var custId = reqWithdrawal.CustId;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var crypto = payBLL.GetCryptoListForPayout().Where(x => x.PayId == reqWithdrawal.PayId && custInfo.CurrencyId == x.CurrencyId).FirstOrDefault();
            if (crypto == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} Invalid Currency");
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == reqWithdrawal.PayId).FirstOrDefault();
            var payCurrency = payBLL.GetPayCurrency().Where(x => x.ChannelId == crypto.ChannelId && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();
            if (pay == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} pay Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var payHelper = payBLL.GetPayHelper(pay.PayName);
            if (payHelper == null)
            {
                logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} payHelper Setting Not Found");
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var WithReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            WithReq.Add("PayCurrencyId", payCurrency?.PayCurrencyId);
            //WithReq.Add("ClientIP", reqWithdrawal.ClientIP);
            WithReq.Add("OutgoingIp", outgoingIp);
            WithReq.Add("CustId", custInfo.CustId.ToString());
            WithReq.Add("TransID", reqWithdrawal.TransId);
            WithReq.Add("Amount", reqWithdrawal.Amount.ToString());
            WithReq.Add("PayCode", crypto.PayCode);
            WithReq.Add("BankCode", crypto.BankCode);
            WithReq.Add("BankName", crypto.BankName);
            WithReq.Add("AccountName", reqWithdrawal.AccountName);
            WithReq.Add("AccountNo", reqWithdrawal.AccountNo);
            WithReq.Add("IsTest", common.IsTest());
            var callBackUrl = configuration.GetValue<string>("CallBack");
            WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");

            logger.LogInformation($"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(WithReq)}");
            log2File.Info($"{pay.PayName}\r{JsonConvert.SerializeObject(WithReq)}", "Withdrawal");

            var result = payHelper.Credit(WithReq);
            if (result.Success)
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = result.Message;
            }
            else
            {
                apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                apiResult.Message = result.Message;
            }

            return apiResult;
        }

        [Route("Payment/WithdrawalCallBack/{TransId}")]
        [HttpPost]
        public async Task<IActionResult> WithdrawalCallBackAsync(string transId)
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
            if (Request.ContentType == null)
            {
                logger.LogError($"Method={GetCurrentAsyncMethod()}, IP={common.GetClientIP(HttpContext)}, DepositCallBack_POST Request.ContentType is null");
                log2File.Info($"IP={common.GetClientIP(HttpContext)}, DepositCallBack_POST Request.ContentType is null", "WithdrawalCallBack");
                return Content("Request data is null");
            }

            if (Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) || Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var vFormData in Request.Form)
                {
                    dicParam.Add(vFormData.Key, vFormData.Value);
                }

                isFormData = true;
            }

            logger.LogInformation($"Method={GetCurrentAsyncMethod()}, IP={common.GetClientIP(HttpContext)}, Url={Request.Path}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");
            log2File.Info($"IP={common.GetClientIP(HttpContext)},\r\n Url={Request.Path},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}", "WithdrawalCallBack");
            
            int withdrawalId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);
            string cTransId = $"W{common.GetEnv()}{withdrawalId}";
            if (cTransId != transId)
            {
                var payHelperNoCallBackUrl = (IPayHelperNoCallBackUrl)payBLL.GetPayHelper(transId);
                transId = payHelperNoCallBackUrl.FetchTransId(dicParam, bodyText);
            }

            var responseToPay = CallBackHandler(transId, dicParam, bodyText, isFormData);
            return Content(responseToPay);
        }

        private string CallBackHandler(string transId, Dictionary<string, string> dicParam, string bodyText, bool isFormData)
        {
            var sbody = string.Join("&", dicParam.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //查詢訂單在不在db
            var withdrawalRequest = paymentBLL.GetWithdrawalRequest(transId);
            //根據訂單的廠商決定要用那家金流去解讀回傳的資料
            if (withdrawalRequest != null)
            {
                var WithReq = withdrawalRequest.ISetting;

                //檢查namespace是否存在 
                if (!WithReq.ContainsKey("PayName"))
                {
                    logger.LogError($"Method={GetCurrentMethod()}, PayName of TransId={transId} is not exist");
                    log2File.Error($"PayName of TransId={transId} is not exist", "CallBackHandler");
                    return $"PayName of TransId={transId} is not exist";
                }

                var payHelper = payBLL.GetPayHelper(withdrawalRequest.PayName);
                //((PayHelperBase)payHelper).AddPaymentLog(InsertPaymentLog);

                //取得每家金流的成果 
                var rNotifyWithdrawalResult = payHelper.CallBackCredit(dicParam, bodyText, WithReq);
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

                string temp = rNotifyWithdrawalResult.WithdrawId;
                while(temp.Substring(0,1) == "0")
                {
                    temp = temp.Remove(0, 1);
                }
                if (!string.Equals(transId, temp, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError($"Method={GetCurrentMethod()}, {transId} is not equal {rNotifyWithdrawalResult.WithdrawId} with PayName={withdrawalRequest.PayName}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");
                    log2File.Error($"{transId} is not equal {rNotifyWithdrawalResult.WithdrawId} with PayName={withdrawalRequest.PayName},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}", "CallBackHandler");
                    return $"{transId} is not equal {rNotifyWithdrawalResult.WithdrawId} with PayName={withdrawalRequest.PayName}";
                }

                NotifyWithdrawResult notifyWithdrawResult = rNotifyWithdrawalResult;
                if (rNotifyWithdrawalResult.TransStatus == TransStatus.Success)
                {
                    #region Transaction is Success
                    try
                    {
                        paymentBLL.WithdrawalSuccess(withdrawalRequest, rNotifyWithdrawalResult);
                    }
                    catch (MySqlException SqlEx)
                    {
                        /*Need Check Transaction Status*/
                        StringBuilder errorMessages = new StringBuilder();
                        errorMessages.AppendLine($"Message: {SqlEx.Message}");
                        errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                        errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                        errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                        errorMessages.AppendLine($"Source: {SqlEx.Source}");
                        logger.LogError(SqlEx, $"Method={GetCurrentMethod()}, transId={transId}");
                        log2File.Error(errorMessages.ToString(), "CallBackHandler");
                        return "Update UserMoney error(SQL)";
                    }
                    catch (Exception exForEachTransaction)
                    {
                        logger.LogError(exForEachTransaction, $"Method={GetCurrentMethod()}, transId={transId}");
                        log2File.Error(exForEachTransaction.ToString(), "CallBackHandler");
                        return "Update UserMoney error(Other)";
                    }
                    #endregion
                }
                else if (rNotifyWithdrawalResult.TransStatus == TransStatus.Fail)
                {
                    try
                    {
                        paymentBLL.WithdrawalFail(withdrawalRequest, rNotifyWithdrawalResult);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Method={GetCurrentMethod()}, transId={transId}");
                        log2File.Error(ex.ToString(), "CallBackHandler");
                        return "Update DepositFail error";
                    }
                }

                return rNotifyWithdrawalResult.Response;
            }
            else
            {
                logger.LogError($"Method={GetCurrentMethod()}, SerialId ={transId} is not exist");
                log2File.Error($"SerialId={transId} is not exist", "CallBackHandler");
                return $"SerialId={transId} is not exist";
            }
        }

        [Route("Payment/verifytransaction")]
        [HttpPost]
        public IActionResult verifytransaction(string transId, string key)
        {
            logger.LogInformation($"Method={GetCurrentMethod()}, IP={common.GetClientIP(HttpContext)}, Url={Request.Path}, transId={transId}, key={key}");
            log2File.Info($"IP={common.GetClientIP(HttpContext)},\r\n Url={Request.Path},\r\n transId={transId},\r\n key={key}", "verifytransaction");
            var withdrawalRequest = paymentBLL.GetWithdrawalRequest(transId);
            if (withdrawalRequest != null) {

                //var payHelper = payBLL.GetPayHelper(withdrawalRequest.PayName);
                //payHelper.
                //lib.ToMD5(transId,)
                //withdrawalRequest.ISetting["key"];

                return Content(true.ToString());
            }
            else
            {
                return Content(false.ToString());
            }
        }
    }
}