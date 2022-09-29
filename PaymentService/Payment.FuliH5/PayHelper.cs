using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.FuliH5
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawalReq
        {
            public string requestId { get; set; }
            public string result { get; set; }
            public string sum { get; set; }
            public string totalSum { get; set; }
            public string balance { get; set; }
            public string canUsedBalance { get; set; }
            public string frozenBalance { get; set; }
            public string unsettledBalance { get; set; }
            public string payType { get; set; }
            public string bankAccount { get; set; }
            public string bankName { get; set; }
            public string name { get; set; }
            public string requestTime { get; set; }
            public string dealTime { get; set; }
            public string noticeType { get; set; }
            public string failReason { get; set; }
            public string signature { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawalReq>(ToJson(formData));
            var p = $"{data.requestId}{data.result}{data.sum}{data.balance}";
            var sign = ToHMACSMD5(p, payResource["key"]);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.requestId;
            oNotify.ThirdId = data.requestId;
            oNotify.Message = data.result;
            if (sign == data.signature)
            {
                if (data.result == "00000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.sum);
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                    var failReason = "";
                    try
                    {
                        //failReason = System.Web.HttpUtility.UrlDecode(data.failReason, System.Text.Encoding.GetEncoding("GB2312"));
                        failReason = GBKToUTF8(data.failReason);
                    }
                    catch(Exception ex)
                    {
                        failReason = data.failReason;
                    }

                    oNotify.Message = failReason;
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
            public string requestId { get; set; }
            public string tradeId { get; set; }
            public string fiscalDate { get; set; }
            public string description { get; set; }
            public string resultSignature { get; set; }
            public string payType { get; set; }
            public string bankCode { get; set; }
            public string totalPrice { get; set; }
            public string tradeAmount { get; set; }
            public string tradeFee { get; set; }
            public string status { get; set; }
            public string endTime { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var p = $"{data.requestId}{data.tradeId}{data.fiscalDate}{data.description}{data.totalPrice}{data.tradeAmount}{data.tradeFee}";
            var sign = ToHMACSMD5(p, payResource["key"]);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.requestId;
            oNotify.ThirdId = data.tradeId;
            oNotify.Comment = data.description;
            oNotify.Message = data.status;

            if (sign == data.resultSignature)
            {
                if (data.status == "2")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.totalPrice);
                    oNotify.ActualAmount = decimal.Parse(data.totalPrice);
                    oNotify.Comment = data.status;
                    oNotify.Response = "success";
                }
                else 
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }


        private class WithdrawalRes
        {
            public string requestId { get; set; }
            public string result { get; set; }
            public string sum { get; set; }
            public string signature { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var formData = new Dictionary<string, string>
            {
                { "requestId", inputs["TransID"] },
                { "merchantCode", inputs["merchant_no"] },
                { "transferType", "1" }
            };
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("sum", amount.ToString("#"));
            formData.Add("accountType", "1");
            formData.Add("unionBankNum", "000000");
            formData.Add("branchBankName", "无");
            formData.Add("openBankName", "无");
            formData.Add("openBankProvince", "无");
            formData.Add("openBankCity", "无");
            formData.Add("accountName", inputs["AccountName"]);
            formData.Add("bankCode", inputs["BankCode"]);
            formData.Add("bankAccount", inputs["AccountNo"]);
            formData.Add("reason", "无");
            formData.Add("noticeUrl", inputs["CallBackUrl"]);

            var p = $"{formData["requestId"]}{formData["merchantCode"]}{formData["transferType"]}{formData["sum"]}{formData["accountType"]}{formData["unionBankNum"]}" +
                    $"{formData["branchBankName"]}{formData["openBankName"]}{formData["openBankProvince"]}{formData["openBankCity"]}{formData["accountName"]}{formData["bankCode"]}" +
                    $"{formData["bankAccount"]}{formData["reason"]}{formData["noticeUrl"]}";
            formData.Add("signature", ToHMACSMD5(p, inputs["key"]));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPostGBK(sUrl, sParam, "application/x-www-form-urlencoded", formData["requestId"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.result == "00000" || withdrawalRes.result == "00001")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.result;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.result;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string requestId { get; set; }
            public string result { get; set; }
            public string codeUrl { get; set; }
            public string codeImgString { get; set; }
            public string codeImgUrl { get; set; }
            public string payId { get; set; }
            public string passThrough { get; set; }
            public string resultSignature { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>
            {
                { "requestId", inputs["TransID"] },
                { "merchantCode", inputs["merchant_no"] },
                { "totalBizType", "BIZ01100" }
            };
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("totalPrice", amount.ToString("#"));
            formData.Add("backurl", inputs["ReturnUrl"]);
            formData.Add("returnurl", inputs["ReturnUrl"]);
            formData.Add("noticeurl", inputs["CallBackUrl"]);
            formData.Add("allowRePay", "0");
            formData.Add("productId", "无");
            formData.Add("productName", "无");;
            formData.Add("fund", formData["totalPrice"]);
            formData.Add("merAcct", inputs["TransID"]);
            formData.Add("bizType", "BIZ01100");
            formData.Add("productNumber", "1");
            formData.Add("payType", inputs["PayCode"]);
            var p = $"{formData["requestId"]}{formData["merchantCode"]}{formData["totalBizType"]}{formData["totalPrice"]}{formData["backurl"]}{formData["returnurl"]}{formData["noticeurl"]}";
            formData.Add("mersignature", ToHMACSMD5(p, inputs["key"]));

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
