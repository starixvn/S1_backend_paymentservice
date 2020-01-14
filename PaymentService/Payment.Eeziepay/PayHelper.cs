using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Payment.Eeziepay
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
            formData.Add("service_version", "2.1");
            formData.Add("partner_code", inputs["merchant_no"]);
            formData.Add("currency", inputs["PayCurrencyId"]);
            formData.Add("partner_orderid", inputs["TransID"]);
            formData.Add("amount", decimal.Parse(inputs["Amount"]).ToString("#"));
            var time = new DateTimeOffset(DateTime.Now);

            formData.Add("trans_time", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            formData.Add("redirect_url", inputs["ReturnUrl"]);
            formData.Add("backend_url", inputs["CallBackUrl"]);
            formData.Add("bank_code", inputs["BankCode"]);
            formData.Add("member_ip", "");
            formData.Add("member_id", inputs["CustId"]);
            //formData.Add("key", inputs["key"]);
            //var dicP = FromJson<Dictionary<string, string>>(ToJson(oDebitReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = $"{formData["service_version"]}{formData["partner_code"]}{formData["partner_orderid"]}{formData["member_id"]}{formData["member_ip"]}{formData["currency"]}{formData["amount"]}{formData["backend_url"]}{formData["redirect_url"]}{formData["bank_code"]}{formData["trans_time"]}";
            formData.Add("sign", ToMD5(p).ToUpper());
           
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
