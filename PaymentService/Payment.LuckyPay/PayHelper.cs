using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Models.Result;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

namespace Payment.LuckyPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        }
        public class DebitResp
        {
            public bool success { get; set; }
            public int code { get; set; }
            public DebitRespData data { get; set; }
            public string message { get; set; }
        }
        public class DebitRespData 
        { 
            public string orderNo { get; set; }
            public string receiveAddr { get; set; }
            public string chainName { get; set; }
            public string coinUnit { get; set; }
            public decimal requestAmount { get; set; }
            public string status { get; set; }
            public string payUrl { get; set; }
            public string hrefbackurl { get; set; }
            public string sign { get; set; }
        }
        public class CallBackDebitReq
        {
            public string clientCode { get; set; }
            public string clientNo { get; set; }
            public string orderNo { get; set; }
            public string chainName { get; set; }
            public string coinUnit { get; set; }
            public string payAmount { get; set; }
            public string status { get; set; }
            public string txid { get; set; }
            public string sign { get; set; }
        }

        public class CreditResp
        {
            public bool success { get; set; }
            public int code { get; set; }
            public CreditRespData data { get; set; }
            public string message { get; set; }
        }
        public class CreditRespData
        {
            public string orderNo { get; set; }
            public string receiveAddr { get; set; }
            public string chainName { get; set; }
            public string coinUnit { get; set; }
            public decimal requestAmount { get; set; }
            public string status { get; set; }
            public string sign { get; set; }
        }
        public class CallBackCreditReq
        {
            public string clientCode { get; set; }
            public string clientNo { get; set; }
            public string orderNo { get; set; }
            public string chainName { get; set; }
            public string coinUnit { get; set; }
            public string payAmount { get; set; }
            public string status { get; set; }
            public string txid { get; set; }
            public string sign { get; set; }
        }
        public class keyData
        {
            public string merchantNo { get; set; }
            public string key { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackCredit, bodyContent:{bodyContent}");
            var data = FromJson<CallBackCreditReq>(bodyContent);
            var oNotify = new NotifyWithdrawResult()
            {
                WithdrawId = data.clientNo
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("clientCode", data.clientCode);
            toMD5Data.Add("clientNo", data.clientNo);
            toMD5Data.Add("orderNo", data.orderNo);
            toMD5Data.Add("payAmount", data.payAmount);
            toMD5Data.Add("status", data.status);
            toMD5Data.Add("txid", data.txid);
            var queryString = string.Join("&", toMD5Data.Select(x => x.Value).ToArray());

            var keyList = new List<keyData>();
            keyList.Add(new keyData() { key = payResource["THBkey"], merchantNo = payResource["THBmerchantNo"] });
            keyList.Add(new keyData() { key = payResource["INRkey"], merchantNo = payResource["INRmerchantNo"] });
            var key = keyList.Find(x => x.merchantNo == data.clientCode).key;
            var sign = ToMD5($"{queryString}{key}").ToLower();

            if (sign == data.sign)
            {
                if (data.status == "PAID" || data.status == "FINISH")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.payAmount);
                    oNotify.ThirdId = data.clientNo;
                    oNotify.Message = data.status;
                    oNotify.Response = "ok";
                }
                else if (data.status == "PAYING")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.clientNo;
                    oNotify.Message = data.status;
                    oNotify.Response = "";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.clientNo;
                    oNotify.Message = data.status;
                    oNotify.Response = "";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.clientNo;
                oNotify.Message = data.status;
                oNotify.Response = "";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackDebit, bodyContent:{bodyContent}");
            var data = FromJson<CallBackDebitReq>(bodyContent);
            var oNotify = new NotifyDepositResult()
            {
                DepositId = data.clientNo
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("clientCode", data.clientCode);
            toMD5Data.Add("clientNo", data.clientNo);
            toMD5Data.Add("orderNo", data.orderNo);
            toMD5Data.Add("payAmount", data.payAmount);
            toMD5Data.Add("status", data.status);
            toMD5Data.Add("txid", data.txid);
            var queryString = string.Join("&", toMD5Data.Select(x => x.Value).ToArray());

            var keyList = new List<keyData>();
            keyList.Add(new keyData() { key = payResource["THBkey"], merchantNo = payResource["THBmerchantNo"] });
            keyList.Add(new keyData() { key = payResource["INRkey"], merchantNo = payResource["INRmerchantNo"] });
            var key = keyList.Find(x => x.merchantNo == data.clientCode).key;
            var sign = ToMD5($"{queryString}{key}").ToLower();

            if (sign == data.sign)
            {
                if (data.status == "PAID" || data.status == "FINISH")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.payAmount);
                    oNotify.ActualAmount = decimal.Parse(data.payAmount);
                    oNotify.ThirdId = data.orderNo;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "ok";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.orderNo;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = "ok";
                }
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{data.sign}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.orderNo;
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
            var sUrl = inputs["host"] + "/api/bank/agentPay/request";

            var PayCurrencyId = inputs["PayCurrencyId"];
            var currencyData = PayCurrencyId switch
            {
                "THB" => new Dictionary<string, string> {
                    { "key", inputs["THBkey"] },{ "merchantNo", inputs["THBmerchantNo"]}
                    },
                "INR" => new Dictionary<string, string> {
                    { "key", inputs["INRkey"] },{ "merchantNo", inputs["INRmerchantNo"]}
                    }
            };

            var currentTime = GetCurrentTime();
            var formData = new Dictionary<string, string>();
            formData.Add("clientCode", currencyData["merchantNo"]);
            formData.Add("chainName", "BANK");
            formData.Add("coinUnit", PayCurrencyId);
            formData.Add("clientNo", inputs["TransID"]);
            formData.Add("currentTime", currentTime);

            var queryString = string.Join("&", formData.Select(x => x.Value).ToArray());
            var sign = ToMD5($"{queryString}{currencyData["key"]}").ToLower();

            var dicP = new Dictionary<string, string>();
            dicP.Add("clientCode", currencyData["merchantNo"]);
            dicP.Add("chainName", "BANK");
            dicP.Add("coinUnit", PayCurrencyId);
            dicP.Add("clientNo", inputs["TransID"]);
            dicP.Add("bankCardNum", inputs["AccountNo"]);
            dicP.Add("bankUserName", inputs["AccountName"]);
            dicP.Add("ifsc", inputs["IFSCCode"]);
            dicP.Add("bankName", inputs["BankName"]);
            dicP.Add("amount", inputs["Amount"]);
            dicP.Add("requestTimestamp", currentTime);
            dicP.Add("callbackurl", inputs["CallBackUrl"]);
            dicP.Add("sign", sign);

            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"]);

            var creditResp = JsonConvert.DeserializeObject<CreditResp>(sResponse);

            var oWithdrawResult = new WithdrawResult();
            if (creditResp.success)
            {
                oWithdrawResult.Success = creditResp.success;
                oWithdrawResult.Message = "";
            }
            else
            {
                oWithdrawResult.Success = creditResp.success;
                oWithdrawResult.Message = creditResp.message;
            }

            return oWithdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"] + "/api/coin/pay/request";

            var PayCurrencyId = inputs["PayCurrencyId"];
            var currencyData = PayCurrencyId switch
            {
                "THB" => new Dictionary<string, string> {
                    { "key", inputs["THBkey"] },{ "merchantNo", inputs["THBmerchantNo"]}
                    },
                "INR" => new Dictionary<string, string> {
                    { "key", inputs["INRkey"] },{ "merchantNo", inputs["INRmerchantNo"]}
                    }
            };

            var currentTime = GetCurrentTime();
            var formData = new Dictionary<string, string>();
            formData.Add("clientCode", currencyData["merchantNo"]);
            formData.Add("chainName", "BANK");
            formData.Add("coinUnit", PayCurrencyId);
            formData.Add("clientNo", inputs["TransID"]);
            formData.Add("currentTime", currentTime);

            var queryString = string.Join("&", formData.Select(x=>x.Value).ToArray());
            var sign = ToMD5($"{queryString}{currencyData["key"]}").ToLower();

            var dicP = new Dictionary<string, string>();
            dicP.Add("clientCode", currencyData["merchantNo"]);
            dicP.Add("chainName", "BANK");
            dicP.Add("coinUnit", PayCurrencyId);
            dicP.Add("clientNo", inputs["TransID"]);
            dicP.Add("memberFlag", inputs["CustId"]);
            dicP.Add("requestAmount", inputs["Amount"]);
            dicP.Add("requestTimestamp", currentTime);
            dicP.Add("callbackurl", inputs["CallBackUrl"]);
            dicP.Add("hrefbackurl", inputs["ReturnUrl"]);
            dicP.Add("sign", sign);
            dicP.Add("toPayQr", "0");
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"]);

            var debitResp = JsonConvert.DeserializeObject<DebitResp>(sResponse);

            var oDepositResult = new DepositResult();
            if (debitResp.success)
            {
                oDepositResult.Success = debitResp.success;
                oDepositResult.Message = "";
                oDepositResult.Url = debitResp.data.payUrl;
            }
            else
            {
                oDepositResult.Success = debitResp.success;
                oDepositResult.Message = debitResp.message;
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

        private string GetCurrentTime()
        {
            Int64 retval = 0;
            DateTime st = new DateTime(1970, 1, 1);
            TimeSpan t = (DateTime.Now.ToUniversalTime() - st);
            retval = (Int64)(t.TotalMilliseconds + 0.5);
            return retval.ToString();
        }
    }
}