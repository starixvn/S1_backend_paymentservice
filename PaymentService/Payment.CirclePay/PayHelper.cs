using PaymentCore;
using System;
using System.Collections.Generic;

namespace Payment.CirclePay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
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
            //URL: http://payment.n88dev.com:59159/Payment/DepositCallBack/CirclePay 
            // Data: {"partner_tran_id":"dd1276","tran_id":"27041859","amount":"5000000"
            //,"method":"IB_ONLINE","status":"00","time":"2020-03-20 22:29:38",
            //"token":"815c1817d1f1be60e7e6d181739bcb787f11a3e1b7b4c9fd45e52c3a9162905a"} 

            var data = FromJson<CallBackDebitReq>(ToJson(formData));

            string p = $"{payResource["email"]}{data.partner_tran_id}{data.amount}";
            string sign = ToHMACSHA256(p, payResource["key"]);

            NotifyDepositResult oNotify = new NotifyDepositResult();
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
                    oNotify.Response = "200”";
                }
                else if (data.status == "02" || data.status == "03")
                {
                    oNotify.TransStatus = TransStatus.Fail;
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
            public string reson { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            CreditReq creditReq = new CreditReq();
            //creditReq.service_version = "2.1";
            //creditReq.partner_code = inputs["merchant_no"];
            //creditReq.partner_orderid = inputs["TransID"];
            //creditReq.member_id = inputs["CustId"];
            //creditReq.currency = inputs["PayCurrencyId"];
            //decimal amount = decimal.Parse(inputs["Amount"]) * 100;
            //creditReq.amount = amount.ToString("#");
            //creditReq.account_name = inputs["AccountName"];
            //creditReq.account_number = inputs["AccountNo"];

            //creditReq.bank_province = bankDic[creditReq.currency];
            //creditReq.bank_city = bankDic[creditReq.currency];
            //creditReq.bank_branch = bankDic[creditReq.currency];
            //creditReq.bank_code = inputs["BankCode"];
            //creditReq.notify_url = inputs["CallBackUrl"];

            //string p = $"service_version={creditReq.service_version}&partner_code={creditReq.partner_code}&partner_orderid={creditReq.partner_orderid}&" +
            //    $"member_id={creditReq.member_id}&currency={creditReq.currency}&amount={creditReq.amount}&account_name={creditReq.account_name}&" +
            //    $"account_number={creditReq.account_number}&bank_province={creditReq.bank_province}&bank_city={creditReq.bank_city}&bank_branch={creditReq.bank_branch}&" +
            //    $"bank_code={creditReq.bank_code}&notify_url={creditReq.notify_url}&key={inputs["key"]}";
            //creditReq.sign = ToSHA1(p).ToUpper();

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            //string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            //string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.partner_orderid);

            //WithdrawResult withdrawResult = new WithdrawResult();
            //XmlSerializer xmlSerializer = new XmlSerializer(typeof(Payout));
            //using var reader = new System.IO.StringReader(sResponse);
            //var payout = (Payout)xmlSerializer.Deserialize(reader);
            //if (payout.status == "000" || payout.status == "001")
            //{
            //    withdrawResult.Success = true;
            //    withdrawResult.Message = payout.error_description;
            //}
            //else
            //{
            //    withdrawResult.Success = false;
            //    withdrawResult.Message = payout.error_description;
            //}

            //return withdrawResult;
            throw new NotImplementedException();
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            string user_code = inputs["user_code"];
            sUrl += "/" + user_code + "/?";

            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("partner_tran_id", inputs["TransID"].ToLower());
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));

            string p = $"{inputs["email"]}{formData["partner_tran_id"]}{formData["amount"]}";

            //fUjMiWpONaLekIz7HESXrnbq
            formData.Add("token", ToHMACSHA256(p, inputs["key"]));

            sUrl += "partner_tran_id=" + inputs["TransID"].ToLower();
            sUrl += "&amount=" + amount.ToString("#");
            sUrl += "&token=" + ToHMACSHA256(p, inputs["key"]);

            DepositResult oDepositResult = new DepositResult();
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
    }
}
