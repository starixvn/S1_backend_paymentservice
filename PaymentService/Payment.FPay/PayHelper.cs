using Newtonsoft.Json;
using PaymentCore;
using System.Net;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.FPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private class CallBackCreditReq
        {
            public string order_id { get; set; }
            public decimal amount { get; set; }
            public string currency { get; set; }
            public string order_status { get; set; }
            public bool status { get; set; }
            public string charge { get; set; }
            public string token { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string remarks { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(bodyContent);
            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = data.order_id
            };
            var p = payResource["WithdrawSecretKey"] + data.order_id;
            var sign = ToMD5(p).ToLower();

            if (sign == data.token)
            {
                if (data.order_status == "completed")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.amount;
                    oNotify.ThirdId = data.order_id;
                    oNotify.Message = data.order_status;
                    oNotify.Response = "200";
                }
                else if (data.order_status == "fail")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.order_id;
                    oNotify.Message = data.remarks;
                    oNotify.Response = "200";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.order_id;
                    oNotify.Message = data.remarks;
                    oNotify.Response = "200";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.order_id;
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string order_id { set; get; }
            public string amount { set; get; }
            public string currency { set; get; }
            public string order_status { set; get; }
            public bool status { set; get; }
            public string token { set; get; }
            public string charge { set; get; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.order_id;

            var p = payResource["secretkey"] + data.order_id;
            var sign = ToMD5(p).ToLower();

            if (sign == data.token)
            {
                if (data.order_status == "completed")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.ThirdId = data.order_id;
                    oNotify.Comment = data.order_status;
                    oNotify.Message = data.order_status;
                    oNotify.Response = "200";
                }
                else if (data.order_status == "fail")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.order_id;
                    oNotify.Comment = data.order_status;
                    oNotify.Message = data.order_status;
                    oNotify.Response = "200";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.order_id;
                    oNotify.Comment = data.order_status;
                    oNotify.Message = data.order_status;
                    oNotify.Response = "200";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.order_id;
                oNotify.Comment = "sign error";
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        private class WithdrawResp
        {
            public bool status { get; set; }
            public string message { get; set; }
        }
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var oDepositResult = new WithdrawResult();
            try
            {
                var dicP = new Dictionary<string, string>()
                {
                    { "username", inputs["WithdrawUsername"] },
                    { "api_key", inputs["WithdrawKey"] }
                };
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var sResponse = HttpPost(sUrl + "merchant/auth", sParam, "application/x-www-form-urlencoded", "0");

                var authResp = JsonConvert.DeserializeObject<AuthResp>(sResponse);
                if (authResp.status)
                {
                    var amount = decimal.Parse(inputs["Amount"]);
                    var withdrawReq = new Dictionary<string, string>()
                    {
                        { "auth", authResp.auth },
                        { "amount", amount.ToString("#") },
                        { "currency", inputs["PayCurrencyId"] },
                        { "orderid", inputs["TransID"] },
                        { "bank_id", inputs["BankCode"] },
                        { "bank_branch", "" },
                        { "holder_name", inputs["AccountName"] },
                        { "account_no", inputs["AccountNo"] }
                    };
                    sParam = string.Join("&", withdrawReq.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                    sResponse = HttpPost(sUrl + "merchant/withdraw_orders", sParam, "application/x-www-form-urlencoded", inputs["TransID"]);

                    var depositResp = JsonConvert.DeserializeObject<WithdrawResp>(sResponse);
                    if (depositResp.status)
                    {
                        oDepositResult.Success = true;
                        oDepositResult.Message = "";
                    }
                    else
                    {
                        oDepositResult.Message = depositResp.message;
                        oDepositResult.RawContent = sResponse;
                    }
                }
                else
                {
                    oDepositResult.Message = "";
                    oDepositResult.RawContent = sResponse;
                }
            }
            catch (WebException wEx)
            {
                var err = FromJson<Dictionary<string, string>>(new StreamReader(wEx.Response?.GetResponseStream()).ReadToEnd());
                if (err.ContainsKey("error"))
                    oDepositResult.Message = $"{err["error"]}-{err["error_message"]}";
                if (err.ContainsKey("err"))
                    oDepositResult.Message = $"{err["err"]}";
            }

            return oDepositResult;
        }

        private class AuthReq
        {
            public string username { get; set; }
            public string api_key { get; set; }
        }

        private class AuthResp
        {
            public bool status { get; set; }
            public string auth { get; set; }
            public string message { get; set; }
        }

        private class DepositReq
        {
            public string username { get; set; }
            public string auth { get; set; }
            public string amount { get; set; }
            public string currency { get; set; }
            public string orderid { get; set; }
            public string email { get; set; }
            public string phone_number { get; set; }
        }

        private class DepositResp
        {
            public bool status { get; set; }
            public string p_url { get; set; }
            public string message { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var authReq = new AuthReq();
            authReq.username = inputs["Username"];
            authReq.api_key = inputs["key"];
            string sResponse = null;
            var oDepositResult = new DepositResult();
            oDepositResult.Success = false;

            try
            {
                var dicP = FromJson<Dictionary<string, string>>(ToJson(authReq));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                sResponse = HttpPost(sUrl + "merchant/auth", sParam, "application/x-www-form-urlencoded", "0");

                var authResp = new AuthResp();
                authResp = JsonConvert.DeserializeObject<AuthResp>(sResponse);

                if (authResp.status)
                {
                    var depositReq = new DepositReq();
                    depositReq.username = inputs["Username"];
                    depositReq.auth = authResp.auth;
                    var amount = decimal.Parse(inputs["Amount"]);
                    depositReq.amount = amount.ToString("#");
                    depositReq.currency = inputs["PayCurrencyId"];
                    depositReq.orderid = inputs["TransID"];
                    depositReq.email = "test@test.com";
                    depositReq.phone_number = "123456789";

                    dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
                    sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                    sResponse = HttpPost(sUrl + "merchant/generate_orders", sParam, "application/x-www-form-urlencoded", depositReq.orderid);

                    var depositResp = new DepositResp();
                    depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);

                    if (depositResp.status)
                    {
                        oDepositResult.Success = true;
                        oDepositResult.Url = depositResp.p_url;
                        oDepositResult.FormData = null;
                        oDepositResult.Message = "";
                    }
                    else
                    {
                        oDepositResult.Message = depositResp.message;
                        oDepositResult.RawContent = sResponse;
                    }
                }
                else
                {
                    oDepositResult.Message = "";
                    oDepositResult.RawContent = sResponse;
                }
            }
            catch (WebException wEx)
            {
                var err = FromJson<Dictionary<string, string>>(new StreamReader(wEx.Response?.GetResponseStream()).ReadToEnd());
                if (err.ContainsKey("error"))
                    oDepositResult.Message = $"{err["error"]}-{err["error_message"]}";
                if (err.ContainsKey("err"))
                    oDepositResult.Message = $"{err["err"]}";
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

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);
            return data.order_id;
        }
    }
}
