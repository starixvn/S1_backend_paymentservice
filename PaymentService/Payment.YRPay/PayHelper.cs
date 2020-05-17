using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.YRPay
{

    class PayHelper : PayHelperBase
    {
        private class CallBackDebitReq
        {
            public string merid { set; get; }
            public string userid { set; get; }
            public string amount { set; get; }
            public string orderid { set; get; }
            public string ordertype { set; get; }
            public string status { set; get; }
            public string created { set; get; }
            public string paid { set; get; }
            public string paytype { set; get; }
            public string is_mobile { set; get; }
            public string is_wap { set; get; }
            public string extend { set; get; }
            public string orig_amount { set; get; }
            public string sign { set; get; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={payResource["key"]}";
            string sign = ToMD5(p);

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.orderid;
            oNotify.ThirdId = data.orderid;
            oNotify.Message = data.status;
            if (sign == data.sign)
            {
                if (data.status == "verified")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.Response = "success";
                }
                else if (data.status == "revoked")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={payResource["key"]}";
            string sign = ToMD5(p);

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.orderid;
            oNotify.ThirdId = data.orderid;
            oNotify.Comment = data.status;
            oNotify.Message = data.status;

            if (sign == data.sign)
            {
                if (data.status == "verified")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.orig_amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);

                    oNotify.Response = "success";
                }
                else if (data.status == "revoked")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
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

        private class CreditReq
        {
            public string merid { get; set; }
            public string userid { get; set; }
            public string amount { get; set; }
            public string orderid { get; set; }
            public string to_bankflag { get; set; }
            public string to_cardnumber { get; set; }
            public string to_cardname { get; set; }
            public string to_province { get; set; }
            public string to_city { get; set; }
            public string extend { get; set; }
            public string notifyurl { get; set; }
            public string sign { get; set; }
        }

        private class CreditResult
        {
            public bool success { get; set; }
            public string msg { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            CreditReq creditReq = new CreditReq();
            creditReq.merid = inputs["CID"];
            creditReq.orderid = inputs["TransID"];
            creditReq.userid = inputs["CustId"];
            decimal amount = decimal.Parse(inputs["Amount"]);
            creditReq.amount = amount.ToString("#");
            creditReq.to_cardname = inputs["AccountName"];
            creditReq.to_cardnumber = inputs["AccountNo"];
            creditReq.to_province = "";
            creditReq.to_city = "";
            creditReq.extend = "";
            creditReq.to_bankflag = inputs["BankCode"];
            creditReq.notifyurl = inputs["CallBackUrl"];

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string P = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={inputs["key"]}";
            dicP.Add("sign", ToMD5(P).ToUpper());
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.orderid);

            WithdrawResult withdrawResult = new WithdrawResult();
            CreditResult creditRes = new CreditResult();
            creditRes = JsonConvert.DeserializeObject<CreditResult>(sResponse);

            if (creditRes.success)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = creditRes.msg;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = creditRes.msg;
            }

            return withdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("merid", inputs["CID"]);
            formData.Add("userid", inputs["CustId"]);
            formData.Add("amount", Convert.ToInt32(decimal.Parse(inputs["Amount"])).ToString());
            formData.Add("orderid", inputs["TransID"]);
            formData.Add("userip", inputs["ClientIP"]);
            formData.Add("notifyurl", inputs["CallBackUrl"]);
            if (inputs["PayCode"] != null)
            {
                if (inputs["PayCode"] =="ALIPAY" || inputs["PayCode"] == "WXPAY" || inputs["PayCode"] == "UNIPAY")
                    formData.Add("paytype", "qrcode");
                else
                    formData.Add("paytype", "quick");
                formData.Add("bankflag", inputs["PayCode"]);
            }
            else
            {
                formData.Add("paytype", "online");
                formData.Add("bankflag", inputs["BankCode"]);
            }
            
            formData.Add("syncurl", "");
            formData.Add("cardname", "");
            formData.Add("extend", "");

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={inputs["key"]}"; 
            formData.Add("sign", ToMD5(p));

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
