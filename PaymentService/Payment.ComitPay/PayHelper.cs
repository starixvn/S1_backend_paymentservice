using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Payment.ComitPay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawReq
        {
            public string MerchantCode { set; get; }
            public string TransactionID { set; get; }
            public string CurrencyCode { set; get; }
            public string Amount { set; get; }
            public string TransactionDateTime { set; get; }
            public string Membercode { set; get; }
            public string ID { set; get; }
            public string Status { set; get; }
            public string Message { set; get; }
            public string Key { set; get; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawReq>(bodyContent);

            var p = $"{data.MerchantCode}{data.TransactionID}{data.Membercode}{data.Amount}{data.CurrencyCode}{data.Status}{payResource["key"]}";
            var sign = ToMD5(p).ToLower();

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.TransactionID;
            oNotify.ThirdId = data.ID;
            oNotify.Message = data.Message;

            if (sign == data.Key)
            {
                if (data.Status == "000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.Response = "OK";
                }
                else if (data.Status == "001")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "OK";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "OK";
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

        private class CallBackDebitReq
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
            public string StatementDate { get; set; }
            public string Deposit { get; set; }
            public string ActualAmount { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var p = $"{data.Merchant}{data.Reference}{data.Customer}{data.Amount}{data.Currency}{data.Status}{payResource["key"]}";
            var sign = ToMD5(p).ToUpper();

            oNotify.DepositId = data.Reference;
            oNotify.ThirdId = data.ID;
            oNotify.Message = data.Status;
            oNotify.Comment = data.Status;

            if (sign == data.Key)
            {
                if (data.Status == "000" || data.Status == "006")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.ActualAmount = decimal.Parse(data.Amount);

                    oNotify.Response = "OK";
                }
                else if (data.Status == "001" || data.Status == "007" || data.Status == "008")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "OK";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "OK";
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

        private class WithdrawalRes
        {
            public string statusCode { get; set; }
            public string message { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"] + "/" + inputs["merchant_no"];

            var formData = new Dictionary<string, string>();
            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("CurrencyCode", inputs["PayCurrencyId"]);
            formData.Add("TransactionID", inputs["TransID"]);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("Membercode", inputs["CustId"]);
            var time = new DateTimeOffset(DateTime.Now);
            formData.Add("TransactionDateTime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            formData.Add("ClientIP", inputs["ClientIP"]);
            formData.Add("ReturnURI", inputs["CallBackUrl"]);
            formData.Add("BankCode", inputs["BankCode"]);
            formData.Add("toBankAccountName", inputs["AccountName"]);
            formData.Add("toBankAccountNumber", inputs["AccountNo"]);

            var p = $"{formData["MerchantCode"]}{formData["TransactionID"]}{formData["Membercode"]}{formData["Amount"]}{formData["CurrencyCode"]}{formData["TransactionDateTime"]}" +
                        $"{formData["ToBankAccountNumber"]}{inputs["key"]}";
            formData.Add("Key", ToMD5(p).ToUpper());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["TransactionID"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.statusCode == "000")
            {
                withdrawResult.Success = true;
            }
            else
            {
                withdrawResult.Success = false;
            }

            withdrawResult.Message = withdrawalRes.message;

            return withdrawResult;
        }


        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>();

            formData.Add("Merchant", inputs["merchant_no"]);
            formData.Add("Currency", inputs["PayCurrencyId"]);
            formData.Add("Reference", inputs["TransID"]);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            var time = new DateTimeOffset(DateTime.Now);
            formData.Add("Datetime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            formData.Add("Bank", inputs["BankCode"]);
            formData.Add("Language", inputs["PayLangId"]);
            formData.Add("FrontURI", inputs["ReturnUrl"]);
            formData.Add("BackURI", inputs["CallBackUrl"]);
            formData.Add("ClientIP", inputs["ClientIP"]);
            formData.Add("Customer", inputs["CustId"]);

            var p = $"{formData["Merchant"]}{formData["Reference"]}{formData["Customer"]}{formData["Amount"]}{formData["Currency"]}{formData["Datetime"]}{inputs["key"]}{formData["ClientIP"]}";
            formData.Add("Key", ToMD5(p).ToUpper());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["Reference"]);
            var oDepositResult = new DepositResult();

            oDepositResult.Success = true;
            oDepositResult.Message = sResponse;

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
