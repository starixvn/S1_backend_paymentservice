using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;
using System.Linq;
using Newtonsoft.Json;

namespace Payment.OneClickPay
{
    public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public class CallBackDebitReq
        {
            public string merchant_txn { get; set; }
            public string merchant_customer { get; set; }
            public string amount { get; set; }
            public string net_amount { get; set; }
            public string tnx { get; set; }
            public string description { get; set; }
            public string sign { get; set; }
            public string bank_code { get; set; }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            string secret_key = payResource["secret_key"];

            string p = $"{data.merchant_txn}{data.merchant_customer}{data.amount}{data.net_amount}{data.tnx}{secret_key}";
            string sign = ToMD5(p).ToLower();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.merchant_txn;
            if (sign == data.sign)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ActualAmount = decimal.Parse(data.net_amount);
                oNotify.ThirdId = data.tnx;
                oNotify.Comment = data.description;
                oNotify.Message = data.description;
                oNotify.Response = "VERIFIED";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Comment = data.description;
                oNotify.Message = data.description;
                oNotify.Response = "Failed";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class CreditReq
        {
            public string merchant_id { get; set; }
            public string merchant_txn { get; set; }
            public string merchant_customer { get; set; }
            public string bank_account_no { get; set; }
            public string bank_account_name { get; set; }
            public string bank_code { get; set; }
            public string amount { get; set; }
            public string sign { get; set; }
        }

        private class Data
        {
            public int payout_id { get; set; }
            public string merchant_txn { get; set; }
        }

        private class Payout
        {
            public int code { get; set; }
            public string message { get; set; }
            public int count { get; set; }
            public Data data { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PayOut"];
            CreditReq creditReq = new CreditReq();
            creditReq.merchant_id = inputs["merchant_no"];
            creditReq.merchant_txn = inputs["TransID"];
            creditReq.merchant_customer = inputs["CustId"];
            creditReq.bank_account_no = inputs["AccountNo"];
            creditReq.bank_account_name = inputs["AccountName"];
            creditReq.bank_code = inputs["BankCode"];
            decimal amount = decimal.Parse(inputs["Amount"]);
            creditReq.amount = amount.ToString("#");

            string p = $"{creditReq.merchant_id}{creditReq.merchant_txn}{creditReq.bank_account_no}{creditReq.bank_code}{creditReq.amount}{inputs["secret_key"]}";
            creditReq.sign = ToMD5(p).ToLower();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpGet(sUrl, sParam, creditReq.merchant_txn);

            Payout payout = new Payout();
            payout = JsonConvert.DeserializeObject<Payout>(sResponse);

            WithdrawResult withdrawResult = new WithdrawResult();
            if (payout.code == 0)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = payout.message;
                withdrawResult.RawContent = payout.message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = payout.message;
                withdrawResult.RawContent = payout.message;
            }

            return withdrawResult;
        }

        private class DepositReq
        {
            public string merchant_id { set; get; }
            public string merchant_txn { set; get; }
            public string merchant_customer { set; get; }
            public string amount { set; get; }
            public string bank_code { set; get; }
            public string description { set; get; }
            public string url_success { set; get; }
            public string url_error { set; get; }
            public string sign { set; get; }
        }

        public class DepositResData
        {
            public string redirect_url { get; set; }
        }

        public class DepositRes
        {
            public int code { get; set; }
            public string msg { get; set; }
            public int count { get; set; }
            public DepositResData data { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            DepositReq depositReq = new DepositReq();
            depositReq.merchant_id = inputs["merchant_no"];
            depositReq.merchant_txn = inputs["TransID"];
            depositReq.merchant_customer = inputs["CustId"];
            decimal amount = decimal.Parse(inputs["Amount"]);
            depositReq.amount = amount.ToString("#");
            depositReq.bank_code = inputs["BankCode"];
            depositReq.description = "";
            depositReq.url_success = inputs["ReturnUrl"];
            depositReq.url_error = inputs["ReturnUrl"];

            string p = $"{depositReq.merchant_id}{depositReq.merchant_txn}{depositReq.merchant_customer}{depositReq.amount}{depositReq.bank_code}{inputs["secret_key"]}";
            depositReq.sign = ToMD5(p).ToLower();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", depositReq.merchant_txn);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);

            DepositResult oDepositResult = new DepositResult();
            if (depositRes.code == 0)
            {
                oDepositResult.Success = true;
                oDepositResult.Url = depositRes.data.redirect_url;
                oDepositResult.Message = depositRes.msg;
                oDepositResult.RawContent = depositRes.msg;
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.msg;
                oDepositResult.RawContent = depositRes.msg;
            }

            return oDepositResult;
        }

        private class payourQueryRed
        {
            string merchant_id { get; set; }
            string merchant_txn { get; set; }
            string sign { get; set; }
        }

        private class PayourQueryResPayout
        {
            public string bank_account { get; set; }
            public string bank_name { get; set; }
            public int bank_id { get; set; }
            public string bank_account_name { get; set; }
            public string bank_code { get; set; }
        }

        private class PayourQueryResData
        {
            public int id { get; set; }
            public string amount { get; set; }
            public string net_amount { get; set; }
            public string fee { get; set; }
            public string note { get; set; }
            public string merchant_txn { get; set; }
            public string merchant_user { get; set; }
            public string stat { get; set; }
            public PayourQueryResPayout data_payout { get; set; }
            public string created_at { get; set; }
        }

        private class PayoutQueryRes
        {
            public int code { get; set; }
            public string message { get; set; }
            public int count { get; set; }
            public PayourQueryResData data { get; set; }
        }
        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            EnquiryWithdrawResult enquiryWithdrawResult = new EnquiryWithdrawResult();
            string sUrl = inputs["PayOutQuery"];
            CreditReq creditReq = new CreditReq();
            creditReq.merchant_id = inputs["merchant_no"];
            creditReq.merchant_txn = inputs["TransID"];

            string p = $"{creditReq.merchant_id}{creditReq.merchant_txn}{inputs["secret_key"]}";
            creditReq.sign = ToMD5(p).ToLower();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.merchant_txn);
            enquiryWithdrawResult.RawContent = sResponse;
            PayoutQueryRes payoutQueryRes = new PayoutQueryRes();
            payoutQueryRes = JsonConvert.DeserializeObject<PayoutQueryRes>(sResponse);

            if (payoutQueryRes.code == 0)
            {
                enquiryWithdrawResult.TransStatus = TransStatus.Success;
                enquiryWithdrawResult.RequestAmount = decimal.Parse(payoutQueryRes.data.amount);
                enquiryWithdrawResult.WithdrawId = payoutQueryRes.data.merchant_txn;
                enquiryWithdrawResult.Message = payoutQueryRes.message;
            }
            else if (payoutQueryRes.code == 18)
            {
                enquiryWithdrawResult.TransStatus = TransStatus.Pending;
                enquiryWithdrawResult.Message = payoutQueryRes.message;
            }
            else if (payoutQueryRes.code == 99)
            {
                if (payoutQueryRes.data.stat == "Processing" || payoutQueryRes.data.stat == "Transfering")
                {
                    enquiryWithdrawResult.TransStatus = TransStatus.Pending;
                    enquiryWithdrawResult.RequestAmount = decimal.Parse(payoutQueryRes.data.amount);
                    enquiryWithdrawResult.WithdrawId = payoutQueryRes.data.merchant_txn;
                    enquiryWithdrawResult.Message = payoutQueryRes.message;
                }
                else if (payoutQueryRes.data.stat == "Refund")
                {
                    enquiryWithdrawResult.TransStatus = TransStatus.Fail;
                    enquiryWithdrawResult.Message = payoutQueryRes.message;
                }
            }
            else if (payoutQueryRes.code == 17)
            {
                enquiryWithdrawResult.TransStatus = TransStatus.Fail;
                enquiryWithdrawResult.Message = payoutQueryRes.message;
            }
            else
            {
                enquiryWithdrawResult.TransStatus = TransStatus.Pending;
                enquiryWithdrawResult.Message = payoutQueryRes.message;
            }

            return enquiryWithdrawResult;
        }

        private class EnquiryDepositReq
        {
            public string merchant_id { get; set; }
            public string mer_txn { get; set; }
            public string sign { get; set; }
        }

        public class DepositQueryResData
        {
            public string transaction_id { get; set; }
            public string mer_txn { get; set; }
            public string mer_customer { get; set; }
            public int amount { get; set; }
            public string fee { get; set; }
            public string bank_code { get; set; }
            public string description { get; set; }
            public string log { get; set; }
            public string stat { get; set; }
            public string created_at { get; set; }
        }

        public class DepositQueryRes
        {
            public int code { get; set; }
            public string msg { get; set; }
            public int count { get; set; }
            public DepositQueryResData data { get; set; }
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["PaymentQuery"];
            EnquiryDepositReq enquiryDepositReq = new EnquiryDepositReq();
            enquiryDepositReq.merchant_id = inputs["merchant_no"];
            enquiryDepositReq.mer_txn = inputs["TransID"];

            string p = $"{enquiryDepositReq.merchant_id}{enquiryDepositReq.mer_txn}{inputs["secret_key"]}";
            enquiryDepositReq.sign = ToMD5(p).ToLower();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(enquiryDepositReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", enquiryDepositReq.mer_txn);

            DepositQueryRes depositQueryRes = new DepositQueryRes();
            depositQueryRes = JsonConvert.DeserializeObject<DepositQueryRes>(sResponse);

            EnquiryDepositResult enquiryDepositResult = new EnquiryDepositResult();
            if (depositQueryRes.code == 0)
            {
                enquiryDepositResult.TransStatus = TransStatus.Success;
                enquiryDepositResult.ActualAmount = decimal.Parse(depositQueryRes.data.amount.ToString());
                enquiryDepositResult.RequestAmount = decimal.Parse(depositQueryRes.data.amount.ToString());
                enquiryDepositResult.DepositId = depositQueryRes.data.mer_txn;
                enquiryDepositResult.ThirdId = depositQueryRes.data.transaction_id;
                enquiryDepositResult.Message = depositQueryRes.msg;
                enquiryDepositResult.RawContent = depositQueryRes.msg;
            }
            else
            {
                enquiryDepositResult.TransStatus = TransStatus.Fail;
                enquiryDepositResult.Message = depositQueryRes.msg;
                enquiryDepositResult.RawContent = depositQueryRes.msg;
            }

            return enquiryDepositResult;
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            return formData["merchant_txn"];
        }
    }
}
