using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PaymentCore;

namespace Payment.NAP3SPay
{
    public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        public class DepositResponse
        {
            public int status { get; set; }
            public string message { get; set; }
            public int tran_id { get; set; }
            public int amount { get; set; }
            public int real_amount { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var amount = decimal.Parse(inputs["Amount"]).ToString("#");
            var formData = new Dictionary<string, string>
            {
                { "api_key", inputs["key"] },
                { "card_seri", inputs["Serial"] },
                { "card_code", inputs["Code"] },
                { "request_id", inputs["TransID"] },
                { "card_amount", amount },
                { "card_type", inputs["PayCode"] },
                { "signature", ToMD5($"{inputs["key"]}{amount}{inputs["Code"]}{inputs["Serial"]}").ToLower() }
            };
            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}"));
            var sResponse = HttpPost(inputs["host"], sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());
            var result = JsonConvert.DeserializeObject<DepositResponse>(sResponse);
            return new DepositResult
            {
                Success = result.status == 0,
                ErrorCode = result.status.ToString(),
                Message = result.message,
                FormData = formData,
                DepositAmount = result.amount,
                SuggestAmount = result.real_amount,
                RawContent = sResponse,
                ThirdId = result.tran_id.ToString(),
            };
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var transStatus = int.Parse(formData["status"]) switch
            {
                0 => TransStatus.Success,
                100 => TransStatus.Success,
                101 => TransStatus.Pending,
                _ => TransStatus.Fail,
            };
            return new NotifyDepositResult
            {
                TransStatus = transStatus,
                Message = formData["message"],
                Comment = formData["message"],
                RequestAmount = decimal.Parse(formData["amount"]),
                ActualAmount = decimal.Parse(formData["real_amount"]),
                DepositId = formData["request_id"],
                RefThirdId = formData["tran_id"],
                ThirdId = formData["tran_id"],
                Response = "success"
            };
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
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
            return formData["request_id"];
        }
    }
}
