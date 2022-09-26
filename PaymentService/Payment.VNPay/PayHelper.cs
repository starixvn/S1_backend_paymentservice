using PaymentCore;
using Newtonsoft.Json;

namespace Payment.VNPay
{
    public class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public class CallBackDebitReq
        {
            public string result_code { set; get; }
            public string merchant_no { set; get; }
            public string order_no { set; get; }
            public string ylt_order_no { set; get; }
            public string amount { set; get; }
            public string channel { set; get; }
            public string extra_param { set; get; }
            public string sign { set; get; }
            public string user_amount { set; get; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var merchant_no = payResource["merchant_no"];
            var key = payResource["key"];
            var amount = decimal.Parse(data.amount);

            var p = $"{data.merchant_no}|{data.order_no}|{data.ylt_order_no}|{data.amount}|{data.channel}|{key}";
            var sign = ToMD5(p).ToLower();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.order_no;
            if (sign == data.sign)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ActualAmount = decimal.Parse(data.amount);
                oNotify.ThirdId = data.ylt_order_no;
                oNotify.Comment = data.result_code;
                oNotify.Message = data.extra_param;
                oNotify.Response = "success";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Comment = data.result_code;
                oNotify.Message = data.extra_param;
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>
            {
                { "merchant_no", inputs["merchant_no"] },
                { "order_no", inputs["TransID"] }
            };
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("channel", inputs["PayCode"]);
            formData.Add("bank_code", inputs["BankCode"]);
            formData.Add("notify_url", inputs["CallBackUrl"]);
            formData.Add("result_url", inputs["ReturnUrl"]);
            formData.Add("c_ip", "");
            formData.Add("extra_param", "");

            var p = $"{formData["merchant_no"]}|{formData["order_no"]}|{formData["amount"]}|{formData["channel"]}|{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var oDepositResult = new DepositResult();
            oDepositResult.Url = sUrl;
            oDepositResult.FormData = formData;
            oDepositResult.Success = true;
            oDepositResult.Message = "";
            return oDepositResult;
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class CreditReq
        {
            public string merchant_no { get; set; }
            public string order_no { get; set; }
            public string amount { get; set; }
            public string notify_url { get; set; }
            public string channel { get; set; }
            public string sign { get; set; }
        }

        private class CreditResult
        {
            public string message { get; set; }
            public string status { get; set; }
            public string info { get; set; }
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["CheckURL"];
            var creditReq = new CreditReq();
            creditReq.merchant_no = inputs["merchant_no"];
            creditReq.order_no = inputs["TransID"];
            creditReq.amount = inputs["Amount"]; ;
            creditReq.channel = inputs["PayCode"];

            var p = $"{creditReq.merchant_no}|{creditReq.order_no}|{creditReq.amount}|{creditReq.channel}|{inputs["key"]}";
            creditReq.sign = ToMD5(p).ToLower();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.order_no);

            var creditResult = new CreditResult();
            creditResult = JsonConvert.DeserializeObject<CreditResult>(sResponse);

            var enquiryDepositResult = new EnquiryDepositResult();
            if (creditResult.status == "1" )
            {
                enquiryDepositResult.TransStatus = TransStatus.Success;
                enquiryDepositResult.Message = creditResult.message;
                enquiryDepositResult.RawContent = creditResult.info;
            }
            else
            {
                enquiryDepositResult.TransStatus = TransStatus.Fail;
                enquiryDepositResult.Message = creditResult.message;
                enquiryDepositResult.RawContent = creditResult.info;
            }

            return enquiryDepositResult;
        }
    }
}
