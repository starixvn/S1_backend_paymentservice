using PaymentCore;
using System;
using System.Collections.Generic;

namespace Payment.NganLuong
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        private class CallBackDebitReq
        {
            public string transaction_id { set; get; }
            public string sender_email { set; get; }
            public string content { set; get; }
            public string time_performed { set; get; }
            public string time_created { set; get; }
            public string total_amount { set; get; }
            public string fee { set; get; }
            public string amount_receiver { set; get; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.content;
            oNotify.TransStatus = TransStatus.Success;
            oNotify.RequestAmount = decimal.Parse(data.total_amount);
            oNotify.ActualAmount = decimal.Parse(data.total_amount);
            oNotify.ThirdId = data.transaction_id;
            oNotify.Comment = data.time_performed;
            oNotify.Message = data.time_performed;
            oNotify.Response = "success";

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"] + $"?language={inputs["PayLangId"]}&email={inputs["email"]}&amount={decimal.Parse(inputs["Amount"]).ToString("#")}&content={inputs["TransID"].ToLower()}";

            DepositResult oDepositResult = new DepositResult();
            oDepositResult.Url = sUrl;
            oDepositResult.Success = true;
            oDepositResult.FormData = null;
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

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);
            return data.content;
        }
    }
}
