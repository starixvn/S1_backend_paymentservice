using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.FattyPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            string sUrl = payResource["host"] + "/api/v1/transaction";
            var data = FromJson<CallBackDebitReq>(bodyContent);

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.MerchantTransactionId;

            string p = $"{payResource["merchant_no"]}&{data.MerchantTransactionId}&{payResource["key"]}";
            string sign = ToSHA512(p).ToUpper();

            if (sign == data.Signature)
            {
                Dictionary<string, string> formData1 = new Dictionary<string, string>();
                formData1.Add("MerchantTransactionId", data.MerchantTransactionId);
                formData1.Add("MerchantCode", payResource["merchant_no"]);
                formData1.Add("Timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());

                p = $"{formData1["MerchantCode"]}&{formData1["MerchantTransactionId"]}&{formData1["Timestamp"]}&{payResource["key"]}";
                formData1.Add("Signature", ToSHA512(p).ToUpper());

                var dicP = FromJson<Dictionary<string, string>>(ToJson(formData1));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var sResponse = HttpGet(sUrl, sParam, data.MerchantTransactionId);

                CallBackDebitReq2 callBackDebitReq2 = new CallBackDebitReq2();
                callBackDebitReq2 = JsonConvert.DeserializeObject<CallBackDebitReq2>(sResponse);
                oNotify.Message = callBackDebitReq2.message;

                if (callBackDebitReq2.statusCode == 3)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(callBackDebitReq2.actualAmount);
                    oNotify.ThirdId = callBackDebitReq2.merchantTransactionId;
                }
                else if (callBackDebitReq2.statusCode == 4 || callBackDebitReq2.statusCode == 6)
                {
                    oNotify.TransStatus = TransStatus.Fail;
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string MerchantTransactionId { set; get; }
            public string Signature { set; get; }
        }
        public class Detail
        {
            public double transactionAmount { get; set; }
            public double rate { get; set; }
            public double baseFee { get; set; }
            public double minFee { get; set; }
            public int calculatedType { get; set; }
            public int decimalPlaces { get; set; }
        }

        public class Fee
        {
            public string currency { get; set; }
            public double amount { get; set; }
            public int type { get; set; }
            public Detail detail { get; set; }
        }

        public class CallBackDebitReq2
        {
            public string merchantTransactionId { get; set; }
            public string actualAmount { get; set; }
            public int statusCode { get; set; }
            public string message { get; set; }
            public List<Fee> fees { get; set; }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            string sUrl = payResource["host"] + "/api/v1/transaction";
            var data = FromJson<CallBackDebitReq>(bodyContent);

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = payResource["TransID"];

            string p = $"{payResource["merchant_no"]}&{data.MerchantTransactionId}&{payResource["key"]}";
            string sign = ToSHA512(p).ToUpper();

            if (sign == data.Signature)
            {
                Dictionary<string, string> formData1 = new Dictionary<string, string>();
                formData1.Add("MerchantTransactionId", data.MerchantTransactionId);
                formData1.Add("MerchantCode", payResource["merchant_no"]);
                formData1.Add("Timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());

                p = $"{formData1["MerchantCode"]}&{formData1["MerchantTransactionId"]}&{formData1["Timestamp"]}&{payResource["key"]}";
                formData1.Add("Signature", ToSHA512(p).ToUpper());

                var dicP = FromJson<Dictionary<string, string>>(ToJson(formData1));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var sResponse = HttpGet(sUrl, sParam, data.MerchantTransactionId);

                CallBackDebitReq2 callBackDebitReq2 = new CallBackDebitReq2();
                callBackDebitReq2 = JsonConvert.DeserializeObject<CallBackDebitReq2>(sResponse);
                oNotify.Comment = callBackDebitReq2.message;
                oNotify.Message = callBackDebitReq2.message;

                if (callBackDebitReq2.statusCode == 3)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(callBackDebitReq2.actualAmount);
                    oNotify.ActualAmount = decimal.Parse(callBackDebitReq2.actualAmount);
                    oNotify.ThirdId = callBackDebitReq2.merchantTransactionId;
                }
                else if (callBackDebitReq2.statusCode == 4 || callBackDebitReq2.statusCode == 6)
                {
                    oNotify.TransStatus = TransStatus.Fail;
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "sign error";
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public class WithdrawalRes
        {
            public int errorCode { get; set; }
            public string errorMessage { get; set; }
        }
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + "/api/v1/withdraw";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            string[] findPayCode = FindPayCode(inputs["PayCode"]);
            formData.Add("PaymentMethod", findPayCode[1]);
            formData.Add("BankCode", inputs["BankCode"]);
            formData.Add("ServiceProvider", "JSTPay");
            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("MerchantTransactionId", inputs["TransID"]);
            formData.Add("CustomerIsoCurrency", inputs["PayCurrencyId"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#0.0000"));
            formData.Add("FirstName", inputs["AccountName"]);
            formData.Add("CustomerAccount", inputs["AccountNo"]);
            formData.Add("NotifyUrl", inputs["CallBackUrl"]);
            formData.Add("CustomerId", inputs["CustId"]);
            string p = $"{inputs["key"]}&{formData["MerchantTransactionId"]}&{formData["Amount"]}&{formData["CustomerIsoCurrency"]}&{formData["CustomerAccount"]}";
            formData.Add("Signature", ToSHA512(p).ToUpper());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["MerchantTransactionId"]);

            WithdrawResult withdrawResult = new WithdrawResult();
            WithdrawalRes withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.errorCode == 0)
            {
                withdrawResult.Success = true;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.errorMessage;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public int status { get; set; }
            public string msg { get; set; }
        }
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + "/api/v2/deposit/request";

            Dictionary<string, string> formData = new Dictionary<string, string>();
            string[] findPayCode = FindPayCode(inputs["PayCode"]);
            if (findPayCode[0] == "BankQr")
            {
                formData.Add("PaymentMethod", findPayCode[0]); 
                formData.Add("BankCode", findPayCode[1]);
            }
            else 
            {
                formData.Add("PaymentMethod", findPayCode[0]);
                formData.Add("BankCode", inputs["BankCode"]);
            }

            formData.Add("ServiceProvider", "JSTPay");
            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("MerchantTransactionId", inputs["TransID"]);
            formData.Add("IsoCurrency", inputs["PayCurrencyId"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#0.0000"));
            formData.Add("FirstName", "FirstName");
            formData.Add("LastName", "LastName");
            formData.Add("FullName", "FullName");
            formData.Add("Email", "test@test.com");
            formData.Add("CustomerId", inputs["CustId"]);
            formData.Add("Language", inputs["PayLangId"]);
            formData.Add("SuccessUrl", inputs["ReturnUrl"]);
            formData.Add("FailUrl", inputs["ReturnUrl"]);
            formData.Add("NotifyUrl", inputs["CallBackUrl"]);
            formData.Add("HomeUrl", inputs["ReturnUrl"]);
            formData.Add("RequestIp", inputs["ClientIP"]);

            string p = $"{inputs["key"]}&{formData["MerchantTransactionId"]}&{formData["ServiceProvider"]}&{formData["PaymentMethod"]}&{formData["IsoCurrency"]}&" +
                        $"{formData["Amount"]}&{formData["Email"]}";
            formData.Add("Signature", ToSHA512(p).ToUpper());

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"]);

            DepositResult oDepositResult = new DepositResult();
            if (sResponse.Contains("http"))
            {
                oDepositResult.Success = true;
                oDepositResult.Message = "";
                oDepositResult.Url = sResponse;
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = sResponse;
            }

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

        private string[] FindPayCode(string payCode)
        {
            string[] sArray = payCode.Split('-');

            return sArray;
        }
    }
}
