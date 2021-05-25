using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Payment.AllpassPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        private class CallBackDebitReq
        {
            public string merchantNum { get; set; }
            public string orderNo { get; set; }
            public string platformOrderNo { get; set; }
            public string amount { get; set; }
            public string attch { get; set; }
            public string state { get; set; }
            public string payTime { get; set; }
            public string sign { get; set; }
            public string actualPayAmount { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            NotifyDepositResult oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            string p = $"{data.state}{data.merchantNum}{data.orderNo}{data.amount}{payResource["key"]}";
            string sign = ToMD5(p).ToLower();

            oNotify.DepositId = data.orderNo;
            oNotify.ThirdId = data.platformOrderNo;
            oNotify.Message = data.state.ToString();
            oNotify.Comment = data.state.ToString();

            if (sign == data.sign)
            {
                if (data.state == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.actualPayAmount);
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
                oNotify.Response = "fail";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawalRes
        {
            public string success { get; set; }
            public string msg { get; set; }
            public string code { get; set; }
            public string total { get; set; }
            public string timestamp { get; set; }
            public string data { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"] + "/withdraw";
            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("merchantNum", inputs["merchant_no"]);
            formData.Add("orderNo", inputs["TransID"]);
            formData.Add("bankCardAccount", inputs["AccountNo"]);
            formData.Add("accountHolder", inputs["AccountName"]);
            formData.Add("openAccountBank", inputs["BankCode"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("withdrawAmount", amount.ToString("#"));

            string p = $"{formData["merchantNum"]}{formData["orderNo"]}{formData["openAccountBank"]}{formData["bankCardAccount"]}{formData["accountHolder"]}{formData["withdrawAmount"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);
            withdrawResult.Message = withdrawalRes.msg;
            withdrawResult.RawContent = withdrawalRes.msg;

            if (withdrawalRes.code == "200")
            {
                withdrawResult.Success = true;
            }
            else
            {
                withdrawResult.Success = false;
            }

            

            return withdrawResult;
        }

        private class DepositRes
        {
            public string success { get; set; }
            public string msg { get; set; }
            public string code { get; set; }
            public string total { get; set; }
            public string timestamp { get; set; }
            public DepositResData data { get; set; }
        }

        public class DepositResData
        {
            public string id { get; set; }
            public string payUrl { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + "/startOrder";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("merchantNum", inputs["merchant_no"]);
            formData.Add("orderNo", inputs["TransID"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("payType", inputs["PayCode"]);
            formData.Add("notifyUrl", inputs["CallBackUrl"]);
            formData.Add("returnUrl", inputs["ReturnUrl"]);

            string p = $"{formData["merchantNum"]}{formData["orderNo"]}{formData["amount"]}{formData["notifyUrl"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.code == "200")
            {
                oDepositResult.Url = depositRes.data.payUrl;
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

        private class EnquiryWithdrawResultRes
        {
            public string success { get; set; }
            public string msg { get; set; }
            public string code { get; set; }
            public string total { get; set; }
            public string timestamp { get; set; }
            public EnquiryWithdrawResultResData data { get; set; }
        }

        private class EnquiryWithdrawResultResData
        {
            public string id { get; set; }
            public string orderNo { get; set; }
            public string withdrawAmount { get; set; }
            public string withdrawFee { get; set; }
            public string applyTime { get; set; }
            public string state { get; set; }
            public string note { get; set; }
            public string approvalTime { get; set; }
            public string confirmCreditedTime { get; set; }
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + "/withdrawState";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("merchantNum", inputs["merchant_no"]);
            formData.Add("orderNo", inputs["TransID"]);

            string p = $"{formData["merchantNum"]}{formData["orderNo"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            EnquiryWithdrawResultRes enquiryWithdrawResultRes = new EnquiryWithdrawResultRes();
            enquiryWithdrawResultRes = JsonConvert.DeserializeObject<EnquiryWithdrawResultRes>(sResponse);
            EnquiryWithdrawResult oNotify = new EnquiryWithdrawResult();

            if (enquiryWithdrawResultRes.code == "200")
            {
                oNotify.WithdrawId = enquiryWithdrawResultRes.data.orderNo;
                oNotify.ThirdId = enquiryWithdrawResultRes.data.id;
                oNotify.Message = enquiryWithdrawResultRes.data.state;

                if (enquiryWithdrawResultRes.data.state == "4")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(enquiryWithdrawResultRes.data.withdrawAmount);
                }
                else if (enquiryWithdrawResultRes.data.state == "3")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
            }

            return oNotify;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
