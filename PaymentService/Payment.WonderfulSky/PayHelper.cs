using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static PaymentCore.RSAHelper;

namespace Payment.WonderfulSky
{
    class PayHelper : PayHelperBase
    {
        private readonly string publicKey = "MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAlPl9FrmjiXGJUkgYMtnfaMm08BdmUHqxy0IIfk1oXTvT40F9hl41D4xvxmnYImHk2B0BOLTQJCkDOacBUJMTjzhCD7ptpQEjS2fBw+OKp81J6n2XlD6yy9XmYweojc3dCvLhveDVYBsE6xZVor4LqL9klTDsZ5WKtRbe2lpUBA5rWVuDl9OThOCTHYqc+79qMnSNBTA1Cn0r9JXFt4bTggqzVoqGAVI63DDmJXGcTx8//Dpc/eyflzk8wx4uLdOgDHBLMzIJnLSiZwDA3tcIK/1dWE2WyHSnwgPfTXBGW02ztC39zD0nlC3vK09GBxFGhDIQhLBlvS6EN+hSrHzW9WiysoKO+W3WT/SrAA9RZ/RtSb/uRhJ6bUle/WtIXQOjE/JAUCPE3Z7kAQtKF2sPWu85WW3K6y0Bgx+1TCNrxMnENeyb0FyKnmUWOCz/1A7E5dT5X3gTnVofKOgi14YI6QQ4OXRZSI76sOPrL4s+twauhF5QwbbbLhRO2H3kbdSOh5RjHZ/35ZMILk6Q61XzZLK/YSxbV9S/ZyJhk+ft0LlP84gTBKPKCnq3wfAp8hZ5ohuIxlsfKBohqu/HNqKYN8c6m1Ww5fUL2ZSXLPPhcluvDGpDeEju1EJGlUJdZMR9rdfxOFLMY2ZnuKGPPsLXj+2HG3zBgBOwDp8DkmtrcIECAwEAAQ==";
        private readonly string privateKey = "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";

        //private class CallBackWithdrawalReq
        //{
        //    public string requestId { get; set; }
        //    public string result { get; set; }
        //    public string sum { get; set; }
        //    public string totalSum { get; set; }
        //    public string balance { get; set; }
        //    public string canUsedBalance { get; set; }
        //    public string frozenBalance { get; set; }
        //    public string unsettledBalance { get; set; }
        //    public string payType { get; set; }
        //    public string bankAccount { get; set; }
        //    public string bankName { get; set; }
        //    public string name { get; set; }
        //    public string requestTime { get; set; }
        //    public string dealTime { get; set; }
        //    public string noticeType { get; set; }
        //    public string failReason { get; set; }
        //    public string signature { get; set; }
        //}

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<WithdrawalRes>(ToJson(formData));
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={payResource["key"]}";

            var rsa = new RSAHelper(RSAType.RSA, Encoding.Unicode, privateKey, publicKey);
            string sign = rsa.Encrypt(p);

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.merchantid;
            oNotify.ThirdId = data.merchantid;
            oNotify.Message = data.code;

            if (sign == data.sign)
            {
                if (data.result == "1000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.Response = "success";
                }
                else if (data.result == "0000")
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
            public string mid { get; set; }
            public string merchantid { get; set; }
            public string systemid { get; set; }
            public string amount { get; set; }
            public string status { get; set; }
            public string time { get; set; }
            public string remark { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            string p = $"amount={data.amount}&merchantid={data.merchantid}&mid={data.mid}&status={data.status}&" +
                         $"systemid={data.systemid}&time={data.time}&{payResource["key"]}";

            var rsa = new RSAHelper(RSAType.RSA, Encoding.Unicode, privateKey, publicKey);
            var sign= rsa.Encrypt(p);

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.merchantid;
            oNotify.ThirdId = data.systemid;
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
            public string result { get; set; }
            public string mid { get; set; }
            public string merchantid { get; set; }
            public string systemid { get; set; }
            public string amount { get; set; }
            public string code { get; set; }
            public string sign { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("service", "Withdraw");
            formData.Add("mid", inputs["merchant_no"]);
            formData.Add("merchantid", inputs["TransID"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("currency", "CNY");
            formData.Add("AccountNo", inputs["ReturnUrl"]);
            formData.Add("cardname", inputs["AccountName"]);
            formData.Add("cardtype", "1");
            formData.Add("bankcode", inputs["BankCode"]);
            formData.Add("bankname", inputs["BankName"]);
            formData.Add("bankprovince", "");
            formData.Add("bankcity","");
            formData.Add("channel", "bank");
            //“bank”: 網銀 “alipay”: 支付寶 “wechat”:微信 “qqpay”: QQ支付

            string p = $"amount={formData["amount"]}&bankbranchname={formData["bankbranchname"]}&ankcity={formData["bankcity"]}&bankcode={formData["bankcode"]}&" +
                        $"bankname={formData["bankname"]}&bankprovince={formData["bankprovince"]}&cardname={formData["cardname"]}&cardno={formData["cardno"]}&" +
                        $"cardtype={formData["cardtype"]}&channel={formData["channel"]}&currency={formData["currency"]}&merchantid={formData["merchantid"]}&" +
                        $"mid={formData["mid"]}&service={formData["service"]}&{inputs["key"]}";

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            //string p = string.Join("&", dicP.Where(x => !x.Key.Equals("remark", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&{inputs["key"]}";


            var rsa = new RSAHelper(RSAType.RSA, Encoding.Unicode, privateKey, publicKey);
            formData.Add("sign", rsa.Encrypt(p));

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["merchantid"]);

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.code == "00000" || withdrawalRes.code == "00001")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.result;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.result;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string result { get; set; }
            public string qrcodeurl { get; set; }
            public string mid { get; set; }
            public string merchantid { get; set; }
            public string sign { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("service", "Payment");
            formData.Add("mid", inputs["merchant_no"]);
            formData.Add("merchantid", inputs["TransID"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("currency", "CNY");
            formData.Add("returnurl", inputs["ReturnUrl"]);
            formData.Add("notifyurl", inputs["CallBackUrl"]);
            formData.Add("channel", inputs["PayCode"]);
            formData.Add("remark", "");

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Key.Equals("remark", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&{inputs["key"]}";

            var rsa = new RSAHelper(RSAType.RSA, Encoding.UTF8, privateKey, publicKey);
            formData.Add("sign", rsa.Sign(p));

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["merchantid"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.result == "1")
            {
                oDepositResult.Url = depositRes.qrcodeurl;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
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
