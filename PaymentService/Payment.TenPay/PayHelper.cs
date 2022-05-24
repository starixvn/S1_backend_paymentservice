using PaymentCore;
using System;
using System.Collections.Generic;
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
            var data = FromJson<CallBackCreditReq>(bodyContent);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.ShopOrderId;

            if (data.DepositOrderStatusId == "2")
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.Amount);
                oNotify.ThirdId = data.TrackingNumber;
                oNotify.Message = data.DepositOrderStatusId;
                oNotify.Response ="ok";
            }
            else if (data.DepositOrderStatusId == "3")
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.TrackingNumber;
                oNotify.Message = data.DepositOrderStatusId;
                oNotify.Response = "ok";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.TrackingNumber;
                oNotify.Message = data.DepositOrderStatusId;
                oNotify.Response = "ok";
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
            var data = FromJson<CallBackDebitReq>(bodyContent);
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).Where(f => f.Value.Length > 0).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            dicP.Remove("EncryptValue");
            var p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&hashkey={payResource["key"]}";
            var sign = ToSHA256(p.ToLower()).ToUpper();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.ShopOrderId;
            if (sign == data.EncryptValue)
            {
                if (data.OrderStatusId == "2")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.Amount);
                    oNotify.ActualAmount = decimal.Parse(data.Amount);
                    oNotify.ThirdId = data.TrackingNumber;
                    oNotify.Comment = data.TrackingNumber;
                    oNotify.Message = data.TrackingNumber;
                    oNotify.Response = "ok";
                }
                else if (data.OrderStatusId == "3")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.TrackingNumber;
                    oNotify.Comment = data.FailedMessage;
                    oNotify.Message = data.FailedMessage;
                    oNotify.Response = "ok";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.TrackingNumber;
                    oNotify.Comment = data.FailedMessage;
                    oNotify.Message = data.FailedMessage;
                    oNotify.Response = "ok";
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
            public bool Success { get; set; }
            public string TrackingNumber { get; set; }
            public string ErrorMessage { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var formData = new Dictionary<string, string>();
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("CurrencyId", inputs["PayCurrencyId"]);
            formData.Add("IsTest", inputs["IsTest"]);
            formData.Add("PayeeAccountName", inputs["AccountName"]);
            formData.Add("PayeeAccountNumber", inputs["AccountNo"]);
            formData.Add("PayeeIFSCCode", inputs["IFSCCode"]);
            formData.Add("PayeeBankName", inputs["BankName"]);
            formData.Add("PaymentChannelId", inputs["PayCode"]);
            formData.Add("ShopInformUrl", inputs["CallBackUrl"]);
            formData.Add("ShopOrderId", inputs["TransID"]);
            formData.Add("ShopUserLongId", inputs["merchant_no"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&hashkey={inputs["key"]}";
            formData.Add("EncryptValue", ToSHA256(p.ToLower()).ToUpper());

            formData["PayeeBankName"] = UrlEncode(inputs["BankName"]);
            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["ShopOrderId"], true);

            var withdrawRes = new WithdrawRes();
            withdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(sResponse);
            var oWithdrawResult = new WithdrawResult();

            if (withdrawRes.Success)
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
            // INR TenPay 存款電話號碼錯誤會無法成功存款所以做此驗證
            if (!inputs["PhoneVerified"].Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return new DepositResult
                {
                    Success = false,
                    Message = "Phone verification not complete."
                };
            }
            
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>();
            var amount = decimal.Parse(inputs["Amount"]);
            var payAccount = inputs["PayAccount"];
            var phoneNo = (inputs["AreaCode"].Replace("+", "") + inputs["PhoneNo"])
                .Replace(" ", "");
            
            formData.Add("Amount", amount.ToString("#"));
            formData.Add("CurrencyId", inputs["PayCurrencyId"]);
            formData.Add("IsTest", inputs["IsTest"]);
            formData.Add("PayerKey", inputs["CustId"]);
            formData.Add("PayerName", inputs["CustId"]);
            formData.Add("PaymentChannelId", inputs["PayCode"]);
            formData.Add("ShopReturnUrl", inputs["ReturnUrl"]);
            formData.Add("ShopOrderId", inputs["TransID"]);
            formData.Add("ShopInformUrl", inputs["CallBackUrl"]);
            formData.Add("ShopUserLongId", inputs["merchant_no"]);
            formData.Add("PayeePhoneNumber", phoneNo);

            if (!string.IsNullOrEmpty(payAccount))
            {
                formData.Add("PayerAccountNumber", payAccount);
            }

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&hashkey={inputs["key"]}";
            formData.Add("EncryptValue", ToSHA256(p.ToLower()).ToUpper());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["ShopOrderId"], true);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
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
