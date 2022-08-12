using Newtonsoft.Json;
using PaymentCore;
using System.Globalization;

namespace Payment.Xingji
{
    class PayHelper : PayHelperBase
    {
        private class CallBackWithdrawalReq
        {
            public string memberid { get; set; }
            public string orderid { get; set; }
            public string amount { get; set; }
            public string transaction_id { get; set; }
            public string datetime { get; set; }
            public string returncode { get; set; }
            public string transaction_code { get; set; }
            public string transaction_msg { get; set; }
            public string sign { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawalReq>(ToJson(formData));
            var p = $"amount={data.amount}&datetime={data.datetime}&memberid={data.memberid}&orderid={data.orderid}&returncode={data.returncode}&" +
                $"transaction_code={data.transaction_code}&transaction_id={data.transaction_id}&transaction_msg={data.transaction_msg}&key={payResource["key"]}";

            var sign = ToMD5(p).ToUpper();

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.orderid;
            oNotify.ThirdId = data.transaction_id;
            oNotify.Message = data.transaction_msg;
            if (sign == data.sign)
            {
                if (data.returncode == "00")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.Response = "OK";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "OK";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string memberid { get; set; }
            public string orderid { get; set; }
            public string amount { get; set; }
            public string transaction_id { get; set; }
            public string datetime { get; set; }
            public string paid_amount { get; set; }
            public string returncode { get; set; }
            public string attach { get; set; }
            public string sign { get; set; }
            public string msg { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var p = $"amount={data.amount}&datetime={data.datetime}&memberid={data.memberid}&orderid={data.orderid}&" +
                    $"returncode={data.returncode}&transaction_id={data.transaction_id}&key={payResource["key"]}";
            var sign = ToMD5(p).ToUpper();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.orderid;
            oNotify.ThirdId = data.transaction_id;
            oNotify.Comment = data.returncode;
            oNotify.Message = data.returncode;

            if (sign == data.sign)
            {
                if (data.returncode == "00")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.Response = "OK";
                }
                else if (data.returncode == "revoked")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
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

            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class WithdrawalReq
        {
            public string pay_memberid { get; set; }
            public string pay_out_trade_no { get; set; }
            public string pay_money { get; set; }
            public string pay_bankname { get; set; }
            public string pay_subbranch { get; set; }
            public string pay_accountname { get; set; }
            public string pay_cardnumber { get; set; }
            public string pay_province { get; set; }
            public string pay_city { get; set; }
            public string pay_notifyurl { get; set; }
            public string pay_md5sign { get; set; }
        }

        private class WithdrawalRes
        {
            public string status { get; set; }
            public string msg { get; set; }
            public string transaction_id { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var withdrawalReq = new WithdrawalReq();
            withdrawalReq.pay_memberid = inputs["custid"];
            withdrawalReq.pay_out_trade_no = inputs["TransID"];
            var amount = decimal.Parse(inputs["Amount"]);
            withdrawalReq.pay_money = amount.ToString("#");
            withdrawalReq.pay_bankname = inputs["BankCode"];
            withdrawalReq.pay_subbranch = "无";
            withdrawalReq.pay_accountname = inputs["AccountName"];
            withdrawalReq.pay_cardnumber = inputs["AccountNo"];
            withdrawalReq.pay_province = "无";
            withdrawalReq.pay_city = "无";
            withdrawalReq.pay_notifyurl = inputs["CallBackUrl"];

            var dicP = FromJson<Dictionary<string, string>>(ToJson(withdrawalReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={inputs["key"]}";
            dicP.Add("pay_md5sign", ToMD5(p).ToUpper());

            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", withdrawalReq.pay_out_trade_no);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.status == "success")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.msg;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.msg;
            }

            return withdrawResult;
        }

        private class DepositReq
        {
            public string pay_memberid { get; set; }
            public string pay_orderid { get; set; }
            public string pay_applydate { get; set; }
            public string pay_bankcode { get; set; }
            public string pay_notifyurl { get; set; }
            public string pay_amount { get; set; }
            public string pay_md5sign { get; set; }
            public string format { get; set; }
            public string pay_productname { get; set; }
        }

        private class DepositRes
        {
            public string status { get; set; }
            public string msg { get; set; }
            public DepositResData Data { get; set; }
            public string code { get; set; }
        }

        private class DepositResData
        {
            public string orderid { get; set; }

            public string transaction_id { get; set; }

            public string qr_url { get; set; }

            public string pay_url { get; set; }

            public string h5_url { get; set; }
            public string code { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var depositReq = new DepositReq();
            depositReq.pay_memberid = inputs["custid"];
            depositReq.pay_orderid = inputs["TransID"];
            var time = new DateTimeOffset(DateTime.Now);
            depositReq.pay_applydate = time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            depositReq.pay_notifyurl = inputs["CallBackUrl"];
            depositReq.pay_amount = Convert.ToInt32(decimal.Parse(inputs["Amount"])).ToString();
            depositReq.pay_bankcode = inputs["PayCode"];

            var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={inputs["key"]}";
            dicP.Add("pay_md5sign", ToMD5(p).ToUpper());
            dicP.Add("format","json");
            dicP.Add("pay_productname","payment");
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", depositReq.pay_orderid);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.status == "ok")
            {
                oDepositResult.Url = depositRes.Data.pay_url;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
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
    }
}
