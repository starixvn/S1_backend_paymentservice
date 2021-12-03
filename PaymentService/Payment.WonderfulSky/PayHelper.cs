using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Payment.WonderfulSky
{
    class PayHelper : PayHelperBase
    {
        // public override ExchangeResult GetExchange(Dictionary<string, string> payResource)
        // {
        //     throw new NotImplementedException();
        // }

        //{
        //    public string requestId { get; set; }
        //    public string result { get; set; }
        //    public string sum { get; set; }
        //    public string totalSum { get; set; }
        //    public string balance { get; set; }
        //    public string canUsedBalance { get; set; }
        //    public string frozenBalance { get; set; }
        //    public string unsettledBalance { get; set; }
        //    public string payType { get; set; }
        //    public string bankAccount { get; set; }
        //    public string bankName { get; set; }
        //    public string name { get; set; }
        //    public string requestTime { get; set; }
        //    public string dealTime { get; set; }
        //    public string noticeType { get; set; }
        //    public string failReason { get; set; }
        //    public string signature { get; set; }
        //}

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<WithdrawalRes>(ToJson(formData));
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&key={payResource["key"]}";

            var rsa = new RSAHelper(RSAType.RSA, Encoding.Unicode, payResource["private_key"], payResource["public_key"]);
            var sign = rsa.Encrypt(p);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.merchantid;
            oNotify.ThirdId = data.merchantid;
            oNotify.Message = data.code;

            if (sign == data.sign)
            {
                if (data.result == "1000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.Response = "success";
                }
                else if (data.result == "0000")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string mid { get; set; }
            public string merchantid { get; set; }
            public string systemid { get; set; }
            public string amount { get; set; }
            public string status { get; set; }
            public string time { get; set; }
            public string remark { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var p = $"amount={data.amount}&merchantid={data.merchantid}&mid={data.mid}&status={data.status}&" +
                    $"systemid={data.systemid}&time={data.time}&{payResource["key"]}";

            var rsa = new RSAHelper(RSAType.RSA, Encoding.Unicode, payResource["private_key"], payResource["public_key"]);
            var sign= rsa.Encrypt(p);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.merchantid;
            oNotify.ThirdId = data.systemid;
            oNotify.Comment = data.status;
            oNotify.Message = data.status;

            if (sign == data.sign)
            {
                if (data.status == "1")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.amount);
                    oNotify.ActualAmount = decimal.Parse(data.amount);
                    oNotify.Response = "success";
                }
                else
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

        private class WithdrawalRes
        {
            public string result { get; set; }
            public string mid { get; set; }
            public string merchantid { get; set; }
            public string systemid { get; set; }
            public string amount { get; set; }
            public string code { get; set; }
            public string sign { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var formData = new Dictionary<string, string>();
            formData.Add("service", "Withdraw");
            formData.Add("mid", inputs["merchant_no"]);
            formData.Add("merchantid", inputs["TransID"]);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("currency", "CNY");
            formData.Add("AccountNo", inputs["ReturnUrl"]);
            formData.Add("cardname", inputs["AccountName"]);
            formData.Add("cardtype", "1");
            formData.Add("bankcode", inputs["BankCode"]);
            formData.Add("bankname", inputs["BankName"]);
            formData.Add("bankprovince", "");
            formData.Add("bankcity","");
            formData.Add("channel", "bank");
            //“bank”: 網銀 “alipay”: 支付寶 “wechat”:微信 “qqpay”: QQ支付

            var p = $"amount={formData["amount"]}&bankbranchname={formData["bankbranchname"]}&ankcity={formData["bankcity"]}&bankcode={formData["bankcode"]}&" +
                    $"bankname={formData["bankname"]}&bankprovince={formData["bankprovince"]}&cardname={formData["cardname"]}&cardno={formData["cardno"]}&" +
                    $"cardtype={formData["cardtype"]}&channel={formData["channel"]}&currency={formData["currency"]}&merchantid={formData["merchantid"]}&" +
                    $"mid={formData["mid"]}&service={formData["service"]}&{inputs["key"]}";

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            //string p = string.Join("&", dicP.Where(x => !x.Key.Equals("remark", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&{inputs["key"]}";


            var rsa = new RSAHelper(RSAType.RSA, Encoding.Unicode, inputs["private_key"], inputs["public_key"]);
            formData.Add("sign", rsa.Encrypt(p));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["merchantid"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.code == "00000" || withdrawalRes.code == "00001")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.result;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.result;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string result { get; set; }
            public string qrcodeurl { get; set; }
            public string mid { get; set; }
            public string merchantid { get; set; }
            public string sign { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var rsa = new RSAHelper(RSAType.RSA, Encoding.UTF8, inputs["private_key"], inputs["public_key"]);

            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>();
            formData.Add("service", "Payment");
            formData.Add("mid", inputs["merchant_no"]);
            formData.Add("merchantid", inputs["TransID"]);
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("amount", amount.ToString("#"));
            formData.Add("currency", "CNY");
            formData.Add("returnurl", inputs["ReturnUrl"]);
            formData.Add("notifyurl", inputs["CallBackUrl"]);
            formData.Add("channel", inputs["PayCode"]);
            formData.Add("remark", "");

            var dicP = FromJson<Dictionary<string, string>>(ToJson(formData)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Where(x => !x.Key.Equals("remark", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&{inputs["key"]}";
            // formData.Add("sign", rsa.Sign(p));

            // formData.Add("sign", RSASign(inputs["private_key"], p));

            //string final = "AHlUlHfYlYmR+41/xKwavGJmajutHEKSFrH6K2GtyPTDxENdcuK3AGNLHde9Ijqc6/cQvszlFHDHgQNfGIu1LM7xVVew5JzdWLpfnWPuY+YZhSfAHH3+ZoqLhe2kn7bVj4GO5qQ679U6n/C1Cs7PySeBUViM206zIrYabvzBw4rN4S+3FKcCUGQ4F23KGASS0mvxKp4SHZZfUQ/UD+nTX04rvcO31ekGbzI1jAEtz+f3AFQcZ8QsNBy7lahbtimJ1MpIPcwFptRcwq8tt4QRX9D4ihCEIRWXgBOowYDa0tmrUCda6V2T3o15TXSMoJMj0G//L7qRYdUk5ZelKf/LZtd3oXn3piRtdZGsJ50sVIU03ZysOQUVd/M7wSL6nzihYvSdwJh94xiirfvIjJ4CCrgd0a+WPfwX718WWwloTT8eYTvE1kbNXCHRxvVnJIF02ATA5kWClJUNDWPSX0HCmSbB5x8NaCqG2MpFpQvtMTaJyCz1RAPznVbvcAjhz4GM6lpXYFLuRtrkS4K7+iRpNLtfjkLC25Pd23XPFFDWByrASXkHz03xGCqs+4HF6p01ZD4VSeAwPxXhfs3mHgWNKB9cblJ1yc4nQvdxiC7wOULy0I6V8f2DFnpW8CZxqz4iB/blYLfXeC00aBzG4PCElcLb6VxkmdmqLI92KbPHN04=";
            //string text = "amount=300&channel=qqpay&currency=CNY&merchantid=DD1590&mid=6550&notifyurl=http://payment.n88dev.com:59159/Payment/DepositCallBack/DD1590&" +
            //    "returnurl=http://localhost:60930/cs/Cashier/Deposit&" +
            //    "service=Payment&5BEA2FEC1937F527BE82795999A96D10";
            //string b = rsa.Sign(text);
            //string aa = rsa.Sign(p);
            //int c = string.Compare(final, b);
            //int d = string.Compare(final, aa);



            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["merchantid"]);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.result == "1")
            {
                oDepositResult.Url = depositRes.qrcodeurl;
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
