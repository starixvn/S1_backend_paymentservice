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

            if (inputs["Lang"] == "en")
            {
                result.message = result.status switch
                {
                    0 => "Request sent successfully. Please wait for a response - wait for that handling that callback",
                    -1 => "Missing parameters - unspecified errors",
                    -2 => "Wrong format card, for example: vtt card code 13 or 15 digital numbers",
                    -3 => "Wrong signature",
                    -5 => "Card is not submitted, return your card - (the system is busy)",
                    -6 => "User procedes not operating cards (inactived)",
                    -10 => "Maintenance api system",
                    -98 => "Card has paided before, insert card",
                    _ => result.message
                };
            }

            return new DepositResult
            {
                Success = result.status == 0,
                ErrorCode = result.status.ToString(),
                Message = result.message,
                FormData = new Dictionary<string, string>()
                {
                    { "Message", result.message } ,
                },
                DepositAmount = result.amount,
                SuggestAmount = result.real_amount,
                RawContent = sResponse,
                ThirdId = result.tran_id.ToString(),
            };
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var amount = decimal.Parse(formData["amount"]);
            var realAmount = decimal.Parse(formData["real_amount"]);
            var minAmount = realAmount < amount ? realAmount : amount;
            return new NotifyDepositResult
            {
                TransStatus = int.Parse(formData["status"]) switch
                {
                    0 => TransStatus.Success,
                    100 => TransStatus.Success,
                    101 => TransStatus.Pending,
                    _ => TransStatus.Fail,
                },
                Message = formData["message"],
                Comment = formData["message"],
                RequestAmount = amount,
                ActualAmount = minAmount * decimal.Parse(payResource["Rate"]),
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
            var formData = new Dictionary<string, string>
            {
                { "api_key", inputs["key"] },
                { "request_id", inputs["TransID"] },
                { "signature", ToMD5($"{inputs["key"]}{inputs["TransID"]}").ToLower() }
            };
            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}"));
            var sResponse = HttpPost(inputs["DebitQuery"], sParam, "application/x-www-form-urlencoded", inputs["TransID"]);
            var result = JsonConvert.DeserializeObject<DepositResponse>(sResponse);

            return new EnquiryDepositResult
            {
                TransStatus = result.status switch
                {
                    0 => TransStatus.Success,
                    -4 => TransStatus.Fail,
                    _ => TransStatus.Pending
                },
                Message = result.message,
                Comment = result.message,
                RawContent = result.message,
                RequestAmount = Convert.ToDecimal(result.amount),
                ActualAmount = Convert.ToDecimal(result.real_amount) * decimal.Parse(inputs["Rate"]),
                DepositId = inputs["TransID"],
                ThirdId = result.tran_id.ToString(),
                RefThirdId = result.tran_id.ToString()
            };
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            return formData["request_id"];
        }
    }
}
