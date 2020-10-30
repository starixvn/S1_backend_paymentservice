using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Payment.AllInOnePay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawalReq
        {
            public string app_id { set; get; }
            public string success { set; get; }
            public string app_order_no { set; get; }
            public string order_no { set; get; }
            public string pay_amount { set; get; }
            public string pay_time { set; get; }
            public string note { set; get; }
            public string message { set; get; }
            public string sign { set; get; }
        }
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawalReq>(ToJson(formData));

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{payResource["merchant_passcode"]}";
            string sign = ToMD5(p).ToLower();

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.app_order_no;

            if (sign == data.sign)
            {
                if (data.success == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.pay_amount);
                    oNotify.ThirdId = data.order_no;
                    oNotify.Message = data.message;
                    oNotify.Response = "success";
                }
                else if (data.success == "0")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.order_no;
                    oNotify.Message = data.message;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.order_no;
                    oNotify.Message = data.message;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.order_no;
                oNotify.Message = data.message;
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string app_id { set; get; }
            public string success { set; get; }
            public string app_order_no { set; get; }
            public string order_no { set; get; }
            public string amount { set; get; }
            public string pay_amount { set; get; }
            public string pay_time { set; get; }
            public string note { set; get; }
            public string message { set; get; }
            public string sign { set; get; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{payResource["merchant_passcode"]}";
            string sign = ToMD5(p).ToLower();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.app_order_no;
            if (sign == data.sign)
            {
                if (data.success == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.ThirdId = data.order_no;
                    oNotify.Comment = data.message;
                    oNotify.Message = data.message;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.order_no;
                oNotify.Comment = data.message;
                oNotify.Message = data.message;
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class PayoutRes
        {
            public string success { get; set; }
            public string order_no { get; set; }
            public string app_order_no { get; set; }
            public string amount { get; set; }
            public string note { get; set; }
            public string message { get; set; }
            public string sign { get; set; }
        }
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            sUrl += "withdrawal/create";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("app_id", inputs["merchant_site_code"]);
            int length = inputs["TransID"].Length;
            string transid = "";
            if (length < 18)
            {
                for (int i = length; i < 18; i++)
                {
                    transid += "0";
                }
            }
            transid += inputs["TransID"];
            formData.Add("app_order_no", transid);
            formData.Add("app_user", inputs["CustId"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("card_name", inputs["AccountName"]);
            formData.Add("card_no", inputs["AccountNo"]);
            formData.Add("bank_id", inputs["BankCode"]);
            formData.Add("branc_name", "No Branch Name");
            formData.Add("notify_url", inputs["CallBackUrl"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{inputs["merchant_passcode"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());

            PayoutRes payoutRes = new PayoutRes();
            payoutRes = JsonConvert.DeserializeObject<PayoutRes>(sResponse);

            WithdrawResult withdrawResult = new WithdrawResult();
            if (payoutRes.success == "1")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = payoutRes.message;
                withdrawResult.RawContent = payoutRes.message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = payoutRes.message;
                withdrawResult.RawContent = payoutRes.message;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string success { get; set; }
            public string order_no { get; set; }
            public string app_order_no { get; set; }
            public string amount { get; set; }
            public string pay_url { get; set; }
            public string valid_time { get; set; }
            public string note { get; set; }
            public string message { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            sUrl += "deposit/create";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("app_id", inputs["merchant_site_code"]);
            formData.Add("app_order_no", inputs["TransID"].ToLower());
            formData.Add("type_id", inputs["PayCode"]);
            if (inputs["PayCode"] == "12" || inputs["PayCode"] == "13")
            {
                formData.Add("bank_id", inputs["BankCode"]);
            }
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("app_user", inputs["CustId"]);
            formData.Add("notify_url", inputs["CallBackUrl"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{inputs["merchant_passcode"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.success == "1")
            {
                oDepositResult.Url = depositRes.pay_url;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.ErrorCode = depositRes.success;
                oDepositResult.Message = depositRes.message;
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
