using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Payment.HonorPay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawalReq
        {
            public string account { get; set; }
            public string amount { get; set; }
            public string fee { get; set; }
            public string orderNo { get; set; }
            public string nonceStr { get; set; }
            public string uuid { get; set; }
            public string status { get; set; }
            public string sign { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawalReq>(ToJson(formData));
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={payResource["key"]}";

            string sign = ToHMACSHA256(p, payResource["key"]).ToUpper();

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.orderNo;
            oNotify.ThirdId = data.uuid;
            oNotify.Message = data.status;
            if (sign == data.sign)
            {
                if (data.status == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.Response = "888888";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "888888";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string account { get; set; }
            public string nonceStr { get; set; }
            public string orderNo { get; set; }
            public string payMoney { get; set; }
            public string payStatus { get; set; }
            public string realCharge { get; set; }
            public string uuid { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={payResource["key"]}";

            string sign = ToHMACSHA256(p, payResource["key"]).ToUpper();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.orderNo;
            oNotify.ThirdId = data.uuid;
            oNotify.Comment = data.payStatus;
            oNotify.Message = data.payStatus;

            if (sign == data.sign)
            {
                if (data.payStatus == "success")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.payMoney);
                    oNotify.ActualAmount = decimal.Parse(data.payMoney);
                    oNotify.Response = "888888";
                }
                else 
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "888888";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }


        private class WithdrawalRes
        {
            public string retCode { get; set; }
            public string uuid { get; set; }
            public string orderNo { get; set; }
            public string amount { get; set; }
            public string fee { get; set; }
            public string retMsg { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
   
            formData.Add("account", inputs["merchant_no"]);
            formData.Add("orderNo", inputs["TransID"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("bankName", UrlEncode(inputs["BankName"]));
            formData.Add("bankHolder", UrlEncode(inputs["AccountName"]));
            formData.Add("bankNumber", inputs["AccountNo"]);
            formData.Add("bankBranch", "");
            formData.Add("bankProvince", "");
            formData.Add("bankCity", "");
            formData.Add("nonceStr", GetRandomString4(20));
            formData.Add("notifyURL", UrlEncode(inputs["CallBackUrl"]));


            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={inputs["key"]}";
            formData.Add("sign", ToHMACSHA256(p, inputs["key"]).ToUpper());
            formData["bankName"] = inputs["BankName"];
            formData["bankHolder"] = inputs["AccountName"];
            formData["notifyURL"] = inputs["CallBackUrl"];

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["token"], formData["orderNo"]);

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.retCode == "0")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.retCode;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.retMsg;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string retCode { get; set; }
            public string uuid { get; set; }
            public string qrContent { get; set; }
            public string realCharge { get; set; }
            public string bankName { get; set; }
            public string bankBranch { get; set; }
            public string bankNumber { get; set; }
            public string bankHolder { get; set; }
            public string number { get; set; }
            public string verifyName { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("account", inputs["merchant_no"]);
            formData.Add("payType", inputs["PayCode"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("payMoney", amount.ToString("#"));
            formData.Add("ip", inputs["ClientIP"]);
            formData.Add("notifyURL", inputs["CallBackUrl"]);
            formData.Add("orderNo", inputs["TransID"]);
            formData.Add("bankName", inputs["BankName"]);

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["token"], formData["orderNo"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.retCode == "0")
            {
                oDepositResult.Url = depositRes.qrContent;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.retCode;
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
