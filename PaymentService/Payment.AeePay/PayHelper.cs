using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.AeePay
{
    class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }
        public class DebitRes
        {
            public bool success { get; set; }
            public string resultCode { get; set; }
            public string resultMsg { get; set; }
            public DebitResData data { get; set; }
            public bool exception { get; set; }
        }
        public class DebitResData
        {
            public string merchantno { get; set; }
            public string payurl { get; set; }
            public string scanimg { get; set; }
            public string orderno { get; set; }
            public string tjmoney { get; set; }
            public string money { get; set; }
            public string info { get; set; }
            public string sign { get; set; }
        }
        public class CreditRes
        {
            public bool success { get; set; }
            public string resultCode { get; set; }
            public string resultMsg { get; set; }
            public CreditResData data { get; set; }
        }
        public class CreditResData
        {
            public string merchantno { get; set; }
            public string type { get; set; }
            public string morderno { get; set; }
            public string orderno { get; set; }
            public string tjmoney { get; set; }
            public string money { get; set; }
            public string cardno { get; set; }
            public string bankcode { get; set; }
            public string realname { get; set; }
            public string status { get; set; }
            public string sign { get; set; }
        }


        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackCredit, bodyContent:{bodyContent}");

            var oNotify = new NotifyWithdrawResult()
            {
                WithdrawId = formData["morderno"]
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("merchantno", formData["merchantno"]);
            toMD5Data.Add("orderno", formData["orderno"]);
            toMD5Data.Add("bankcode", formData["bankcode"]);
            toMD5Data.Add("morderno", formData["morderno"]);
            toMD5Data.Add("cardno", formData["cardno"]);
            toMD5Data.Add("tjmoney", formData["tjmoney"]);
            toMD5Data.Add("money", formData["money"]);
            toMD5Data.Add("status", formData["status"]);
            toMD5Data.Add("md5key", payResource["key"]);

            var queryString = string.Join("|", toMD5Data.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString).ToLower();

            if (sign == formData["sign"])
            {
                if (formData["status"] == "3")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(formData["tjmoney"]);
                    oNotify.ThirdId = formData["orderno"];
                    oNotify.Message = "Success";
                    oNotify.Response = "ok";
                }
                else if (formData["status"] == "4")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = formData["orderno"];
                    oNotify.Message = "Fail";
                    oNotify.Response = "ok";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = formData["orderno"];
                    oNotify.Message = "Pending";
                    oNotify.Response = "ok";
                }
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{formData["sign"]}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = formData["orderno"];
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackDebit, bodyContent:{bodyContent}");

            var oNotify = new NotifyDepositResult()
            {
                DepositId = formData["morderno"]
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("merchantno", formData["merchantno"]);
            toMD5Data.Add("orderno", formData["orderno"]);
            toMD5Data.Add("morderno", formData["morderno"]);
            toMD5Data.Add("paycode", formData["paycode"]);
            toMD5Data.Add("tjmoney", formData["tjmoney"]);
            toMD5Data.Add("money", formData["money"]);
            toMD5Data.Add("status", formData["status"]);
            toMD5Data.Add("md5key", payResource["key"]);

            var queryString = string.Join("|", toMD5Data.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString).ToLower();

            if (sign == formData["sign"])
            {
                if (formData["status"] == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(formData["tjmoney"]);
                    oNotify.ActualAmount = decimal.Parse(formData["tjmoney"]);
                    oNotify.ThirdId = formData["orderno"];
                    oNotify.Comment = "Success";
                    oNotify.Message = "Success";
                    oNotify.Response = "ok";
                }
                else if (formData["status"] == "2")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = formData["orderno"];
                    oNotify.Comment = "Fail";
                    oNotify.Message = "Fail";
                    oNotify.Response = "ok";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = formData["orderno"];
                    oNotify.Message = "Pending";
                    oNotify.Comment = "Pending";
                    oNotify.Response = "ok";
                }
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{formData["sign"]}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = formData["orderno"];
                oNotify.Comment = "sign error";
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var oWithdrawResult = new WithdrawResult();
            var sUrl = inputs["host"] + "/api/withdraw";
            DateTime now = DateTime.Now;
            string dateTimeString = now.ToString("yyyyMMddHHmmss");

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("merchantno", inputs["merchantno"]);
            toMD5Data.Add("morderno  ", inputs["TransID"]);
            toMD5Data.Add("bankcode", inputs["BankCode"]);
            toMD5Data.Add("type", "0");
            toMD5Data.Add("realname", inputs["AccountName"]);
            toMD5Data.Add("cardno", inputs["AccountNo"]);
            toMD5Data.Add("money", inputs["Amount"]);
            toMD5Data.Add("sendtime", dateTimeString);
            toMD5Data.Add("buyerip ", inputs["ClientIP"]);
            toMD5Data.Add("md5key ", inputs["key"]);

            var queryString = string.Join("|", toMD5Data.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString).ToLower();

            var formData = new Dictionary<string, string>();
            formData.Add("merchantno", inputs["merchantno"]);
            formData.Add("morderno", inputs["TransID"]);
            formData.Add("type", "0");
            formData.Add("money", inputs["Amount"]);
            formData.Add("bankcode", inputs["BankCode"]);
            formData.Add("realname", inputs["AccountName"]);
            formData.Add("cardno", inputs["AccountNo"]);
            formData.Add("sendtime", dateTimeString);
            formData.Add("notifyurl", inputs["CallBackUrl"]);
            formData.Add("buyerip", inputs["ClientIP"]);
            formData.Add("sign", sign);

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"], false);
            var withdrawRes = JsonConvert.DeserializeObject<CreditRes>(sResponse);

            if (withdrawRes.success)
            {
                oWithdrawResult.Success = withdrawRes.success;
                oWithdrawResult.Message = "";
            }
            else
            {
                oWithdrawResult.Success = withdrawRes.success;
                oWithdrawResult.Message = withdrawRes.resultMsg;
            }

            return oWithdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var oDepositResult = new DepositResult();
            var sUrl = inputs["host"] + "/api/payment";
            DateTime now = DateTime.Now;
            string dateTimeString = now.ToString("yyyyMMddHHmmss");

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("merchantno", inputs["merchantno"]);
            toMD5Data.Add("morderno", inputs["TransID"]);
            toMD5Data.Add("paycode ", inputs["PayCode"]);
            toMD5Data.Add("notifyurl", inputs["CallBackUrl"]);
            toMD5Data.Add("money", inputs["Amount"]);
            toMD5Data.Add("sendtime", dateTimeString);
            toMD5Data.Add("buyerip", inputs["ClientIP"]);
            toMD5Data.Add("md5key", inputs["key"]);

            var queryString = string.Join("|", toMD5Data.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString).ToLower();

            var formData = new Dictionary<string, string>();
            formData.Add("merchantno", inputs["merchantno"]);
            formData.Add("morderno", inputs["TransID"]);
            formData.Add("productname", "Nova88");
            formData.Add("money", inputs["Amount"]);
            formData.Add("paycode", inputs["PayCode"]);
            formData.Add("sendtime", dateTimeString);
            formData.Add("notifyurl", inputs["CallBackUrl"]);
            formData.Add("buyerip", inputs["ClientIP"]);
            formData.Add("userinfo", inputs["UserName"]);
            formData.Add("sign", sign);

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"], false);
            var depositRes = JsonConvert.DeserializeObject<DebitRes>(sResponse);

            if (depositRes.success)
            {
                oDepositResult.Success = depositRes.success;
                oDepositResult.Message = "";
                oDepositResult.Url = depositRes.data.payurl;
            }
            else
            {
                oDepositResult.Success = depositRes.success;
                oDepositResult.Message = depositRes.resultMsg;
            }

            return oDepositResult;
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}