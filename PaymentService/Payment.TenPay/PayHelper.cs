using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;
using System.Linq;
using Newtonsoft.Json;

namespace Payment.TenPay
{
    public class PayHelper : PayHelperBase
    {

        private class CallBackCreditReq
        {
            public string Amount { get; set; }
            public string CreatedAt { get; set; }
            public string CurrencyId { get; set; }
            public string DepositOrderStatusId { get; set; }
            public string EncryptValue { get; set; }
            public string IsTest { get; set; }
            public string PaidAt { get; set; }
            public string PaymentChannelId { get; set; }
            public string ShopCommissionAmount { get; set; }
            public string ShopOrderId { get; set; }
            public string ShopRemark { get; set; }
            public string ShopUserLongId { get; set; }
            public string TrackingNumber { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(ToJson(formData));

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.ShopOrderId;

            if (data.DepositOrderStatusId == "2")
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.Amount);
                oNotify.ThirdId = data.TrackingNumber;
                oNotify.Message = data.DepositOrderStatusId;
                oNotify.Response ="OK";
            }
            else if (data.DepositOrderStatusId == "3")
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.TrackingNumber;
                oNotify.Message = data.DepositOrderStatusId;
                oNotify.Response = "OK";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.TrackingNumber;
                oNotify.Message = data.DepositOrderStatusId;
                oNotify.Response = "OK";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string Amount { get; set; }
            public string AmountPaid { get; set; }
            public string CreatedAt { get; set; }
            public string CurrencyId { get; set; }
            public string EncryptValue { get; set; }
            public string FailedMessage { get; set; }
             public string IsTest { get; set; }
            public string OrderStatusId { get; set; }
            public string OriginalOrderTrackingNumber { get; set; }
            public string PaymentChannelId { get; set; }
            public string PaymentReceivedAt { get; set; }
            public string RealAmount { get; set; }
            public string ShopCommissionAmount { get; set; }
            public string ShopOrderId { get; set; }
            public string ShopRemark { get; set; }
            public string ShopUserLongId { get; set; }
            public string TrackingNumber { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&hashkey={payResource["key"]}";
            string sign = ToSHA256(p.ToLower()).ToUpper();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.ShopOrderId;
            if (sign == data.EncryptValue)
            {
                if (data.OrderStatusId == "2")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.ActualAmount = decimal.Parse(data.AmountPaid);
                    oNotify.ThirdId = data.TrackingNumber;
                    oNotify.Comment = data.TrackingNumber;
                    oNotify.Message = data.TrackingNumber;
                    oNotify.Response = "OK";
                }
                else if (data.OrderStatusId == "3")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.TrackingNumber;
                    oNotify.Comment = data.FailedMessage;
                    oNotify.Message = data.FailedMessage;
                    oNotify.Response = "OK";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.TrackingNumber;
                    oNotify.Comment = data.FailedMessage;
                    oNotify.Message = data.FailedMessage;
                    oNotify.Response = "OK";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "Fail";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawRes
        {
            public string Success { get; set; }
            public string TrackingNumber { get; set; }
            public string ErrorMessage { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("CurrencyId", inputs["PayCurrencyId"]);
            formData.Add("IsTest", inputs["IsTest"]);
            formData.Add("PayeeAccountName", inputs["AccountName"]);
            formData.Add("PayeeAccountNumber", inputs["AccountNo"]);
            formData.Add("PayeeIFSCCode", inputs["BankCode"]);
            formData.Add("PayeeAccountNumber", inputs["AccountNo"]);
            formData.Add("PayeeBankName", inputs["BankName"]);
            formData.Add("PaymentChannelId", inputs["PayCode"]);
            formData.Add("ShopInformUrl", inputs["ReturnUrl"]);
            formData.Add("ShopOrderId", inputs["TransID"]);
            formData.Add("ShopReturnUrl", inputs["CallBackUrl"]);
            formData.Add("ShopUserLongId", inputs["merchant_no"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&hashkey={inputs["key"]}";
            formData.Add("EncryptValue", ToSHA256(p.ToLower()).ToUpper());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["ShopOrderId"]);

            WithdrawRes withdrawRes = new WithdrawRes();
            withdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(sResponse);
            WithdrawResult oWithdrawResult = new WithdrawResult();

            if (withdrawRes.Success == "true")
            {
                oWithdrawResult.Success = true;
                oWithdrawResult.Message = withdrawRes.TrackingNumber;
            }
            else
            {
                oWithdrawResult.Success = false;
                oWithdrawResult.Message = withdrawRes.ErrorMessage;
            }

            return oWithdrawResult;
        }

        private class DepositRes
        {
            public string AccountName { get; set; }
            public string AccountNumber { get; set; }
            public string Amount { get; set; }
            public string BankName { get; set; }
            public string CreatedAt { get; set; }
            public string ExpiredAt { get; set; }
            public string FileName { get; set; }
            public string IFSCCode { get; set; }
            public string PayUrl { get; set; }
            public string QrCodeData { get; set; }
            public string RealAmount { get; set; }
            public string Success { get; set; }
            public string TrackingNumber { get; set; }
            public string ErrorMessage { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("CurrencyId", inputs["PayCurrencyId"]);
            formData.Add("IsTest", inputs["IsTest"]);
            formData.Add("PayerKey", inputs["CustId"]);
            formData.Add("PayerName", inputs["CustId"]);
            formData.Add("PaymentChannelId", inputs["PayCode"]);
            formData.Add("ShopInformUrl", inputs["ReturnUrl"]);
            formData.Add("ShopOrderId", inputs["TransID"]);
            formData.Add("ShopReturnUrl", inputs["CallBackUrl"]);
            formData.Add("ShopUserLongId", inputs["merchant_no"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&hashkey={inputs["key"]}";
            formData.Add("EncryptValue", ToSHA256(p.ToLower()).ToUpper());

            string sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["ShopOrderId"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            if (depositRes.Success == "true")
            {
                oDepositResult.Url = depositRes.PayUrl;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.ErrorMessage;
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
    }
}
