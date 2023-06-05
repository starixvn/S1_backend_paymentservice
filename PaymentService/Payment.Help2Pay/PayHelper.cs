using PaymentCore;
using System.Globalization;
using System.Xml.Serialization;
using PaymentCore.Models.Result;

namespace Payment.Help2Pay
{
    public class PayHelper : PayHelperBase
    {
        public class CallBackCreditReq
        {
            public string MerchantCode { get; set; }
            public string TransactionID { get; set; }
            public string CurrencyCode { get; set; }
            public string Amount { get; set; }
            public string TransactionDatetime { get; set; }
            public string Key { get; set; }
            public string Status { get; set; }
            public string MemberCode { get; set; }
            public string ID { get; set; }
            public string PayoutFee { get; set; }
            public string Message { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(ToJson(formData));
            var merchant_no = payResource["merchant_no"];
            var key = payResource["key"];
            var amount = 0m;
            decimal.TryParse(data.Amount, out amount);
            var p = $"{merchant_no}{data.TransactionID}{data.MemberCode}{amount.ToString("#.00")}{data.CurrencyCode}{data.Status}{key}";
            var sign = ToMD5(p).ToUpper();

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.TransactionID;
            if (sign == data.Key)
            {
                if (data.Status == "000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.ThirdId = data.ID;
                    oNotify.Message = data.Message;
                    oNotify.Response = "OK";
                }
                else if (data.Status == "001")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.ID;
                    oNotify.Message = data.Message;
                    oNotify.Response = "Fail";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.ID;
                    oNotify.Message = data.Status;
                    oNotify.Response = "Pending";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
            }

            return oNotify;
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
            var merchant_no = payResource["merchant_no"];
            var key = payResource["key"];
            var p = $"{merchant_no}{data.Reference}{data.Customer}{data.Amount}{data.Currency}{data.Status}{key}";
            var sign = ToMD5(p).ToUpper();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.Reference;
            if (sign == data.Key)
            {
                if (data.Status == "000" || data.Status == "006")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.ActualAmount = decimal.Parse(data.Amount);
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
            var sUrl = inputs["PayOut"];
            var creditReq = new CreditReq();
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
            var P = $"{creditReq.MerchantCode}{creditReq.TransactionID}{creditReq.MemberCode}{creditReq.Amount}{creditReq.CurrencyCode}{time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss")}{creditReq.toBankAccountNumber}{inputs["key"]}";
            dicP.Add("Key", ToMD5(P).ToUpper());
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.TransactionID);

            var withdrawResult = new WithdrawResult();

            var xmlSerializer = new XmlSerializer(typeof(Payout));
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
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>
            {
                { "Merchant", inputs["merchant_no"] },
                { "Currency", inputs["PayCurrencyId"] },
                { "Reference", inputs["TransID"] },
                { "Amount", decimal.Parse(inputs["Amount"]).ToString("#.00") }
            };
            var time = new DateTimeOffset(DateTime.Now);
            var bankCode = inputs["BankCode"];
            var payCode = inputs["PayCode"];

            formData.Add("Datetime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd hh:mm:sstt", CultureInfo.InvariantCulture));
            formData.Add("FrontURI", inputs["ReturnUrl"]);
            formData.Add("BackURI", inputs["CallBackUrl"]);
            formData.Add("Bank", string.IsNullOrEmpty(bankCode) ? payCode : bankCode);
            formData.Add("Language", inputs["PayLangId"]);
            formData.Add("ClientIP", inputs["ClientIP"]);
            formData.Add("Customer", "");
            
            var p = $"{formData["Merchant"]}{formData["Reference"]}{""}{formData["Amount"]}{formData["Currency"]}{time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss")}{inputs["key"]}{formData["ClientIP"]}";
            formData.Add("Key", ToMD5(p).ToUpper());

            var oDepositResult = new DepositResult();
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
