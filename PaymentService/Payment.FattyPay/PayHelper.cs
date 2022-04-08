using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;

namespace Payment.FattyPay
{
    class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;
        private readonly Dictionary<string, string> _providerMapping;
        private readonly Dictionary<string, List<string>> _noBankCodeMethodList;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _providerMapping = new Dictionary<string, string>
            {
                {"VND", "JSTPay"},
                {"MYR", "VaderPay"},
                {"THB", "OneWallet"}
            };
            _noBankCodeMethodList = new Dictionary<string, List<string>>
            {
                {"MYR", new List<string> { "ShopeePay", "MaybankEWallet" ,"Boost" ,"GrabPay", "TouchnGo", "DuitNow" }},
                {"THB", new List<string> { "TrueMoney", "ThaiQr" ,"PromptPay" }}
            };
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var sUrl = payResource["host"] + "/api/v1/transaction";
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.MerchantTransactionId;

            var p = $"{payResource["merchant_no"]}&{data.MerchantTransactionId}&{payResource["key"]}";
            var sign = ToSHA512(p).ToUpper();

            if (sign == data.Signature)
            {
                var formData1 = new Dictionary<string, string>
                {
                    { "MerchantTransactionId", data.MerchantTransactionId },
                    { "MerchantCode", payResource["merchant_no"] },
                    { "Timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() }
                };

                p = $"{formData1["MerchantCode"]}&{formData1["MerchantTransactionId"]}&{formData1["Timestamp"]}&{payResource["key"]}";
                formData1.Add("Signature", ToSHA512(p).ToUpper());

                var dicP = FromJson<Dictionary<string, string>>(ToJson(formData1));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var sResponse = HttpGet(sUrl, sParam, data.MerchantTransactionId, true);

                var callBackDebitReq2 = new CallBackDebitReq2();
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
            var sUrl = payResource["host"] + "/api/v1/transaction";
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = payResource["TransID"];

            var p = $"{payResource["merchant_no"]}&{data.MerchantTransactionId}&{payResource["key"]}";
            var sign = ToSHA512(p).ToUpper();

            if (sign == data.Signature)
            {
                var formData1 = new Dictionary<string, string>
                {
                    { "MerchantTransactionId", data.MerchantTransactionId },
                    { "MerchantCode", payResource["merchant_no"] },
                    { "Timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() }
                };

                p = $"{formData1["MerchantCode"]}&{formData1["MerchantTransactionId"]}&{formData1["Timestamp"]}&{payResource["key"]}";
                formData1.Add("Signature", ToSHA512(p).ToUpper());

                var dicP = FromJson<Dictionary<string, string>>(ToJson(formData1));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var sResponse = HttpGet(sUrl, sParam, data.MerchantTransactionId, true);

                var callBackDebitReq2 = new CallBackDebitReq2();
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
                else if (callBackDebitReq2.statusCode == 4 || callBackDebitReq2.statusCode == 5)
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
            var sUrl = inputs["host"] + "/api/v1/withdraw";
            var currency = inputs["PayCurrencyId"];
            var provider = GetProviderByCurrency(currency);

            var formData = new Dictionary<string, string>();
            var findPayCode = FindPayCode(inputs["PayCode"]);
            formData.Add("PaymentMethod", findPayCode[1]);
            formData.Add("BankCode", inputs["BankCode"]);
            formData.Add("ServiceProvider", provider);
            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("MerchantTransactionId", inputs["TransID"]);
            formData.Add("CustomerIsoCurrency", currency);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("Amount", amount.ToString("#0.0000"));
            formData.Add("FirstName", inputs["AccountName"]);
            formData.Add("CustomerAccount", inputs["AccountNo"]);
            formData.Add("NotifyUrl", inputs["CallBackUrl"]);
            formData.Add("CustomerId", inputs["CustId"]);
            var p = $"{inputs["key"]}&{formData["MerchantTransactionId"]}&{formData["Amount"]}&{formData["CustomerIsoCurrency"]}&{formData["CustomerAccount"]}";
            formData.Add("Signature", ToSHA512(p).ToUpper());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["MerchantTransactionId"], true);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
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
            var sUrl = inputs["host"] + "/api/v2/deposit/request";
            var currency = inputs["PayCurrencyId"];
            var provider = GetProviderByCurrency(currency);

            var formData = new Dictionary<string, string>();
            var findPayCode = FindPayCode(inputs["PayCode"]);
            var payMethod = findPayCode[0];
            
            if (payMethod == "BankQr")
            {
                formData.Add("PaymentMethod", payMethod); 
                formData.Add("BankCode", findPayCode[1]);
            }
            else
            {
                var bankCode = inputs["BankCode"];
                if (string.IsNullOrEmpty(bankCode))
                {
                    bankCode = GetNoBankCode(currency, payMethod);
                }
                
                formData.Add("PaymentMethod", payMethod);
                formData.Add("BankCode", bankCode);
            }

            formData.Add("ServiceProvider", provider);
            formData.Add("MerchantCode", inputs["merchant_no"]);
            formData.Add("MerchantTransactionId", inputs["TransID"]);
            formData.Add("IsoCurrency", currency);
            var amount = decimal.Parse(inputs["Amount"]);
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

            var p = $"{inputs["key"]}&{formData["MerchantTransactionId"]}&{formData["ServiceProvider"]}&{formData["PaymentMethod"]}&{formData["IsoCurrency"]}&" +
                    $"{formData["Amount"]}&{formData["Email"]}";
            
            //_logger.LogInformation($"Plain Text: {p}");
            var sign = ToSHA512(p).ToUpper();
            //_logger.LogInformation($"Signature: {sign}");
            formData.Add("Signature", sign);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"], true);

            var oDepositResult = new DepositResult();
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
            var sArray = payCode.Split('-');

            return sArray;
        }
        private string GetProviderByCurrency(string currency)
        {
            if (!_providerMapping.ContainsKey(currency))
            {
                throw new ArgumentException($"Currency: {currency} has not defined service provider.");
            }

            return _providerMapping[currency];
        }

        private string GetNoBankCode(string currency, string method)
        {
            if (!_noBankCodeMethodList.ContainsKey(currency))
                return string.Empty;

            return _noBankCodeMethodList[currency].Contains(method)
                ? "NoBankCode"
                : string.Empty;
        }
    }
}
