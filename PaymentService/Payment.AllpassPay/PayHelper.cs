using Newtonsoft.Json;
using PaymentCore;

namespace Payment.AllpassPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        private class CallBackDebitReq
        {
            public string merchantNum { get; set; }
            public string orderNo { get; set; }
            public string platformOrderNo { get; set; }
            public string amount { get; set; }
            public string attch { get; set; }
            public string state { get; set; }
            public string payTime { get; set; }
            public string sign { get; set; }
            public string actualPayAmount { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            var p = $"{data.state}{data.merchantNum}{data.orderNo}{data.amount}{payResource["key"]}";
            var sign = ToMD5(p).ToLower();

            oNotify.DepositId = data.orderNo;
            oNotify.ThirdId = data.platformOrderNo;
            oNotify.Message = data.state.ToString();
            oNotify.Comment = data.state.ToString();

            if (sign == data.sign)
            {
                if (data.state == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
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
            public string success { get; set; }
            public string msg { get; set; }
            public string code { get; set; }
            public string total { get; set; }
            public string timestamp { get; set; }
            public string data { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"] + "/withdraw";
            var formData = new Dictionary<string, string>
            {
                { "merchantNum", inputs["merchant_no"] },
                { "orderNo", inputs["TransID"] },
                { "bankCardAccount", inputs["AccountNo"] },
                { "accountHolder", inputs["AccountName"] },
                { "openAccountBank", inputs["BankCode"] }
            };

            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("withdrawAmount", amount.ToString("#"));

            var p = $"{formData["merchantNum"]}{formData["orderNo"]}{formData["openAccountBank"]}{formData["bankCardAccount"]}{formData["accountHolder"]}{formData["withdrawAmount"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);
            withdrawResult.Message = withdrawalRes.msg;
            withdrawResult.RawContent = withdrawalRes.msg;

            if (withdrawalRes.code == "200")
            {
                withdrawResult.Success = true;
            }
            else
            {
                withdrawResult.Success = false;
            }

            

            return withdrawResult;
        }

        private class DepositRes
        {
            public string success { get; set; }
            public string msg { get; set; }
            public string code { get; set; }
            public string total { get; set; }
            public string timestamp { get; set; }
            public DepositResData data { get; set; }
        }

        public class DepositResData
        {
            public string id { get; set; }
            public string payUrl { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"] + "/startOrder";

            var formData = new Dictionary<string, string>
            {
                { "merchantNum", inputs["merchant_no"] },
                { "orderNo", inputs["TransID"] }
            };
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("payType", inputs["PayCode"]);
            formData.Add("notifyUrl", inputs["CallBackUrl"]);
            formData.Add("returnUrl", inputs["ReturnUrl"]);

            var p = $"{formData["merchantNum"]}{formData["orderNo"]}{formData["amount"]}{formData["notifyUrl"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.code == "200")
            {
                oDepositResult.Url = depositRes.data.payUrl;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.msg;
            }

            return oDepositResult;
        }

        private class EnquiryWithdrawResultRes
        {
            public string success { get; set; }
            public string msg { get; set; }
            public string code { get; set; }
            public string total { get; set; }
            public string timestamp { get; set; }
            public EnquiryWithdrawResultResData data { get; set; }
        }

        private class EnquiryWithdrawResultResData
        {
            public string id { get; set; }
            public string orderNo { get; set; }
            public string withdrawAmount { get; set; }
            public string withdrawFee { get; set; }
            public string applyTime { get; set; }
            public string state { get; set; }
            public string note { get; set; }
            public string approvalTime { get; set; }
            public string confirmCreditedTime { get; set; }
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"] + "/withdrawState";

            var formData = new Dictionary<string, string>
            {
                { "merchantNum", inputs["merchant_no"] },
                { "orderNo", inputs["TransID"] }
            };

            var p = $"{formData["merchantNum"]}{formData["orderNo"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderNo"]);

            var enquiryWithdrawResultRes = new EnquiryWithdrawResultRes();
            enquiryWithdrawResultRes = JsonConvert.DeserializeObject<EnquiryWithdrawResultRes>(sResponse);
            var oNotify = new EnquiryWithdrawResult();

            if (enquiryWithdrawResultRes.code == "200")
            {
                oNotify.WithdrawId = enquiryWithdrawResultRes.data.orderNo;
                oNotify.ThirdId = enquiryWithdrawResultRes.data.id;
                oNotify.Message = enquiryWithdrawResultRes.data.state;

                if (enquiryWithdrawResultRes.data.state == "4")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(enquiryWithdrawResultRes.data.withdrawAmount);
                }
                else if (enquiryWithdrawResultRes.data.state == "3")
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
            }

            return oNotify;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
