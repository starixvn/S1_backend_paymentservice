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

        public WithdrawalController(IConfiguration configuration, ILogger<WithdrawalController> logger, CustInfoBLL custInfoBLL, PayBLL payBLL, Common common, PaymentBLL paymentBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.payBLL = payBLL;
            outgoingIp = configuration.GetValue<string>("OutgoingIp");
            this.configuration = configuration;
            this.logger = logger;
            this.common = common;
            this.paymentBLL = paymentBLL;
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
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

      
            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == reqWithdrawal.PayId).FirstOrDefault();
            var payCurrency = payBLL.GetPayCurrency().Where(x => x.ChannelId == onlineBank.ChannelId && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (pay == null)
            {
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var payHelper = payBLL.GetPayHelper(pay.PayName);

            if (payHelper == null)
            {
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
            var callBackUrl = configuration.GetValue<string>("CallBack");
            WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");

            logger.LogInformation($"{pay.PayName}\r{JsonConvert.SerializeObject(WithReq)}");

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

            logger.LogInformation(bodyText);


            Dictionary<string, string> dicParam = new Dictionary<string, string>();
            bool isFormData = false;
            if (Request.ContentType == null)
            {
                logger.LogError($"IP={common.GetClientIP(HttpContext)}, DepositCallBack_POST Request.ContentType is null");
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

            logger.LogInformation($"IP={common.GetClientIP(HttpContext)},\r\n Url={Request.Path},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}");

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
                    logger.LogError($"PayName of TransId={transId} is not exist");
                    //dblogger.Write(SerialId, $"PayName of SerialId={depositRequest.SerialId} is not exist");
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
                    logger.LogError($"{transId} is not equal {rNotifyWithdrawalResult.WithdrawId} with PayName={withdrawalRequest.PayName},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}");
                    //dblogger.Write(SerialId, $"{SerialId} is not equal {rNotifyDepositResult.DepositID} with PayName={DepReq["PayName"]}");
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
                        //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));

                        errorMessages.AppendLine($"Message: {SqlEx.Message}");
                        errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                        errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                        errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                        errorMessages.AppendLine($"Source: {SqlEx.Source}");
                        logger.LogError(errorMessages.ToString());
                        //dblogger.Write(SerialId, errorMessages.ToString());
                        return "Update UserMoney error(SQL)";
                    }
                    catch (Exception exForEachTransaction)
                    {
                        logger.LogError(exForEachTransaction.Message);
                        //dblogger.Write(SerialId, exForEachTransaction.Message);
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
                        logger.LogError(ex.Message);
                        //dblogger.Write(SerialId, ex.Message);
                        return "Update DepositFail error";
                    }
                }

                return rNotifyWithdrawalResult.Response;
            }
            else
            {
                logger.LogError($"SerialId={transId} is not exist");
                //dblogger.Write(SerialId, $"SerialId={SerialId} is not exist");
                return $"SerialId={transId} is not exist";
            }
        }

        [Route("Payment/verifytransaction")]
        [HttpPost]
        public IActionResult verifytransaction(string transId, string key)
        {
            logger.LogInformation($"IP={common.GetClientIP(HttpContext)},\r\n Url={Request.Path},\r\n transId={transId},\r\n key={key}");
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