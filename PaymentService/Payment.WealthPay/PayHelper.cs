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
            throw new NotImplementedException();
        }
        private class CallBackDebitReq
        {
            public int merchantId { get; set; }
            public string orderSn { get; set; }
            public string queryId { get; set; }
            public string goodsName { get; set; }
            public decimal amount { get; set; }
            public int status { get; set; }
            public decimal realMoney { get; set; }
            public string message { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            NotifyDepositResult oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(bodyContent);

            string p = $"{data.merchantId}{data.orderSn}{data.amount}{payResource["key"]}";
            string sign = ToMD5(p).ToLower();

            oNotify.DepositId = data.orderSn;
            oNotify.ThirdId = data.queryId;
            oNotify.Message = data.message;

            if (sign == data.sign)
            {
                if (data.status == 2 || data.status == 3)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.amount / 100;
                    oNotify.ActualAmount = data.amount / 100;
                    oNotify.Message = data.message;
                    oNotify.Comment = $"{data.status}-{data.message}";
                    oNotify.Response = "success";
                }
                else if (data.status == 1)
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Message = data.message;
                    oNotify.Comment = $"{data.status}-{data.message}";
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Message = data.message;
                    oNotify.Comment = $"{data.status}-{data.message}";
                    oNotify.Response = "success";
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
            public long code { get; set; }

            public string message { get; set; }

            public object[] data { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("merchantId", inputs["merchant_no"]);
            formData.Add("orderSn", inputs["TransID"]);
            formData.Add("currency", inputs["PayCurrencyId"]);
            decimal amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            formData.Add("type", "bank");
            formData.Add("userName", inputs["AccountName"]);
            formData.Add("cardNo", inputs["AccountNo"]);
            formData.Add("bankCode", inputs["BankCode"]);
            formData.Add("bankName", inputs["BankCode"]);
            formData.Add("bankBranch", inputs["BankCode"]);
            formData.Add("branchAddress", inputs["BankCode"]);
            formData.Add("province", inputs["BankCode"]);
            formData.Add("city", inputs["BankCode"]);
            formData.Add("unionCode", inputs["BankCode"]);
            formData.Add("bankMobile", inputs["BankCode"]);

            string p = $"{formData["merchantId"]}{formData["orderSn"]}{formData["currency"]}{formData["amount"]}{formData["type"]}{formData["userName"]}{formData["cardNo"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderSn"]);

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.code == 200)
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
            formData.Add("CurrencyCode", inputs["PayCurrencyId"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("RedirectURL", inputs["ReturnUrl"]);
            formData.Add("CallbackURL", inputs["CallBackUrl"]);
            formData.Add("Note", "");
            formData.Add("MemberID", inputs["CustId"]);
            formData.Add("ClientIP", inputs["ClientIP"]);
            var time = new DateTimeOffset(DateTime.Now);
            formData.Add("TransactionTime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            string p = $"{formData["MerchantCode"]}{formData["TransactionID"]}{formData["MemberID"]}{formData["CurrencyCode"]}{formData["BankCode"]}{formData["Amount"]}{formData["Note"]}{formData["RedirectURL"]}{formData["CallbackURL"]}{formData["ClientIP"]}{formData["TransactionTime"]}{inputs["key"]}";
            formData.Add("Signature", ToSHA256(p).ToLower());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["TransactionID"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.Status == "0000")
            {
                oDepositResult.Url = depositRes.Status;
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
