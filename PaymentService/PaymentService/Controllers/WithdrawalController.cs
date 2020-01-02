using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        public WithdrawalController(IConfiguration configuration, ILogger<WithdrawalController> logger, CustInfoBLL custInfoBLL, PayBLL payBLL, Common common)
        {
            this.custInfoBLL = custInfoBLL;
            this.payBLL = payBLL;
            outgoingIp = configuration.GetValue<string>("OutgoingIp");
            this.configuration = configuration;
            this.logger = logger;
            this.common = common;
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
            var onlineBank = payBLL.GetOnlineBankList().Where(x => x.PayId == reqWithdrawal.PayId && reqWithdrawal.SysId == x.SysId).FirstOrDefault();

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

            WithReq.Add("PayCurrencyId", payCurrency.PayCurrencyId);
            //WithReq.Add("ClientIP", reqWithdrawal.ClientIP);
            WithReq.Add("OutgoingIp", outgoingIp);
            WithReq.Add("CustId", custInfo.CustId.ToString());
            WithReq.Add("TransID", reqWithdrawal.TransId);
            WithReq.Add("Amount", reqWithdrawal.Amount.ToString());
            WithReq.Add("PayCode", onlineBank.PayCode);
            WithReq.Add("BankCode", onlineBank.BankCode);
            WithReq.Add("AccountName", reqWithdrawal.AccountName);
            WithReq.Add("AccountNo", reqWithdrawal.AccountNo);
            var callBackUrl = configuration.GetValue<string>("CallBack");
            WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");
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
            throw new Exception("xxx");
        }

        [Route("Payment/verifytransaction")]
        [HttpPost]
        public IActionResult verifytransaction(string transId, string key)
        {
            logger.LogInformation($"IP={common.GetClientIP(HttpContext)},\r\n Url={Request.Path},\r\n transId={transId},\r\n key={key}");
            return Content(true.ToString());
        }
    }
}