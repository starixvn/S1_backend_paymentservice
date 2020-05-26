using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Payment.PatekPay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawalReq
        {
            public string transferno { get; set; }
            public string outtransferno { get; set; }
            public string tradeamount { get; set; }
            public string transferamount { get; set; }
            public string endtime { get; set; }
            public string remark { get; set; }
            public string status { get; set; }
            public string sign { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawalReq>(ToJson(formData));
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&secret={payResource["key"]}";

            string sign = ToMD5(p);

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.outtransferno;
            oNotify.ThirdId = data.transferno;
            oNotify.Message = data.status;
            if (sign == data.sign)
            {
                if (data.status == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.transferamount);
                    oNotify.Response = "success";
                }
                else if (data.status == "0" || data.status == "2")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
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
            public string tradeno { get; set; }
            public string outtradeno { get; set; }
            public string amount { get; set; }
            public string ramount { get; set; }
            public string endtime { get; set; }
            public string status { get; set; }
            public string remark { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            data.endtime = System.Web.HttpUtility.UrlEncode(data.endtime).ToLower();
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&secret={payResource["key"]}";
            string sign = ToMD5(p).ToLower();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.outtradeno;
            oNotify.ThirdId = data.tradeno;
            oNotify.Comment = data.status;
            oNotify.Message = data.status;

            if (sign == data.sign)
            {
                if (data.status == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.Response = "success";
                }
                else if (data.status == "0")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "success";
                }
                else 
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
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
   
            formData.Add("merchant", inputs["merchant_no"]);
            formData.Add("outtransferno", inputs["TransID"].ToLower());
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("bankname", UrlEncode(inputs["BankName"]).ToLower());
            formData.Add("cardname", UrlEncode(inputs["AccountName"]).ToLower());
            formData.Add("cardno", inputs["AccountNo"]);
            formData.Add("notifyurl", UrlEncode(inputs["CallBackUrl"]).ToLower());


            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&secret={inputs["key"]}";
            formData.Add("sign", ToMD5(p));
            formData["bankname"] = inputs["BankName"];
            formData["cardname"] = inputs["AccountName"];
            formData["notifyurl"] = inputs["CallBackUrl"];

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["outtransferno"]);

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
            formData.Add("merchant", inputs["merchant_no"]);
            formData.Add("paytype", inputs["PayCode"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("notifyurl", UrlEncode(inputs["CallBackUrl"]).ToLower());
            formData.Add("outtradeno", inputs["TransID"].ToLower());
            formData.Add("payername", "");
            formData.Add("returndataformat", "clienthtml");

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&secret={inputs["key"]}";
            formData.Add("sign", ToMD5(p));
            formData["notifyurl"] = inputs["CallBackUrl"];

            DepositResult oDepositResult = new DepositResult();
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

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
