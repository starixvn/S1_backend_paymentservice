using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.MegaPay
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
        public class DebitResp
        {
            public bool success { get; set; }
            public DebitRespData data { get; set; }
            public string message { get; set; }
        }
        public class DebitRespData
        {
            public string trade_no { get; set; }
            public string out_trade_no { get; set; }
            public string amount { get; set; }
            public string discount { get; set; }
            public decimal discount_amount { get; set; }
            public string uri { get; set; }
            public string qrcode { get; set; }
        }
        public class CallBackDebitReq
        {
            public string trade_no { get; set; }
            public string amount { get; set; }
            public string request_amount { get; set; }
            public string out_trade_no { get; set; }
            public string callback_url { get; set; }
            public string state { get; set; }
            public string sign { get; set; }
        }
        public class CreditResp
        {
            public bool success { get; set; }
            public CreditRespData data { get; set; }
            public string message { get; set; }
        }
        public class CreditRespData
        {
            public string account_number { get; set; }
            public string trade_no { get; set; }
            public string out_trade_no { get; set; }
            public string amount { get; set; }
            public string bank_id { get; set; }
            public string bank_owner { get; set; }
            public string callback_url { get; set; }
            public string state { get; set; }
        }
        public class CallBackCreditReq
        {
            public string trade_no { get; set; }
            public string amount { get; set; }
            public string out_trade_no { get; set; }
            public string callback_url { get; set; }
            public string state { get; set; }
            public string sign { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackCredit, bodyContent:{bodyContent}");

            var data = FromJson<CallBackCreditReq>(bodyContent);

            var oNotify = new NotifyWithdrawResult()
            {
                WithdrawId = data.out_trade_no
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("amount", data.amount);
            toMD5Data.Add("callback_url", data.callback_url);
            toMD5Data.Add("out_trade_no", data.out_trade_no);
            toMD5Data.Add("state", data.state);
            toMD5Data.Add("trade_no", data.trade_no);
            var queryString = string.Join("&", toMD5Data.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var p = queryString + payResource["apiToken"] + payResource["callBackToken"];
            var sign = ToMD5(p).ToLower();

            if (sign == data.sign)
            {
                if (data.state == "completed")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ThirdId = data.trade_no;
                    oNotify.Message = data.state;
                    oNotify.Response = "ok";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.trade_no;
                    oNotify.Message = data.state;
                    oNotify.Response = "ok";
                }
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{data.sign}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.trade_no;
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            _logger.LogInformation($"CallBackDebit, bodyContent:{bodyContent}");

            var oNotify = new NotifyDepositResult()
            {
                DepositId = data.out_trade_no
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("amount", data.amount);
            toMD5Data.Add("callback_url", data.callback_url);
            toMD5Data.Add("out_trade_no", data.out_trade_no);
            toMD5Data.Add("request_amount", data.request_amount);
            toMD5Data.Add("state", data.state);
            toMD5Data.Add("trade_no", data.trade_no);
            var queryString = string.Join("&", toMD5Data.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var p = queryString + payResource["apiToken"] + payResource["callBackToken"];
            var sign = ToMD5(p).ToLower();

            if (sign == data.sign)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.request_amount);
                oNotify.ActualAmount = decimal.Parse(data.amount);
                oNotify.ThirdId = data.trade_no;
                oNotify.Comment = data.state;
                oNotify.Message = data.state;
                oNotify.Response = "ok";
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{data.sign}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.trade_no;
                oNotify.Comment = "sign error";
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"] + "/api/payment";

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("account_number", inputs["AccountNo"]);
            toMD5Data.Add("amount", inputs["Amount"]);
            toMD5Data.Add("bank_id", inputs["BankCode"]);
            toMD5Data.Add("bank_owner", inputs["AccountName"]);
            toMD5Data.Add("callback_url", inputs["CallBackUrl"]);
            toMD5Data.Add("out_trade_no", inputs["TransID"]);
            toMD5Data.Add("phone", inputs["PhoneNo"]);
            var queryString = string.Join("&", toMD5Data.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var p = queryString + inputs["apiToken"] + inputs["callBackToken"];
            var sign = ToMD5(p).ToLower();

            var req = new Dictionary<string, string>
            {
                {"account_number", inputs["AccountNo"]},
                {"out_trade_no", inputs["TransID"]},
                {"bank_id", inputs["BankCode"]},
                {"bank_owner", inputs["AccountName"]},
                {"amount", inputs["Amount"]},
                {"phone", inputs["PhoneNo"]},
                {"callback_url", inputs["CallBackUrl"]},
                {"sign", sign}
            };

            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(inputs["apiToken"])
                .WithHeader("accept", "application/json")
                .PostUrlEncoded<Dictionary<string, string>, CreditResp>(sUrl, req);

            var oWithdrawResult = new WithdrawResult();

            if (!statusCode.IsSuccessStatus())
            {
                oWithdrawResult.Success = false;
                return oWithdrawResult;
            }

            if (result.success)
            {
                oWithdrawResult.Success = result.success;
                oWithdrawResult.Message = result.data.state;
            }
            else
            {
                oWithdrawResult.Success = result.success;
                oWithdrawResult.Message = result.data.state;
            }

            return oWithdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var oDepositResult = new DepositResult();
            var sUrl = inputs["host"] + "/api/transaction";
            var req = new Dictionary<string, string>
            {
                {"amount", inputs["Amount"]},
                {"callback_url", inputs["CallBackUrl"]},
                {"out_trade_no", inputs["TransID"]}
            };

            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(inputs["apiToken"])
                .WithHeader("accept", "application/json")
                .PostUrlEncoded<Dictionary<string, string>, DebitResp>(sUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                oDepositResult.Success = false;
                return oDepositResult;
            }

            if (result.success)
            {
                oDepositResult.Success = result.success;
                oDepositResult.Message = "";
                oDepositResult.Url = result.data.uri;
            }
            else
            {
                oDepositResult.Success = result.success;
                oDepositResult.Message = result.message;
            }
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