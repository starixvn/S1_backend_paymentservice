using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.FXBit
{
    class PayHelper : PayHelperBase, IExchangeHelper
    {
        private class CNY
        {
            public DateTime updated_timestamp { get; set; }
            public string sellRate { get; set; }
        }

        private class BTC
        {
            public CNY CNY { get; set; }
        }

        private class ETH
        {
            public CNY CNY { get; set; }
        }

        private class USDT
        {
            public CNY CNY { get; set; }
        }

        private class USDC
        {
            public CNY CNY { get; set; }
        }

        private class GetExchangeResp
        {
            public BTC BTC { get; set; }
            public ETH ETH { get; set; }
            public USDT USDT { get; set; }
            public USDC USDC { get; set; }
        }

        public ExchangeResult GetExchange(Dictionary<string, string> payResource)
        {
            string sUrl = payResource["host"];
            string getExchangeRate = sUrl + "getExchangeRate";
            string apiKey = payResource["key"];

            string sResponse = HttpGet(getExchangeRate, null, "application/json", apiKey, null);
            var data = FromJson<GetExchangeResp>(sResponse);
            var result = ToJson(data);
            
            ExchangeResult oExchangeResult = new ExchangeResult();
            oExchangeResult.Message = result;

            return oExchangeResult;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        private class CallBackDepositReq
        {
            public string receipt_token { get; set; }
            public string marked_currency { get; set; }
            public string marked_price { get; set; }
            public string billed_currency { get; set; }
            public string billed_price { get; set; }
            public string received_amount { get; set; }
            public string status { get; set; }
            public string signature { get; set; }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDepositReq>(bodyContent);
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            string p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("signature", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&{payResource["key"]}";
            string sign = ToSHA256(p).ToLower();

            NotifyDepositResult oNotify = new NotifyDepositResult();
            oNotify.DepositId = payResource["TransID"];
            oNotify.ThirdId = data.receipt_token;
            oNotify.Comment = data.status;
            oNotify.Message = data.status;

            if (sign == data.signature)
            {
                if (data.status == "2")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.marked_price);
                    oNotify.ActualAmount = decimal.Parse(data.marked_price);
                    oNotify.Comment = data.status;
                    oNotify.Response = "success";
                }
                else if(data.status == "3")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
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
            throw new NotImplementedException();
        }

        private class DepositReq
        {
            public string marked_currency { get; set; }
            public string marked_price { get; set; }
            public string remark { get; set; }
            public string callback_url { get; set; }
        }
        private class DepositResp
        {
            public int error { get; set; }
            public string shop_identifier { get; set; }
            public string receipt_token { get; set; }
        }

        private class DepositGetWalletReq
        {
            public string receipt_token { get; set; }
            public string billed_currency { get; set; }
        }

        private class DepositGetWalletResp
        {
            public string public_address { get; set; }
            public string billed_price { get; set; }
            public string index { get; set; }
            public string rate { get; set; }
            public string expire { get; set; }
            public int error { get; set; }
            public int error_message { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            string apiKey = inputs["key"];
            string getReceptionToken = sUrl + "getReceiptToken";
            string getWalletAddress = sUrl + "getWalletAddress";
            string H5Url = inputs["H5host"];

            DepositReq depositReq = new DepositReq();
            depositReq.marked_currency = "USDT";
            decimal amount = decimal.Parse(inputs["Amount"]);
            depositReq.marked_price = amount.ToString("#");
            depositReq.remark = inputs["TransID"];
            depositReq.callback_url = inputs["CallBackUrl"];

            string sResponse = HttpPost(getReceptionToken, ToJson(depositReq), "application/json", apiKey, depositReq.remark);

            DepositResp depositResp = new DepositResp();
            depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);
            DepositResult oDepositResult = new DepositResult();
            oDepositResult.Success = false;

            if (depositResp.error == 0)
            {
                DepositGetWalletReq depositGetWalletReq = new DepositGetWalletReq();
                depositGetWalletReq.receipt_token = depositResp.receipt_token;
                depositGetWalletReq.billed_currency = inputs["PayCode"];

                string sResponse1 = HttpPost(getWalletAddress, ToJson(depositGetWalletReq), "application/json", apiKey, depositReq.remark);

                DepositGetWalletResp depositGetWalletResp = new DepositGetWalletResp();
                depositGetWalletResp = JsonConvert.DeserializeObject<DepositGetWalletResp>(sResponse1);

                if (depositGetWalletResp.error == 0)
                {
                    Dictionary<string, string> depositGetH5Req = new Dictionary<string, string>();
                    depositGetH5Req.Add("receipt_token", depositGetWalletReq.receipt_token);
                    depositGetH5Req.Add("wallet_addr", depositGetWalletResp.public_address);
                    string sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                    
                    oDepositResult.Success = true;
                    oDepositResult.Url = H5Url + "?" + sParam;
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
    }
}
