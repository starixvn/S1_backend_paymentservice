using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace Payment.Help2Pay
{
    public class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public class CallBackDebitReq
        {
            public string ID { get; set; }
            public string Amount { get; set; }
            public string Currency { get; set; }
            public string Customer { get; set; }
            public string Language { get; set; }
            public string Merchant { get; set; }
            public string Reference { get; set; }
            public string Datetime { get; set; }
            public string Status { get; set; }
            public string Key { get; set; }
            public string Note { get; set; }
            public string StatementDate { get; set; }
            public string DepositFee { get; set; }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            //new DateTimeOffset()
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            //var json = FromJson<Dictionary<string, string>>(payResource["JSONstr"]);
            string merchant_no = payResource["merchant_no"];
            string key = payResource["key"];
            string p = $"{merchant_no}{data.Reference}{data.Customer}{data.Amount}{data.Currency}{data.Status}{key}";
            string sign = ToMD5(p).ToUpper();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.Reference;
            if (sign == data.Key)
            {
                if (data.Status == "000" || data.Status == "006")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.ActualAmount = decimal.Parse(data.Amount) - decimal.Parse(data.DepositFee);
                    oNotify.ThirdId = data.ID;
                    oNotify.Comment = data.Status;
                    oNotify.Message = data.Status;
                    oNotify.Response = "OK";
                }
                else if (data.Status == "001" || data.Status == "007" || data.Status == "008")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.ID;
                    oNotify.Comment = data.Status;
                    oNotify.Message = data.Status;
                    oNotify.Response = "Fail";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.ID;
                    oNotify.Comment = data.Status;
                    oNotify.Message = data.Status;
                    oNotify.Response = "Pending";
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

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class CreditReq
        {
            public string ClientIP { get; set; }
            public string ReturnURI { get; set; }
            public string MerchantCode { get; set; }
            public string TransactionID { get; set; }
            public string CurrencyCode { get; set; }
            public string MemberCode { get; set; }
            public string Amount { get; set; }
            public string TransactionDateTime { get; set; }
            public string BankCode { get; set; }
            public string toBankAccountName { get; set; }
            public string toBankAccountNumber { get; set; }
        }

        public class Payout
        {
            public string statusCode { set; get; }
            public string message { set; get; }
        }
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            CreditReq creditReq = new CreditReq();
            creditReq.ClientIP = inputs["OutgoingIp"];
            creditReq.ReturnURI = inputs["CallBackUrl"];
            creditReq.MerchantCode = inputs["merchant_no"];
            creditReq.TransactionID = inputs["TransID"];
            creditReq.CurrencyCode = inputs["PayCurrencyId"];
            creditReq.MemberCode = inputs["CustId"];
            creditReq.Amount = decimal.Parse(inputs["Amount"]).ToString("#.00");
            var time = new DateTimeOffset(DateTime.Now);
            creditReq.TransactionDateTime = time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd hh:mm:sstt", CultureInfo.InvariantCulture);
            creditReq.BankCode = inputs["BankCode"];
            creditReq.toBankAccountName = inputs["AccountName"];
            creditReq.toBankAccountNumber = inputs["AccountNo"];

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            string P = $"{creditReq.MerchantCode}{creditReq.TransactionID}{creditReq.MemberCode}{creditReq.Amount}{creditReq.CurrencyCode}{time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss")}{creditReq.toBankAccountNumber}{inputs["key"]}";
            dicP.Add("Key", ToMD5(P).ToUpper());
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.TransactionID);

            WithdrawResult withdrawResult = new WithdrawResult();

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Payout));
            using var reader = new System.IO.StringReader(sResponse);
            var payout = (Payout)xmlSerializer.Deserialize(reader);
            if (payout.statusCode == "000")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = payout.message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = payout.message;
            }

            return withdrawResult;
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
