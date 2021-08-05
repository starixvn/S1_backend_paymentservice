using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Payment.DirePay
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawReq
        {
            public string txid { get; set; }
            public string amount { get; set; }
            public string token { get; set; }
            public string address { get; set; }
            public string customer_email { get; set; }
            public string client_reference_id { get; set; }
            public string status { get; set; }
            public string tx_hash { get; set; }
            public string fee { get; set; }
            public string hash_code { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDepositReq>(bodyContent);

            NotifyWithdrawResult oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.client_reference_id;
            oNotify.ThirdId = data.status.ToString();
            oNotify.Message = data.status.ToString();

            if (data.status == "2")
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.paid_amount);
                oNotify.Response = "{\"status\":\"ok\"}";
            }
            else if (data.status == "0")
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = "{\"status\":\"ok\"}";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Response = "{\"status\":\"ok\"}";
            }

            return oNotify;
        }

        private class CallBackDepositReq
        {
            public string txid { get; set; }
            public line_items[] line_items { get; set; }
            public string tx_hash { get; set; }
            public string customer { get; set; }
            public string customer_email { get; set; }
            public string client_reference_id { get; set; }
            public string paid_amount { get; set; }
            public string paid_fiat { get; set; }
            public string status { get; set; }
            public string hash_code { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDepositReq>(bodyContent);

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = payResource["TransID"];
            oNotify.ThirdId = data.client_reference_id;
            oNotify.Comment = data.status;
            oNotify.Message = data.status;

            if (data.status == "200")
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.paid_amount);
                oNotify.ActualAmount = decimal.Parse(data.paid_amount);
                oNotify.RefThirdId = data.txid;
                oNotify.Response = "true";
            }
            else if (data.status == "expired")
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Response = "true";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawGetWalletReq
        {
            public string command { get; set; }
            public string hashCode { get; set; }
            public string amount { get; set; }
            public string currency { get; set; }
            public string withdrawal_amount { get; set; }
            public string withdrawal_token { get; set; }
            public string address { get; set; }
            public string callback_url { get; set; }
            public string customer_uid { get; set; }
        }

        private class WithdrawGetWalletResp
        {
            public string message { get; set; }
            public string txid { get; set; }
            public string amount { get; set; }
            public string token { get; set; }
            public string status { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            string getReceptionToken = sUrl + "/api/login";
            string getWalletAddress = sUrl + "/api/callBack";

            Dictionary<string, string> depositGetH5Req = new Dictionary<string, string>();
            depositGetH5Req.Add("email", inputs["Email"]);
            depositGetH5Req.Add("password", inputs["Password"]);
            string sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = null;

            DepositResult oDepositResult = new DepositResult();
            oDepositResult.Success = false;

            WithdrawResult withdrawResult = new WithdrawResult();
            try
            {
                sResponse = HttpPost(getReceptionToken, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());

                DepositResp depositResp = new DepositResp();
                depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);

                if (depositResp != null && depositResp.access_token != null)
                {
                    WithdrawGetWalletReq withdrawGetWalletReq = new WithdrawGetWalletReq();
                    withdrawGetWalletReq.command = "partner_withdraw";
                    decimal amount = decimal.Parse(inputs["Amount"]);
                    withdrawGetWalletReq.amount = amount.ToString("#");
                    withdrawGetWalletReq.withdrawal_amount = amount.ToString("#");
                    withdrawGetWalletReq.currency = inputs["CurrencyId"];
                    withdrawGetWalletReq.withdrawal_token = $"{inputs["PayCurrencyId"]}-USDT";
                    withdrawGetWalletReq.callback_url = inputs["CallBackUrl"];
                    withdrawGetWalletReq.address = inputs["address"];
                    withdrawGetWalletReq.customer_uid = inputs["CustId"];
                    withdrawGetWalletReq.hashCode = ToMD5("partner_withdraw" + inputs["key"]).ToLower();
                    string sResponse1 = HttpPost(getWalletAddress, ToJson(withdrawGetWalletReq), "application/json", depositResp.access_token, inputs["TransID"].ToLower());

                    WithdrawGetWalletResp withdrawGetWalletResp = new WithdrawGetWalletResp();
                    withdrawGetWalletResp = JsonConvert.DeserializeObject<WithdrawGetWalletResp>(sResponse1);

                    withdrawResult.Message = withdrawGetWalletResp.message;
                    withdrawResult.RawContent = withdrawGetWalletResp.message;
                    if (withdrawGetWalletResp.status == "pending")
                    {
                        withdrawResult.Success = true;
                    }
                    else
                    {
                        withdrawResult.Success = false;
                    }

                    return withdrawResult;
                }
                else
                {
                    oDepositResult.Message = depositResp.message;
                    oDepositResult.RawContent = sResponse;
                }
            }
            catch (WebException wEx)
            {
                var err = FromJson<Dictionary<string, string>>(new StreamReader(wEx.Response?.GetResponseStream()).ReadToEnd());
                if (err.ContainsKey("error"))
                {
                    oDepositResult.Message = $"{err["error"]}-{err["error_message"]}";
                    oDepositResult.RawContent = $"{err["error"]}-{err["error_message"]}";
                }
                if (err.ContainsKey("err"))
                {
                    oDepositResult.Message = $"{err["err"]}";
                    oDepositResult.RawContent = $"{err["err"]}";
                }
            }

            return withdrawResult;
        }

        private class DepositResp
        {
            public string access_token { get; set; }
            public string message { get; set; }
        }

        private class DepositGetWalletReq
        {
            public string command { get; set; }
            public line_items[] line_items { get; set; }
            public string hashCode { get; set; }
            public string currency { get; set; }
            public string token { get; set; }
            public string callback_url { get; set; }
            public string customer_uid { get; set; }
            public string customer_email { get; set; }
            public string client_reference_id { get; set; }
        }

        private class line_items
        {
            public string name { get; set; }
            public string item_id { get; set; }
            public string description { get; set; }
            public string amount { get; set; }
            public string quantity { get; set; }
        }

        private class DepositGetWalletResp
        {
            public string message { get; set; }
            public string txid { get; set; }
            public string customer_uid { get; set; }
            public string invoice_amount { get; set; }
            public string invoice_currency { get; set; }
            public string payment_token { get; set; }
            public string rates { get; set; }
            public string payment_address { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            string getReceptionToken = sUrl + "/api/login";
            string getWalletAddress = sUrl + "/api/callBack";

            Dictionary<string, string> depositGetH5Req = new Dictionary<string, string>();
            depositGetH5Req.Add("email", inputs["Email"]);
            depositGetH5Req.Add("password", inputs["Password"]);
            string sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            DepositResult oDepositResult = new DepositResult();
            oDepositResult.Success = false;
            try
            {
                string sResponse = HttpPost(getReceptionToken, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower());

                DepositResp depositResp = new DepositResp();
                depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);

                if (depositResp != null && depositResp.access_token != null)
                {
                    DepositGetWalletReq depositGetWalletReq = new DepositGetWalletReq();
                    depositGetWalletReq.command = "payment";
                    depositGetWalletReq.line_items = new line_items[1];
                    depositGetWalletReq.line_items[0] = new line_items();
                    depositGetWalletReq.line_items[0].name = "deposit";
                    depositGetWalletReq.line_items[0].item_id = $"{inputs["PayCurrencyId"]}";
                    depositGetWalletReq.line_items[0].description = $"Deposit {currMapping(inputs["CurrencyId"])} via USDT";
                    decimal amount = decimal.Parse(inputs["Amount"]);
                    depositGetWalletReq.line_items[0].amount = amount.ToString("#");
                    depositGetWalletReq.line_items[0].quantity = "1";
                    depositGetWalletReq.currency = currMapping(inputs["CurrencyId"]);
                    depositGetWalletReq.token = $"{inputs["PayCurrencyId"]}-USDT";
                    depositGetWalletReq.callback_url = inputs["CallBackUrl"];
                    depositGetWalletReq.customer_uid = inputs["CustId"];
                    depositGetWalletReq.hashCode = ToMD5("payment" + inputs["key"]).ToLower();
                    string sResponse1 = HttpPost(getWalletAddress, ToJson(depositGetWalletReq), "application/json", depositResp.access_token, inputs["TransID"].ToLower());

                    DepositGetWalletResp depositGetWalletResp = new DepositGetWalletResp();
                    depositGetWalletResp = JsonConvert.DeserializeObject<DepositGetWalletResp>(sResponse1);

                    if (depositGetWalletResp != null && depositGetWalletResp.txid != null)
                    {
                        Dictionary<string, string> ResData = new Dictionary<string, string>();
                        ResData.Add("DepositAmount", depositGetWalletResp.invoice_amount);
                        ResData.Add("Rate", depositGetWalletResp.rates);
                        decimal requestAmount = decimal.Parse(depositGetWalletResp.invoice_amount) * decimal.Parse(depositGetWalletResp.rates);
                        ResData.Add("RequestAmount", requestAmount.ToString());
                        ResData.Add("Token", depositGetWalletResp.payment_token);

                        oDepositResult.Success = true;
                        oDepositResult.Url = depositGetWalletResp.payment_address;
                        oDepositResult.ThirdId = depositGetWalletResp.txid;
                        oDepositResult.FormData = ResData;
                    }
                    else
                    {
                        oDepositResult.Message = $"{depositGetWalletResp.message}";
                        oDepositResult.RawContent = sResponse1;
                    }
                }
                else
                {
                    oDepositResult.Message = depositResp.message;
                    oDepositResult.RawContent = sResponse;
                }
            }
            catch (WebException wEx)
            {
                var err = FromJson<Dictionary<string, string>>(new StreamReader(wEx.Response?.GetResponseStream()).ReadToEnd());
                if (err.ContainsKey("error"))
                {
                    oDepositResult.Message = $"{err["error"]}-{err["error_message"]}";
                    oDepositResult.RawContent = $"{err["error"]}-{err["error_message"]}";
                }
                if (err.ContainsKey("err"))
                {
                    oDepositResult.Message = $"{err["err"]}";
                    oDepositResult.RawContent = $"{err["err"]}";
                }
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

        private class GetExchangeResp
        {
            public string amount { get; set; }
            public string currency { get; set; }
            public string message { get; set; }
        }
    }
}
