using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Payment.Porttopup
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        private class CallBackDebitReq
        {
            public string data { get; set; }
            public string code { get; set; }
            public string message { get; set; }
            public string price { get; set; }
            public string request_id { get; set; }
            public string tran_id { get; set; }
            public string acc_game { get; set; }
            public string partner_code { get; set; }
            public string signature { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            NotifyDepositResult oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var tmp = ToBase64Decode(data.data);
            string[] sArray = tmp.Split('|');
            data.code = sArray[0];
            data.message = sArray[1];
            data.price = sArray[2];
            data.request_id = sArray[3];
            data.tran_id = sArray[4];
            data.acc_game = sArray[5];
            data.partner_code = sArray[6];
            data.signature = sArray[7];

            string p = $"{data.code}|{data.message}|{data.price}|{data.request_id}|{data.tran_id}|{data.acc_game}|{data.partner_code}";
            string sign = ToHMACSHA256Base64(p,payResource["secret"]);

            oNotify.DepositId = data.request_id;
            oNotify.ThirdId = data.tran_id;
            oNotify.Message = data.message;
            oNotify.Comment = data.message;

            if (sign == data.signature)
            {
                if (data.code == "0")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.price);
                    oNotify.ActualAmount = decimal.Parse(data.price) * decimal.Parse(payResource["Rate"]);
                    oNotify.Response = "{\"rc\": 0, \"rd\": \"OK\"}";
                }
                else if (data.code == "44" || data.code == "46" || data.code == "56" || data.code == "58" || data.code == "62" || data.code == "64")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "{\"rc\": 0, \"rd\": \"OK\"}";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "{\"rc\": 0, \"rd\": \"OK\"}";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "fail";
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

        private class DepositRes
        {
            public string rc { get; set; }
            public string rd { get; set; }
            public string Request_id { get; set; }
            public string Tran_idrc { get; set; }
            public string Partner_code { get; set; }
            public string Price { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            string sUrl = inputs["host"];
            Dictionary<string, string> formData = new Dictionary<string, string>();

            formData.Add("request_id", inputs["TransID"]);
            formData.Add("partner_code", inputs["merchant_no"]);
            formData.Add("card_code", inputs["Code"]);
            formData.Add("card_serial", inputs["Serial"]);
            decimal amount = decimal.Parse(inputs["Amount"]);
            formData.Add("card_value", amount.ToString("#")); 
            formData.Add("acc_game", inputs["CustId"]);
            formData.Add("telco", inputs["PayCode"]);
            formData.Add("access_key", inputs["key"]);

            string p = $"{formData["request_id"]}|{formData["partner_code"]}|{formData["acc_game"]}|{formData["card_code"]}|{formData["card_serial"]}|" +
                        $"{formData["card_value"]}|{formData["telco"]}|{formData["access_key"]}";

            formData.Add("signature", ToHMACSHA256Base64(p, inputs["secret"]));
            string sParam = $"{formData["request_id"]}|{formData["partner_code"]}|{formData["acc_game"]}|{formData["card_code"]}|{formData["card_serial"]}|" +
                            $"{formData["card_value"]}|{formData["telco"]}|{formData["access_key"]}|{formData["signature"]}";
            string sResponse = HttpGet(sUrl, $"data={ToBase64Encode(sParam)}", formData["request_id"]);

            DepositRes depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            DepositResult oDepositResult = new DepositResult();

            Dictionary<string, string> ResData = new Dictionary<string, string>();
            ResData.Add("Message", depositRes.rd);
            oDepositResult.FormData = ResData;
            oDepositResult.Message = depositRes.rd;

            if (depositRes.rc == "0")
            {
                oDepositResult.Success = true;
            }
            else
            {
                oDepositResult.Success = false;
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

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var tmp = ToBase64Decode(data.data);
            string[] sArray = tmp.Split('|');
            data.code = sArray[0];
            data.message = sArray[1];
            data.price = sArray[2];
            data.request_id = sArray[3];
            data.tran_id = sArray[4];
            data.acc_game = sArray[5];
            data.partner_code = sArray[6];
            data.signature = sArray[7];

            return data.request_id;
        }
    }
}
