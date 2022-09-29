using PaymentCore;
using PaymentCore.Models.Result;

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
                { "custName", inputs["AccountName"] },
                { "amount", amount.ToString("#") },
                { "returnUrl", inputs["ReturnUrl"] },
                { "callbackUrl", inputs["CallBackUrl"] },
                { "paymentMethod", inputs["PayCode"] }
            };
            formData.Add("hashVal", Hash(formData, inputs["clientSecret"]));
            
            var oDepositResult = new DepositResult
            {
                Url = sUrl,
                FormData = formData,
                Success = true,
                Message = ""
            };
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
            var oNotify = new NotifyDepositResult();
            var status = formData["status"];
            var dict = new Dictionary<string, string>()
            {
                { "clientId", payResource["clientId"] }
            };
            
            if (status == "success")
            {
                dict.Add("referenceId", formData["referenceId"]);
                dict.Add("transactionId", formData["transactionId"]);
                dict.Add("custName", formData["custName"]);
                dict.Add("amount", formData["amount"]);
                dict.Add("status", formData["status"]);
            }
            else
            {
                dict.Add("transactionId", formData["transactionId"]);
                dict.Add("status", formData["status"]);
            }
            
            var hashVal = formData["hashVal"];
            var actual = Hash(dict, payResource["clientSecret"]);
            if (string.Equals(hashVal, actual))
            {
                
                oNotify.DepositId = formData["transactionId"];
                oNotify.Message = status;
                oNotify.Comment = status;
                if (status == "success")
                {
                    var amount = decimal.Parse(formData["amount"]);
                    oNotify.ThirdId = formData["referenceId"];
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = amount;
                    oNotify.ActualAmount = amount;
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
                oNotify.Response = "Signature error";
            }

            return oNotify;
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