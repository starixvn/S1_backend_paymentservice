using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.FattyPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
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
            public int status { get; set; }
            public string msg { get; set; }
        }
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            sUrl += "/api/v2/deposit/request";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("ServiceProvider", "JSTPay");
            formData.Add("PaymentMethod", inputs["PayCode"]);
            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("MerchantTransactionId", inputs["TransID"]);
            formData.Add("BankCode", inputs["BankCode"]);
            formData.Add("IsoCurrency", inputs["PayCurrencyId"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#0.0000"));
            formData.Add("FirstName", "FirstName");
            formData.Add("LastName", "LastName");
            formData.Add("FullName", "FullName");
            formData.Add("Email", "test@test.com");
            formData.Add("CustomerId", inputs["CustId"]);
            formData.Add("Language", inputs["PayLangId"]);
            formData.Add("SuccessUrl", inputs["ReturnUrl"]);
            formData.Add("FailUrl", inputs["ReturnUrl"]);
            formData.Add("NotifyUrl", inputs["CallBackUrl"]);
            formData.Add("HomeUrl", inputs["ReturnUrl"]);
            formData.Add("RequestIp", inputs["ClientIP"]);

            string p = $"{inputs["key"]}&{formData["MerchantTransactionId"]}&{formData["ServiceProvider"]}&{formData["PaymentMethod"]}&{formData["IsoCurrency"]}&" +
                        $"{formData["Amount"]}&{formData["Email"]}";

            formData.Add("Signature", ToSHA512(p).ToUpper());
            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"]);
           
            DepositResult oDepositResult = new DepositResult();
            if (sResponse.Contains("http"))
            {
                oDepositResult.Success = true;
				oDepositResult.Message = "";
				oDepositResult.Url = sResponse;
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
