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

            string p = $"service_version={formData["service_version"]}&partner_code={formData["partner_code"]}&partner_orderid={formData["partner_orderid"]}&" +
                $"member_id={formData["member_id"]}&member_ip={formData["member_ip"]}&currency={formData["currency"]}&amount={formData["amount"]}&" +
                $"backend_url={formData["backend_url"]}&redirect_url={formData["redirect_url"]}&bank_code={formData["bank_code"]}&trans_time={formData["trans_time"]}&key={inputs["key"]}";
            formData.Add("sign", ToSHA1(p).ToUpper());
           
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
