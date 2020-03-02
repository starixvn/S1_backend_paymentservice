using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PaymentCore;
using PaymentService.Service.BLL;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

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

        public DepositController(IConfiguration configuration, ILogger<DepositController> logger, CourierAdapter courierAdapter, PayBLL payBLL, CustInfoBLL custInfoBLL, PaymentBLL paymentBLL, Common common)
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
            var onlineBank = payBLL.GetOnlineBankList().Where(x => x.PayId == reqDeposit.PayId && reqDeposit.SysId == x.SysId && x.PointLevel <= vipLevel).FirstOrDefault();
            //var PayCurrency = payBLL.GetPayCurrency().;
            if (onlineBank == null)
            {
                apiResult.ErrorCode = (int)ErrorCode.BankNotFound;
                return apiResult;
            }

            if (custInfo.CurrencyId != onlineBank.CurrencyId)
            {
                apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                return apiResult;
            }

            var payLangId = payBLL.GetPayLanguage().Where(x => string.Equals(x.LangId, custInfo.UserLang, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.PayLangId;
            var pay = payBLL.GetPaymentList(custInfo.SiteId).Where(x => x.PayId == reqDeposit.PayId).FirstOrDefault();
            var payCurrency = payBLL.GetPayCurrency().Where(x => x.ChannelId == onlineBank.ChannelId && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();
            if (pay == null)
            {
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }
            //DateTimeOffset
            var payHelper = payBLL.GetPayHelper(pay.PayName);

            if (payHelper == null)
            {
                apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                return apiResult;
            }

            var DepReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
            DepReq.Add("PayCurrencyId", payCurrency.PayCurrencyId);
            DepReq.Add("ClientIP", reqDeposit.ClientIP);
            DepReq.Add("TransID", reqDeposit.TransId);
            DepReq.Add("Amount", reqDeposit.Amount.ToString());
            DepReq.Add("PayCode", onlineBank.PayCode);
            DepReq.Add("BankCode", onlineBank.BankCode);
            DepReq.Add("ReturnUrl", reqDeposit.ReturnUrl);
            DepReq.Add("PayLangId", payLangId);
            DepReq.Add("CustId", reqDeposit.CustId.ToString());
            var callBackUrl = configuration.GetValue<string>("CallBack");
            DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");

            logger.LogInformation($"{pay.PayName}\r{JsonConvert.SerializeObject(DepReq)}");

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
            }

            return apiResult;
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
            var responseToPay = await CallBackHandlerAsync(transId, dicParam, "", isFormData);
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
                    logger.LogError($"PayName of TransId={transId} is not exist");
                    //dblogger.Write(SerialId, $"PayName of SerialId={depositRequest.SerialId} is not exist");
                    return $"PayName of TransId={transId} is not exist";
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
                    logger.LogError($"{transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName},\r\n form={JsonConvert.SerializeObject(dicParam)},\r\n bodyText={bodyText}");
                    //dblogger.Write(SerialId, $"{SerialId} is not equal {rNotifyDepositResult.DepositID} with PayName={DepReq["PayName"]}");
                    return $"{transId} is not equal {rNotifyDepositResult.DepositId} with PayName={paySetting.PayName}";
                }

                if (rNotifyDepositResult.RequestAmount == 0)
                    rNotifyDepositResult.RequestAmount = depositRequest.Amount;

                if (rNotifyDepositResult.RequestAmount != depositRequest.Amount)
                {
                    logger.LogError($"Request Amount {rNotifyDepositResult.RequestAmount} is not equal {depositRequest.Amount}");
                    //dblogger.Write(transId, $"OrderAmount{rNotifyDepositResult.OrderAmount} is not equal {depositRequest.UserMoney}");
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
                        logger.LogError(ex.Message);
                        //dblogger.Write(SerialId, ex.Message);
                        return "Update DepositFail error";
                    }
                }

                return rNotifyDepositResult.Response;
            }
            else
            {
                logger.LogError($"SerialId={transId} is not exist");
                //dblogger.Write(SerialId, $"SerialId={SerialId} is not exist");
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