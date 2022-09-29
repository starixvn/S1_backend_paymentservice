using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DragonPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private class CallBackCreditReq : CallBackDebitReq
        {
            public string bankCode { get; set; }
            public string province { get; set; }
            public string city { get; set; }
            public string bankBranch { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(ToJson(formData));

            var p = $"{payResource["merchant_no"]}{data.transactionId}{data.amount}{data.timestamp}";
            var sign = UrlEncode(RSADecryptWithPrivateKey(RSAPrivateKeyJava2DotNet(payResource["PrivateKey"]), data.sign));

            var oNotify = new NotifyWithdrawResult();
            //oNotify.WithdrawId = data.transactionId.Remove(0, data.transactionId.IndexOf("-") + 1);
            oNotify.WithdrawId = data.transactionId.Substring(data.transactionId.IndexOf("-") + 1);
            oNotify.ThirdId = data.transactionStatus.ToString();
            oNotify.Message = data.transactionStatus.ToString();

            if (p == sign)
            {
                if (data.transactionStatus == 2)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.amount / 100;
                    oNotify.Response = "{\"status\":\"ok\"}";
                }
                else if (data.transactionStatus == 0)
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "{\"status\":\"ok\"}";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "{\"status\":\"ok\"}";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "{\"status\":\"fail\"}";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public long amount { get; set; }
            public string charge { get; set; }
            public int transactionStatus { get; set; }
            public string createdDateTime { get; set; }
            public string modificationDateTime { get; set; }
            public string transactionId { get; set; }
            public string platformTransactionId { get; set; }
            public string timestamp { get; set; }
            public string playerId { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            var p = $"{payResource["merchant_no"]}{data.transactionId}{data.amount}{data.timestamp}";
            var sign = UrlEncode(RSADecryptWithPrivateKey(RSAPrivateKeyJava2DotNet(payResource["PrivateKey"]), data.sign));

            //oNotify.DepositId = data.transactionId.Remove(0, data.transactionId.IndexOf("-") + 1);
            oNotify.DepositId = data.transactionId.Substring(data.transactionId.IndexOf("-") + 1);
            oNotify.ThirdId = data.platformTransactionId;
            oNotify.Message = data.transactionStatus.ToString();
            oNotify.Comment = data.transactionStatus.ToString();

            if (sign == p)
            {
                if (data.transactionStatus == 2)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.amount / 100;
                    oNotify.ActualAmount = data.amount / 100;
                    oNotify.Response = "{\"status\":\"ok\"}";
                }
                else if (data.transactionStatus == 0)
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "{\"status\":\"ok\"}";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "{\"status\":\"ok\"}";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "{\"status\":\"fail\"}";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawalRes
        {
            public long statusCode { get; set; }

            public string description { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"] + "/pms/api/payout/request";
            var formData = new Dictionary<string, string>
            {
                { "orderNo", inputs["TransID"] },
                { "merchantCode", inputs["merchant_no"] },
                { "beneficiaryAccount", inputs["AccountNo"] },
                { "beneficiaryName", inputs["AccountName"] },
                { "bankCode", inputs["BankCode"] }
            };

            var amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            formData.Add("currencyCode", inputs["PayCurrencyId"]);

            var p = $"{formData["merchantCode"]}{formData["orderNo"]}{formData["bankCode"]}{formData["beneficiaryAccount"]}{formData["amount"]}";
            formData.Add("sign", UrlEncode(RSAEncryptWithPrivateKey(RSAPrivateKeyJava2DotNet(inputs["PrivateKey"]), p)));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.statusCode == 200)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.description;
                withdrawResult.RawContent = withdrawalRes.description;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.description;
                withdrawResult.RawContent = withdrawalRes.description;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string statusCode { get; set; }
            public string description { get; set; }
            public DepositResData data { get; set; }
        }

        public class DepositResData
        {
            public string orderNo { get; set; }
            public string amount { get; set; }
            public string systemAmount { get; set; }
            public string discountAmount { get; set; }
            public string requestAmount { get; set; }
            public string accountNumber { get; set; }
            public string accountName { get; set; }
            public string remainingFinishTime { get; set; }
            public string frontEndTimeOutInMinutes { get; set; }
            public string remaningFrontEndTimeOutInSec { get; set; }
            public string bankName { get; set; }
            public string transactionStatus { get; set; }
            public string qrCodeImagePath { get; set; }
            public string referenceCode { get; set; }
            public string eWalletName { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = "";
            var p = "";
            var formData = new Dictionary<string, string>
            {
                { "merchantCode", inputs["merchant_no"] },
                { "orderNo", inputs["TransID"] },
                { "currencyCode", inputs["PayCurrencyId"] }
            };
            var amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));

            if (inputs["PayCode"] != null)
            {
                sUrl = inputs["host"] + "/pms/api/eWalletDeposit/request";
                formData.Add("eWalletCode", inputs["PayCode"]);
                p = $"{formData["merchantCode"]}{formData["orderNo"]}{formData["eWalletCode"]}{formData["amount"]}";

            }
            else
            {
                sUrl = inputs["host"] + "/pms/api/deposit/request";
                formData.Add("bankCode", inputs["BankCode"]);
                p = $"{formData["merchantCode"]}{formData["orderNo"]}{formData["bankCode"]}{formData["amount"]}";
            }

            formData.Add("sign", UrlEncode(RSAEncryptWithPrivateKey(RSAPrivateKeyJava2DotNet(inputs["PrivateKey"]), p)));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.statusCode == "200")
            {
                oDepositResult.ThirdId = depositRes.data.orderNo;
                oDepositResult.Url = depositRes.data.qrCodeImagePath;
                var ResData = new Dictionary<string, string>();
                var reqamount = decimal.Parse(depositRes.data.systemAmount) / 100;
                ResData.Add("DepositAmount", reqamount.ToString("#"));
                ResData.Add("BankName", depositRes.data.bankName);
                ResData.Add("AccountName", depositRes.data.accountName);
                ResData.Add("AccountNumber", depositRes.data.accountNumber);
                oDepositResult.FormData = ResData;
                oDepositResult.Success = true;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.description;
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

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            //return data.transactionId.Remove(0, data.transactionId.IndexOf("-") + 1);
            return data.transactionId.Substring(data.transactionId.IndexOf("-") + 1);
        }
    }
}
