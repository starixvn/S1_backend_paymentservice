using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Payment.WealthPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            var data = FromJson<CallBackDebitReq>(bodyContent);

            string p = $"{data.MerchantCode}{data.CurrencyCode}{data.BankCode}{data.TransactionID}{data.Amount}" +
                        $"{data.TransactionTime}{data.ID}{data.Status}{data.Message}{payResource["key"]}";
            string sign = ToSHA256(p).ToLower();

            oNotify.WithdrawId = data.TransactionID;
            oNotify.ThirdId = data.ID;
            oNotify.Message = data.Message;

            if (sign == data.Signature)
            {
                if (data.Status == "0001")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.Amount;
                    oNotify.Response = "200";
                }
                else if (data.Status == "0003")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "200";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "200";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "fail";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string MerchantCode { get; set; }
            public string CurrencyCode { get; set; }
            public string BankCode { get; set; }
            public string TransactionID { get; set; }
            public decimal Amount { get; set; }
            public string SenderAccountNumber { get; set; }
            public string TransactionTime { get; set; }
            public string ID { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
            public string Signature { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            NotifyDepositResult oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(bodyContent);

            string p = $"{data.MerchantCode}{data.CurrencyCode}{data.BankCode}{data.TransactionID}{data.Amount}" +
                        $"{data.TransactionTime}{data.ID}{data.Status}{data.Message}{payResource["key"]}";
            string sign = ToSHA256(p).ToLower();

            oNotify.DepositId = data.TransactionID;
            oNotify.ThirdId = data.ID;
            oNotify.Message = data.Message;

            if (sign == data.Signature)
            {
                if (data.Status == "0001")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.Amount;
                    oNotify.ActualAmount = data.Amount;
                    oNotify.Message = data.Message;
                    oNotify.Comment = data.Message;
                    oNotify.Response = "200";
                }
                else if (data.Status == "0003" || data.Status == "0004")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Message = data.Message;
                    oNotify.Comment = data.Message;
                    oNotify.Response = "200";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Message = data.Message;
                    oNotify.Comment = data.Message;
                    oNotify.Response = "200";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "fail";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawalRes
        {
            public string status { get; set; }
            public string submissionID { get; set; }
            public string message { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("TransactionID", inputs["TransID"]);
            formData.Add("MemberID", inputs["MemberID"]);
            formData.Add("CurrencyCode", inputs["PayCurrencyId"]);
            formData.Add("BankCode", inputs["BankCode"]);
            formData.Add("ToAccountNumber", inputs["AccountNo"]);
            formData.Add("ToAccountName", inputs["AccountName"]);
            formData.Add("ToProvince", "");
            formData.Add("ToCity", "");
            formData.Add("ToBranch", "");
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("Note", "");
            formData.Add("CallbackURL", inputs["CallBackUrl"]);
            var time = new DateTimeOffset(DateTime.Now);
            formData.Add("TransactionTime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            string p = $"{formData["MerchantCode"]}{formData["TransactionID"]}{formData["MemberID"]}{formData["CurrencyCode"]}{formData["BankCode"]}" +
                                    $"{formData["ToAccountNumber"]}{formData["ToAccountName"]}{formData["ToProvince"]}{formData["ToCity"]}{formData["ToBranch"]}" +
                                    $"{formData["Amount"]}{formData["Note"]}{formData["CallbackURL"]}{formData["ClientIP"]}{formData["TransactionTime"]}{inputs["key"]}"; 
            formData.Add("Signature", ToSHA256(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["TransactionID"]);

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.status == "0000")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.message;
                withdrawResult.RawContent = withdrawalRes.message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.message;
                withdrawResult.RawContent = withdrawalRes.message;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public string RedirectURL { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            //if (inputs["PayCode"] != null)
            //{
            //    formData.Add("DepositChannel", inputs["PayCode"]);
            //}
            //else
            //{
            //    formData.Add("DepositChannel", "1");
            //}

            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("DepositType", "0");
            formData.Add("DepositChannel", inputs["PayCode"]);
            formData.Add("SenderBankCode", "");
            formData.Add("SendeAccountNumber", "");
            formData.Add("SendeAccountName", "");
            formData.Add("TransactionID", inputs["TransID"]);
            formData.Add("MemberID", inputs["CustId"]);
            formData.Add("CurrencyCode", inputs["PayCurrencyId"]);
            formData.Add("BankCode", inputs["BankCode"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("Note", "");
            formData.Add("RedirectURL", inputs["ReturnUrl"]);
            formData.Add("CallbackURL", inputs["CallBackUrl"]);
            formData.Add("ClientIP", inputs["ClientIP"]);
            var time = new DateTimeOffset(DateTime.Now);
            formData.Add("TransactionTime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            string p = $"{formData["MerchantCode"]}{formData["TransactionID"]}{formData["MemberID"]}{formData["CurrencyCode"]}{formData["BankCode"]}" +
                        $"{formData["Amount"]}{formData["Note"]}{formData["RedirectURL"]}{formData["CallbackURL"]}{formData["ClientIP"]}" +
                        $"{formData["TransactionTime"]}{inputs["key"]}";
            formData.Add("Signature", ToSHA256(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["TransactionID"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.Status == "0000")
            {
                oDepositResult.Url = depositRes.RedirectURL;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.Message;
            }

            return oDepositResult;
        }

        private class EnquiryWithdrawRes
        {
            public string code { get; set; }
            public string message { get; set; }
            public EnquiryWithdrawResData data { get; set; }
        }

        public class EnquiryWithdrawResData
        {
            public string orderSn { get; set; }
            public string upOrderSn { get; set; }
            public string amount { get; set; }
            public string status { get; set; }

        }

        Dictionary<string, string> payCurrencyIdMapping = new Dictionary<string, string>()
        {
            { "4","5"}
        };

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOutQuery"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            formData.Add("merchantId", inputs["merchant_no"]);
            formData.Add("orderSn", inputs["TransID"]);
            formData.Add("currency", payCurrencyIdMapping[inputs["CurrencyId"]]);
            decimal amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            formData.Add("type", "bank");

            string p = $"{formData["merchantId"]}{formData["orderSn"]}{formData["currency"]}{formData["amount"]}{formData["type"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderSn"]);

            EnquiryWithdrawRes enquiryWithdrawRes = new EnquiryWithdrawRes();
            enquiryWithdrawRes = JsonConvert.DeserializeObject<EnquiryWithdrawRes>(sResponse);
            EnquiryWithdrawResult oNotify = new EnquiryWithdrawResult();
            if (enquiryWithdrawRes.code == "200")
            {
                if (enquiryWithdrawRes.data.status == "2" || enquiryWithdrawRes.data.status == "3")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(enquiryWithdrawRes.data.amount) / 100;
                    oNotify.ThirdId = enquiryWithdrawRes.data.upOrderSn;
                }
                else if (enquiryWithdrawRes.data.status == "4")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = enquiryWithdrawRes.data.upOrderSn;
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                }
            }
            else if (enquiryWithdrawRes.code == "201")
            {
                oNotify.TransStatus = TransStatus.Fail;
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
            }

            oNotify.Message = enquiryWithdrawRes.message;
            return oNotify;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
