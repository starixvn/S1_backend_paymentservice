using Flurl;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.GlobalPay
{
    internal class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
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

        private class DepositReq
        {
            public string sign { set; get; }
            public string alliesNo { set; get; }
            public string tradeNo { set; get; }
            public string totalAmount { set; get; }
            public string tradeType { set; get; }
            public string noticeUrl { set; get; }
            public string productId { set; get; }
            public string frontNotifyUrl { set; get; }
        }
        private class DepositResp
        {
            public string code { set; get; }
            public string msg { set; get; }
            public string tradeNo { set; get; }
            public string payUrl { set; get; }
            public string outTradeNo { set; get; }
            public string bankCard { set; get; }
            public string bankName { set; get; }
            public string bankAccount { set; get; }
            public string bankAddress { set; get; }
            public string sign { set; get; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var depositReq = new DepositReq();
            var oDepositResult = new DepositResult();
            var sUrl = inputs["host"];
            var key = inputs["key"];
            var alliesNo = inputs["alliesNo"];
            var clientId = inputs["clientId"];
            var tradeNo = inputs["TransID"];
            var totalAmount = inputs["Amount"];
            var tradeType = "90";
            var noticeUrl = "http://payment.n88dev.com:59159/Payment/DepositCallBack/DP123456781";
            var productId = "0";
            var frontNotifyUrl = "https://stag.goal123.com/vn/";
            string Param = $"alliesNo={alliesNo}&tradeNo={inputs["TransID"]}&totalAmount={totalAmount}&tradeType={tradeType}";
            var sign = GetSign(Param, key);
            depositReq.sign = sign;
            depositReq.alliesNo = alliesNo;
            depositReq.tradeNo = tradeNo;
            depositReq.totalAmount = totalAmount;
            depositReq.tradeType = tradeType;
            depositReq.noticeUrl = noticeUrl;
            depositReq.productId = productId;
            depositReq.frontNotifyUrl = frontNotifyUrl;
            var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl + "alliesPay/bussiness/order", sParam, "application/x-www-form-urlencoded", "0");

            var depositResp = new DepositResp();
            depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);

            if (depositResp.code == "10000")
            {
                oDepositResult.Success = true;
                oDepositResult.Url = depositResp.payUrl;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Message = depositResp.msg;
                oDepositResult.RawContent = sResponse;
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

        private string GetSign(string para, string key)
        {
            string stringSignTemp = para + "&key=" + key;
            return ToMD5(stringSignTemp).ToUpper();
        }
    }
}
