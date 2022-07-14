using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PaymentCore;

namespace Payment.DoitnowPay
{
    public class PayHelper : PayHelperBase
    {
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var amount = decimal.Parse(inputs["Amount"]);
            var formData = new Dictionary<string, string>()
            {
                { "clientId", inputs["clientId"] },
                { "transactionId", inputs["TransID"] },
                { "custName", inputs["CustId"] },
                { "amount", amount.ToString("#") },
                { "returnUrl", inputs["ReturnUrl"] },
                { "callbackUrl", inputs["CallBackUrl"] },
                { "paymentMethod", inputs["PayCode"] }
            };
            formData.Add("hashVal", Hash(formData, inputs["clientSecret"]));
            
            var oDepositResult = new DepositResult();
            oDepositResult.Url = sUrl;
            oDepositResult.FormData = formData;
            oDepositResult.Success = true;
            oDepositResult.Message = "";
            return oDepositResult;
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new System.NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new System.NotImplementedException();
        }
        
        public string Hash(Dictionary<string, string> para, string key)
        {
            var queryString = string.Join("", para.Select(p => p.Value).ToArray());
            return ToSHA256($"{queryString}{key}").ToLower();
        }
    }
}