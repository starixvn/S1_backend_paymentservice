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

        public DepositController(IConfiguration configuration, ILogger<DepositController> logger, PayBLL payBLL, CustInfoBLL custInfoBLL)
        {
            this.payBLL = payBLL;
            this.custInfoBLL = custInfoBLL;
            this.configuration = configuration;
            this.logger = logger;
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
            var callBackUrl = configuration.GetValue<string>("CallBack");
            DepReq.Add("CallBackUrl", $"{callBackUrl}/Payment/DepositCallBack/{reqDeposit.TransId}");
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
        public IActionResult DepositCallBack(string transId)
        {
            string bodyText = "";
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                bodyText = reader.ReadToEnd();
            }

            logger.LogDebug(bodyText);
            return Content("test!");
        }
    }
}