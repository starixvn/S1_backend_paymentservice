using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.GlobalPay
{
    public class PayHelper : PayHelperBase
    {
        #region Model
        public class CallBackCreditReq
        {
            public string sign { set; get; }
            public string tradeNo { set; get; }
            public decimal totalAmount { set; get; }
            public string outTradeNo { set; get; }
            public string tradeTime { set; get; }
            public TradeStatusEnum tradeStatus { set; get; }
            public string attach { set; get; }
        }

        public class DepositResp
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
        public class CallBackDebitReq
        {
            public string sign { set; get; }
            public string tradeNo { set; get; }
            public string orderAmount { set; get; }
            public decimal totalAmount { set; get; }
            public string outTradeNo { set; get; }
            public string tradeTime { set; get; }
            public int tradeStatus { set; get; }
            public string otherTradeNo { set; get; }
            public string attach { set; get; }
        }
        public class attach
        {
            public string bankAccount { get; set; }
            public string bankCard { get; set; }
            public string bankMark { get; set; }
            public string bankName { get; set; }
        }
        public class WithdrawResp
        {
            public string code { get; set; }
            public string msg { get; set; }
            public string tradeNo { get; set; }
            public string outTradeNo { get; set; }
        }
        #endregion

        #region Enum
        public enum TradeStatusEnum 
        {
            Pending = 0,
            Success = 1,
            Fail = 2,
            Paying = 3,
            PayByTransfer = 4
        }
        #endregion

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(bodyContent);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.tradeNo;
            oNotify.ThirdId = data.outTradeNo;

            if (data.tradeStatus == TradeStatusEnum.Success || data.tradeStatus == TradeStatusEnum.PayByTransfer)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = data.totalAmount;
                oNotify.Response = "success";
            }
            else if (data.tradeStatus == TradeStatusEnum.Fail)
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Response = "success";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = "success";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.tradeNo;
            if (data.tradeStatus == 1)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = data.totalAmount;
                oNotify.ThirdId = data.outTradeNo;
                oNotify.RefThirdId = data.otherTradeNo;
                oNotify.Message = "Success";
                oNotify.Response = "success";
            }
            else if (data.tradeStatus == 0)
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.outTradeNo;
                oNotify.Message = "Fail";
                oNotify.Response = "success";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.outTradeNo;
                oNotify.Message = "Pending";
                oNotify.Response = "success";
            }
            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var oWithdrawResult = new WithdrawResult();
            var payCode = inputs["PayCode"];
            string[] payCodes = payCode.Split('-');
            var sUrl = inputs["host"];
            var key = inputs["key"];
            //var tradeType = payCodes[0];
            //var productId = payCodes[1];
            //var alliesNo = inputs["alliesNo"];
            //var clientId = inputs["clientId"];
            //var tradeNo = inputs["TransID"];
            //var totalAmount = inputs["Amount"];
            //var noticeUrl = inputs["CallBackUrl"];
            //var bankAccount = inputs["AccountName"];
            //var bankCard = inputs["AccountNo"];
            //var bankMark = inputs["BankCode"];
            //var bankName = inputs["BankName"];
            var attach = new attach()
            {
                bankAccount = inputs["AccountName"],
                bankCard = inputs["AccountNo"],
                bankMark = inputs["BankCode"],
                bankName = inputs["BankName"],
            };
            var newattach = ToBase64Encode(JsonConvert.SerializeObject(attach));
            Dictionary<string, string> paraidc = new Dictionary<string, string>();

            paraidc.Add("alliesNo", inputs["alliesNo"]);
            paraidc.Add("noticeUrl", inputs["CallBackUrl"]);
            paraidc.Add("productId", payCodes[1]);
            paraidc.Add("orderAmount", inputs["Amount"]);
            paraidc.Add("tradeNo", inputs["TransID"]);
            paraidc.Add("tradeType", payCodes[0]);
            paraidc.Add("attach", newattach);

            var Param = string.Join("&", paraidc.OrderBy(x => x.Key).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sign = GetSign(Param, key);
            paraidc.Add("sign", sign);
            var sParam = string.Join("&", paraidc.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var sResponse = HttpPost(sUrl + "peerpay/bussiness/order", sParam, "application/x-www-form-urlencoded", "0");

            var withdrawResp = new WithdrawResp();
            withdrawResp = JsonConvert.DeserializeObject<WithdrawResp>(sResponse);

            if (withdrawResp.code == "10000")
            {
                oWithdrawResult.Success = true;
            }
            else
            {
                oWithdrawResult.Success = false;
                oWithdrawResult.RawContent = sResponse;
            }
            return oWithdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var oDepositResult = new DepositResult();
            Dictionary<string, string> paraidc = new Dictionary<string, string>();
            var payCode = inputs["PayCode"];
            string[] payCodes = payCode.Split('-');
            var sUrl = inputs["host"];
            var key = inputs["key"];
            var tradeType = payCodes[0];
            var productId = payCodes[1];
            //var alliesNo = inputs["alliesNo"];
            //var clientId = inputs["clientId"];
            //var tradeNo = inputs["TransID"];
            //var totalAmount = inputs["Amount"];
            //var noticeUrl = inputs["CallBackUrl"];
            //var frontNotifyUrl = inputs["ReturnUrl"];

            paraidc.Add("alliesNo", inputs["alliesNo"]);
            paraidc.Add("frontNotifyUrl", inputs["ReturnUrl"]);
            paraidc.Add("noticeUrl", inputs["CallBackUrl"]);
            paraidc.Add("productId", payCodes[1]);
            paraidc.Add("totalAmount", inputs["Amount"]);
            paraidc.Add("tradeNo", inputs["TransID"]);
            paraidc.Add("tradeType", payCodes[0]);
            var Param = string.Join("&", paraidc.OrderBy(x => x.Key).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sign = GetSign(Param, key);
            paraidc.Add("sign", sign);

            var sParam = string.Join("&", paraidc.OrderBy(x => x.Key).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
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
