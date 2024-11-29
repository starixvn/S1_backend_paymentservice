using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.VNPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }
        public class PayoutCallbackRequest
        {
            public string merchant_no { get; set; }
            public string order_no { get; set; }
            public string amount { get; set; }
            public string ylt_order_no { get; set; }
            public int status { get; set; }
            public string sign { get; set; }

        }

        public enum CallBackStatus
        {
            success = 1,
            fail = 2,
            exception =3,
            insufficientBalance = 4,
            reject = 5
        }
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var key = payResource["OBKey"];
            var req = FromJson<PayoutCallbackRequest>(bodyContent);
            var sign = $"{req.merchant_no}|{req.order_no}|{req.ylt_order_no}|{req.amount}|{key}";

            if (req.sign != ToMD5(sign).ToLower())
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Pending,
                    WithdrawId = req.order_no,
                    Message = "Signature verification failed.",
                    Response = "fail"
                };
            }

            var status = req.status;
            if (status != (int)CallBackStatus.success)
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Fail,
                    WithdrawId = req.order_no,
                    Message = ((CallBackStatus)status).ToString(),
                    Response = "fail"
                };
            }
            else
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = req.ylt_order_no,
                    TransStatus = TransStatus.Success,
                    Message = ((CallBackStatus)status).ToString(),
                    Response = "success",
                    WithdrawId = req.order_no
                };
            }
        }

        public class CallBackDebitReq
        {
            public string result_code { set; get; }
            public string merchant_no { set; get; }
            public string order_no { set; get; }
            public string ylt_order_no { set; get; }
            public string amount { set; get; }
            public string channel { set; get; }
            public string extra_param { set; get; }
            public string sign { set; get; }
            public string user_amount { set; get; }
        }
        public class WithdrawResponse
        {
            public string message { get; set; }
            public string status { get; set; }
            public string info { get; set; }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var merchant_no = payResource["merchant_no"];
            var key = payResource["key"];
            var amount = decimal.Parse(data.amount);

            var p = $"{data.merchant_no}|{data.order_no}|{data.ylt_order_no}|{data.amount}|{data.channel}|{key}";
            var sign = ToMD5(p).ToLower();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.order_no;
            if (sign == data.sign)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ActualAmount = decimal.Parse(data.amount);
                oNotify.ThirdId = data.ylt_order_no;
                oNotify.Comment = data.result_code;
                oNotify.Message = data.extra_param;
                oNotify.Response = "success";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Comment = data.result_code;
                oNotify.Message = data.extra_param;
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["Withdrawhost"];
            var amount = decimal.Parse(inputs["Amount"]);
            var req = new Dictionary<string, string>
            {
                { "merchant_no", inputs["merchant_no"] },
                { "order_no", inputs["TransID"] },
                { "amount", amount.ToString() },
                { "channel", "bank" },
                { "notify_url", inputs["CallBackUrl"] },
                { "bank_code", inputs["BankCode"] },
                { "bank_number", inputs["AccountNo"] },
            };

            var sign = $"{req["merchant_no"]}|{req["order_no"]}|{req["amount"]}|{req["channel"]}|{inputs["key"]}";
            req.Add("sign", ToMD5(sign).ToLower());

            var (response, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, WithdrawResponse>($"{sUrl}/api/v2/payOut", req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {(int)statusCode}", response.message);
            }

            if (response.message != "success")
                throw new CustomException(
                    $"VNPay CreatOrder response error: status={response.status}, info={response.info}, msg={response.message}");

            return new WithdrawResult
            {
                Success = true,
                WithdrawId = inputs["TransID"],
                Message = response.message,
                RawContent = JsonConvert.SerializeObject(response)
            };
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>
            {
                { "merchant_no", inputs["merchant_no"] },
                { "order_no", inputs["TransID"] }
            };
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("channel", inputs["PayCode"]);
            formData.Add("bank_code", inputs["BankCode"]);
            formData.Add("notify_url", inputs["CallBackUrl"]);
            formData.Add("result_url", inputs["ReturnUrl"]);
            formData.Add("c_ip", "");
            formData.Add("extra_param", "");

            var p = $"{formData["merchant_no"]}|{formData["order_no"]}|{formData["amount"]}|{formData["channel"]}|{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

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

        private class CreditReq
        {
            public string merchant_no { get; set; }
            public string order_no { get; set; }
            public string amount { get; set; }
            public string notify_url { get; set; }
            public string channel { get; set; }
            public string sign { get; set; }
        }

        private class CreditResult
        {
            public string message { get; set; }
            public string status { get; set; }
            public string info { get; set; }
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["CheckURL"];
            var creditReq = new CreditReq();
            creditReq.merchant_no = inputs["merchant_no"];
            creditReq.order_no = inputs["TransID"];
            creditReq.amount = inputs["Amount"]; ;
            creditReq.channel = inputs["PayCode"];

            var p = $"{creditReq.merchant_no}|{creditReq.order_no}|{creditReq.amount}|{creditReq.channel}|{inputs["key"]}";
            creditReq.sign = ToMD5(p).ToLower();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}"));
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.order_no);

            var creditResult = new CreditResult();
            creditResult = JsonConvert.DeserializeObject<CreditResult>(sResponse);

            var enquiryDepositResult = new EnquiryDepositResult();
            if (creditResult.status == "1")
            {
                enquiryDepositResult.TransStatus = TransStatus.Success;
                enquiryDepositResult.Message = creditResult.message;
                enquiryDepositResult.RawContent = creditResult.info;
            }
            else
            {
                enquiryDepositResult.TransStatus = TransStatus.Fail;
                enquiryDepositResult.Message = creditResult.message;
                enquiryDepositResult.RawContent = creditResult.info;
            }

            return enquiryDepositResult;
        }
    }
}
