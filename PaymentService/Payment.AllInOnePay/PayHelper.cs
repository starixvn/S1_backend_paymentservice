using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Models.Result;

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
            var p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{payResource["merchant_passcode"]}";
            var sign = ToMD5(p).ToLower();

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.app_order_no;

            //if (sign == data.sign)
            //{
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
            //}
            //else
            //{
            //    oNotify.TransStatus = TransStatus.Pending;
            //    oNotify.ThirdId = data.order_no;
            //    oNotify.Message = data.message;
            //    oNotify.Response = "sign error";
            //}

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
            var p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{payResource["merchant_passcode"]}";
            var sign = ToMD5(p).ToLower();

            var oNotify = new NotifyDepositResult();
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
            var sUrl = inputs["host"];
            sUrl += "withdrawal/create";

            var formData = new Dictionary<string, string> { { "app_id", inputs["merchant_site_code"] } };
            var length = inputs["TransID"].Length;
            var transid = "";
            if (length < 18)
            {
                for (var i = length; i < 18; i++)
                {
                    transid += "0";
                }
            }
            transid += inputs["TransID"];
            formData.Add("app_order_no", transid);
            formData.Add("app_user", inputs["CustId"]);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("card_name", inputs["AccountName"]);
            formData.Add("card_no", inputs["AccountNo"]);
            formData.Add("bank_id", inputs["BankCode"]);
            formData.Add("branc_name", "No Branch Name");
            formData.Add("notify_url", inputs["CallBackUrl"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{inputs["merchant_passcode"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());

            var payoutRes = new PayoutRes();
            payoutRes = JsonConvert.DeserializeObject<PayoutRes>(sResponse);

            var withdrawResult = new WithdrawResult();
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
            var sUrl = inputs["host"];
            sUrl += "deposit/create";

            var formData = new Dictionary<string, string>
            {
                { "app_id", inputs["merchant_site_code"] },
                { "app_order_no", inputs["TransID"].ToLower() },
                { "type_id", inputs["PayCode"] }
            };
            if (inputs["PayCode"] == "12" || inputs["PayCode"] == "13")
            {
                formData.Add("bank_id", inputs["BankCode"]);
            }
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("app_user", inputs["CustId"]);
            formData.Add("notify_url", inputs["CallBackUrl"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"{inputs["merchant_passcode"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
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
