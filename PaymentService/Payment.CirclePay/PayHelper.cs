using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.CirclePay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private class CallBackWithdrawReq
        {
            public string tran_id { set; get; }
            public string partner_tran_id { set; get; }
            public string total_amount { set; get; }
            public string token { set; get; }
            public string time { set; get; }
            public string status { set; get; }
            public string note { set; get; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawReq>(bodyContent);

            //string p = $"{data.total_amount}{data.bank_code}{data.card_fullname}{data.card_number}{data.partner_tran_id}";
            //string sign = ToHMACSHA256(p, payResource["key"]);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.partner_tran_id;

            //if (sign == data.token)
            //{
                if (data.status == "00")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.total_amount);
                    oNotify.ThirdId = data.tran_id;
                    oNotify.Message = data.status + "-" + data.note;
                    oNotify.Response = "200";
                }
                else if (data.status == "01" || data.status == "04")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.tran_id;
                    oNotify.Message = data.status + "-" + data.note;
                    oNotify.Response = "200";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.tran_id;
                    oNotify.Message = data.status + "-" + data.note;
                    oNotify.Response = "200";
                }
            //}
            //else
            //{
            //    oNotify.TransStatus = TransStatus.Pending;
            //    oNotify.ThirdId = data.tran_id;
            //    oNotify.Message = data.status;
            //    oNotify.Response = "Token error";
            //}

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string partner_tran_id { set; get; }
            public string tran_id { set; get; }
            public string amount { set; get; }
            public string status { set; get; }
            public string time { set; get; }
            public string token { set; get; }
            public string method { set; get; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var p = $"{payResource["email"]}{data.partner_tran_id}{data.amount}";
            var sign = ToHMACSHA256(p, payResource["key"]);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.partner_tran_id;
            if (sign == data.token)
            {
                if (data.status == "00")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.ThirdId = data.tran_id;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "200";
                }
                else if (data.status == "02" || data.status == "03")
                {
                    // Circlepay remove fail status due to they will update to accpet if fail
                    oNotify.TransStatus = TransStatus.Fail;
                    //oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.tran_id;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "200";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.tran_id;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "200";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.tran_id;
                oNotify.Comment = data.status;
                oNotify.Message = data.status;
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class CreditReq
        {
            public string total_amount { get; set; }
            public string bank_code { get; set; }
            public string card_fullname { get; set; }
            public string card_number { get; set; }
            public string reason { get; set; }
            public string partner_tran_id { get; set; }
            public string token { get; set; }
        }

        private class Payout
        {
            public string error_code { get; set; }
            public string error_message { get; set; }
            public string tran_id { get; set; }
            public string partner_tran_id { get; set; }
            public string total_amount { get; set; }
            public string status { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var creditReq = new CreditReq();
            creditReq.partner_tran_id = inputs["TransID"];
            var amount = decimal.Parse(inputs["Amount"]);
            creditReq.total_amount = amount.ToString("#");
            creditReq.card_fullname = inputs["AccountName"];
            creditReq.card_number = inputs["AccountNo"];
            creditReq.bank_code = inputs["BankCode"];
            creditReq.reason = "payout";

            var p = $"{creditReq.total_amount}{creditReq.bank_code}{creditReq.card_fullname}{creditReq.card_number}{creditReq.partner_tran_id}";
            creditReq.token = ToHMACSHA256(p, inputs["key"]);

            var username = inputs["email"];
            var password = ToHMACSHA256(username, inputs["key"]);

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", username, password, creditReq.partner_tran_id);

            var payout = new Payout();
            payout = JsonConvert.DeserializeObject<Payout>(sResponse);

            var withdrawResult = new WithdrawResult();
            if (payout.error_code == "00")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = payout.error_message;
                withdrawResult.RawContent = payout.error_message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = payout.error_message;
                withdrawResult.RawContent = payout.error_message;
            }

            return withdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var user_code = inputs["user_code"];

            var formData = new Dictionary<string, string> { { "partner_tran_id", inputs["TransID"].ToLower() } };

            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));

            var p = $"{inputs["email"]}{formData["partner_tran_id"]}{formData["amount"]}";
            formData.Add("token", ToHMACSHA256(p, inputs["key"]));
            sUrl += "/" + user_code + "/?partner_tran_id=" + inputs["TransID"].ToLower() + "&amount=" + amount.ToString("#") + "&token=" + ToHMACSHA256(p, inputs["key"]);

            var oDepositResult = new DepositResult();
            oDepositResult.Success = true;
            oDepositResult.Url = sUrl;

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
            var data = FromJson<CallBackDebitReq>(bodyContent);
            return data.partner_tran_id;
        }
    }
}
