using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Payment.Help2Pay
{
    public class PayHelper : PayHelperBase
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

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("Merchant", inputs["merchant_no"]);
            formData.Add("Currency", inputs["PayCurrencyId"]);
            formData.Add("Reference", inputs["TransID"]);
            formData.Add("Amount", decimal.Parse(inputs["Amount"]).ToString("#.00"));
            var time = new DateTimeOffset(DateTime.Now);

            formData.Add("Datetime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd hh:mm:sstt", CultureInfo.InvariantCulture));
            formData.Add("FrontURI", inputs["ReturnUrl"]);
            formData.Add("BackURI", inputs["CallBackUrl"]);
            formData.Add("Bank", inputs["BankCode"]);
            formData.Add("Language", inputs["PayLangId"]);
            formData.Add("ClientIP", inputs["ClientIP"]);
            formData.Add("Customer", "");
            //formData.Add("key", inputs["key"]);
            //var dicP = FromJson<Dictionary<string, string>>(ToJson(oDebitReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = $"{formData["Merchant"]}{formData["Reference"]}{""}{formData["Amount"]}{formData["Currency"]}{time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss")}{inputs["key"]}{formData["ClientIP"]}";
            formData.Add("Key", ToMD5(p).ToUpper());

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
