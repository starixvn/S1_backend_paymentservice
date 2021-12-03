using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.TruePay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawReq
        {
            public string id { set; get; }
            public int status { set; get; }
            public string amount { set; get; }
            public string fee { set; get; }
            public string message { set; get; }
            public string token { set; get; }
            public string ts { set; get; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            string p = $"{payResource["Merchant"]}-{payResource["key"]}-{data.id}-{data.amount}-{data.ts}";
            string sign = ToMD5(p).ToLower();

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.id;

            //if (sign == data.token)
            //{
            if (data.status == 1)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ThirdId = data.id;
                oNotify.Message = data.status + "-" + data.message;
                oNotify.Response = "OK";
            }
            else if (data.status == -1)
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.id;
                oNotify.Message = data.status + "-" + data.message;
                oNotify.Response = "OK";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.id;
                oNotify.Message = data.status + "-" + data.message;
                oNotify.Response = "OK";
            }
            //}
            //else
            //{
            //    oNotify.TransStatus = TransStatus.Pending;
            //    oNotify.ThirdId = data.id;
            //    oNotify.Message = "Signature error";
            //    oNotify.Response = "Signature error";
            //}

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string id { get; set; }
            public int status { get; set; }
            public string amount { get; set; }
            public string fee { get; set; }
            public string message { get; set; }
            public string token { get; set; }
            public string ts { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            NotifyDepositResult oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            string p = $"{payResource["Merchant"]}-{payResource["key"]}-{data.id}-{data.amount}-{data.ts}";
            string sign = ToMD5(p).ToLower();

            oNotify.DepositId = data.id;
            oNotify.ThirdId = data.id;
            oNotify.Message = data.message;

            //if (sign == data.token)
            //{
            if (data.status == 1)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ActualAmount = decimal.Parse(data.amount);
                oNotify.Message = data.message;
                oNotify.Comment = $"{data.status}-{data.message}";
                oNotify.Response = "OK";
            }
            else if (data.status == -1)
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Message = data.message;
                oNotify.Comment = $"{data.status}-{data.message}";
                oNotify.Response = "OK";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = data.message;
                oNotify.Comment = $"{data.status}-{data.message}";
                oNotify.Response = "OK";
            }
            //}
            //else
            //{
            //    oNotify.TransStatus = TransStatus.Pending;
            //    oNotify.Comment = "Signature error";
            //    oNotify.Message = "Signature error";
            //    oNotify.Response = "Signature error";
            //}

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawalRes
        {
            public int status { get; set; }
            public string msg { get; set; }
            public WithdrawalResData d { get; set; }
        }

        public class WithdrawalResData
        {
            public string id { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + "/a/sPayout";

            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("mCode", inputs["Merchant"]);
            formData.Add("ts", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
            formData.Add("bCode", inputs["BankCode"]);
            formData.Add("trxid", inputs["TransID"]);
            formData.Add("user", inputs["CustId"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amt", amount.ToString("#"));
            formData.Add("bAccName", inputs["AccountName"]);
            formData.Add("bAccNo", inputs["AccountNo"]);
            formData.Add("notifyUrl", inputs["CallBackUrl"]);

            string p = $"{formData["mCode"]}-{inputs["key"]}-sPayout-{formData["ts"]}";
            formData.Add("token", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["trxid"]);

            sResponse = sResponse.Replace("[", "{");
            sResponse = sResponse.Replace("]", "}");

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.status == 0)
            {
                withdrawResult.Success = true;
            }
            else
            {
                withdrawResult.Success = false;
            }

            withdrawResult.Message = withdrawalRes.msg;

            return withdrawResult;
        }

        private class DepositRes
        {
            public int status { get; set; }
            public string msg { get; set; }
            public DepositResData d { get; set; }
        }

        public class DepositResData
        {
            public int id { get; set; }
            public string token { get; set; }
            public string url { get; set; }

        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + "/a/getDPLink";
            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("mCode", inputs["Merchant"]);
            formData.Add("ts", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
            if (inputs["PayCode"] != null)
                formData.Add("bCode", inputs["PayCode"]);
            else
                formData.Add("bCode", inputs["BankCode"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amt", amount.ToString("#"));
            formData.Add("user", inputs["CustId"]);
            formData.Add("trxid", inputs["TransID"]);
            formData.Add("notifyUrl", inputs["CallBackUrl"]);
            formData.Add("webUrl", inputs["ReturnUrl"]);

            string p = $"{formData["mCode"]}-{inputs["key"]}-getDPLink-{formData["ts"]}";
            formData.Add("token", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["trxid"]);

            sResponse = sResponse.Replace("[", "{");
            sResponse = sResponse.Replace("]", "}");

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.status == 0)
            {
                oDepositResult.ThirdId = depositRes.d.id.ToString();
                oDepositResult.Url = depositRes.d.url;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.msg;
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
