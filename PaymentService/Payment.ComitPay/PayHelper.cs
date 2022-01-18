using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.ComitPay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawReq
        {
            public string id { set; get; }
            public int status { set; get; }
            public string amount { set; get; }
            public string fee { set; get; }
            public string message { set; get; }
            public string token { set; get; }
            public string ts { set; get; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            var p = $"{payResource["Merchant"]}-{payResource["key"]}-{data.id}-{data.amount}-{data.ts}";
            var sign = ToMD5(p).ToLower();

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.id;

            //if (sign == data.token)
            //{
            if (data.status == 1)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ThirdId = data.id;
                oNotify.Message = data.status + "-" + data.message;
                oNotify.Response = "OK";
            }
            else if (data.status == -1)
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.id;
                oNotify.Message = data.status + "-" + data.message;
                oNotify.Response = "OK";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.id;
                oNotify.Message = data.status + "-" + data.message;
                oNotify.Response = "OK";
            }
            //}
            //else
            //{
            //    oNotify.TransStatus = TransStatus.Pending;
            //    oNotify.ThirdId = data.id;
            //    oNotify.Message = "Signature error";
            //    oNotify.Response = "Signature error";
            //}

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
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

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
            public int status { get; set; }
            public string msg { get; set; }
            public WithdrawalResData d { get; set; }
        }

        public class WithdrawalResData
        {
            public string id { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"] + "/a/sPayout";

            var formData = new Dictionary<string, string>();

            formData.Add("mCode", inputs["Merchant"]);
            formData.Add("ts", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
            formData.Add("bCode", inputs["BankCode"]);
            formData.Add("trxid", inputs["TransID"]);
            formData.Add("user", inputs["CustId"]);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amt", amount.ToString("#"));
            formData.Add("bAccName", inputs["AccountName"]);
            formData.Add("bAccNo", inputs["AccountNo"]);
            formData.Add("notifyUrl", inputs["CallBackUrl"]);

            var p = $"{formData["mCode"]}-{inputs["key"]}-sPayout-{formData["ts"]}";
            formData.Add("token", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["trxid"]);

            sResponse = sResponse.Replace("[", "{");
            sResponse = sResponse.Replace("]", "}");

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.status == 0)
            {
                withdrawResult.Success = true;
            }
            else
            {
                withdrawResult.Success = false;
            }

            withdrawResult.Message = withdrawalRes.msg;

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
            formData.Add("Datetime", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
            formData.Add("Bank", inputs["BankCode"]);
            formData.Add("Language", inputs["PayLangId"]);
            formData.Add("FrontURI", inputs["ReturnUrl"]);
            formData.Add("BackURI", inputs["CallBackUrl"]);
            formData.Add("ClientIP", inputs["ClientIP"]);
            formData.Add("Customer", inputs["CustId"]);

            var p = $"{formData["Merchant"]}{inputs["Reference"]}{formData["Customer"]}{formData["Amount"]}{formData["Currency"]}{formData["Datetime"]}{inputs["key"]}{formData["ClientIP"]}";
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
