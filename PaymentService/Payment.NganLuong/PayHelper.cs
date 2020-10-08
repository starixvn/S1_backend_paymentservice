using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Payment.NganLuong
{
    class PayHelper : PayHelperBase
    {
        public override ExchangeResult GetExchange(Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        private class CallBackDebitReq
        {
            public string token_code { set; get; }
            public string version { set; get; }
            public string order_code { set; get; }
            public string order_description { set; get; }
            public string amount { set; get; }
            public string currency { set; get; }
            public string payment_method_code { set; get; }
            public string payment_method_name { set; get; }
            public string sender_fee { set; get; }
            public string receiver_fee { set; get; }
            public string status { set; get; }
            public string return_url { set; get; }
            public string cancel_url { set; get; }
            public string notify_url { set; get; }
            public string checksum { set; get; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            string p = $"{data.token_code}|{data.version}|{data.order_code}|{data.order_description}|{data.amount}|{data.sender_fee}|{data.receiver_fee}|{data.currency}|" +
                        $"{data.return_url}|{data.cancel_url}|{data.notify_url}|{data.status}|{data.payment_method_code}|{data.payment_method_name}|{payResource["merchant_passcode"]}";
            string sign = ToMD5(p).ToLower();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.order_code;
            if (sign == data.checksum)
            {
                if (data.status == "3")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.ThirdId = data.order_code;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "{\"result_code\":\"0000\",\"result_message\":\"OK\"}";
                }
                else if (data.status == "4" || data.status == "7")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.order_code;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "{\"result_code\":\"0000\",\"result_message\":\"OK\"}";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.order_code;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "{\"result_code\":\"0000\",\"result_message\":\"OK\"}";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.order_code;
                oNotify.Comment = data.status;
                oNotify.Message = data.status;
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
            throw new NotImplementedException();
        }

        private class DepositRes
        {
            public string result_code { get; set; }
            public string result_message { get; set; }
            public DepositResData result_data { get; set; }
        }

        public class DepositResData
        {
            public string checkout_url { get; set; }
            public string token_code { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("function", "CreateOrder");
            formData.Add("merchant_site_code", inputs["merchant_site_code"]);
            formData.Add("order_code", inputs["TransID"].ToLower());
            
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));

            formData.Add("currency", inputs["PayCurrencyId"]);
            formData.Add("buyer_fullname", "N/A");
            formData.Add("buyer_email", "nobody@nobody.com");
            formData.Add("buyer_address", "N/A");
            formData.Add("buyer_mobile", "0948888123");
            formData.Add("return_url", inputs["ReturnUrl"]);
            formData.Add("cancel_url", inputs["ReturnUrl"]);
            formData.Add("notify_url", inputs["CallBackUrl"]); 
            formData.Add("language", inputs["PayLangId"]);
            formData.Add("order_description", "");

            string p = $"{formData["merchant_site_code"]}|{formData["order_code"]}|{formData["order_description"]}|{formData["amount"]}|{formData["currency"]}|{formData["buyer_fullname"]}|{formData["buyer_email"]}|" +
                        $"{formData["buyer_mobile"]}|{formData["buyer_address"]}|{formData["return_url"]}|{formData["cancel_url"]}|{formData["notify_url"]}|{formData["language"]}|{inputs["merchant_passcode"]}";
            formData.Add("checksum", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["order_code"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.result_code == "0000")
            {
                    oDepositResult.Url = depositRes.result_data.checkout_url;
                    oDepositResult.Success = true;
                    oDepositResult.FormData = null;
                    oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.result_message;
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
