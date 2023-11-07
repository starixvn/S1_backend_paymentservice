using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Models.Result;

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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackCredit, bodyContent:{bodyContent}");

            var oNotify = new NotifyWithdrawResult()
            {
                WithdrawId = formData["clientNo"]
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("clientCode", formData["clientCode"]);
            toMD5Data.Add("clientNo", formData["clientNo"]);
            toMD5Data.Add("orderNo", formData["orderNo"]);
            toMD5Data.Add("payAmount", formData["payAmount"]);
            toMD5Data.Add("status", formData["status"]);
            toMD5Data.Add("txid", formData["txid"]);
            var queryString = string.Join("&", toMD5Data.Select(x => x.Value).ToArray());

            var currencyData = formData["coinUnit"] switch
            {
                "THB" => new Dictionary<string, string> {
                    { "key", payResource["THBkey"] },{ "merchantNo", payResource["THBmerchantNo"]}
                    },
                "INR" => new Dictionary<string, string> {
                    { "key", payResource["INRkey"] },{ "merchantNo", payResource["INRmerchantNo"]}
                    }
            };

            var sign = ToMD5($"{queryString}{currencyData["key"]}").ToLower();

            if (sign == formData["sign"])
            {
                if (formData["status"] == "PAID" || formData["status"] == "FINISH")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(formData["payAmount"]);
                    oNotify.ThirdId = formData["clientNo"];
                    oNotify.Message = formData["status"];
                    oNotify.Response = "ok";
                }
                else if (formData["status"] == "PAYING")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = formData["clientNo"];
                    oNotify.Message = formData["status"];
                    oNotify.Response = "";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = formData["clientNo"];
                    oNotify.Message = formData["status"];
                    oNotify.Response = "";
                }
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{formData["sign"]}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = formData["clientNo"];
                oNotify.Message = formData["status"];
                oNotify.Response = "";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            _logger.LogInformation($"CallBackDebit, bodyContent:{bodyContent}");

            var oNotify = new NotifyDepositResult()
            {
                DepositId = formData["clientNo"]
            };

            var toMD5Data = new Dictionary<string, string>();
            toMD5Data.Add("clientCode", formData["clientCode"]);
            toMD5Data.Add("clientNo", formData["clientNo"]);
            toMD5Data.Add("orderNo", formData["orderNo"]);
            toMD5Data.Add("payAmount", formData["payAmount"]);
            toMD5Data.Add("status", formData["status"]);
            toMD5Data.Add("txid", formData["txid"]);
            var queryString = string.Join("&", toMD5Data.Select(x => x.Value).ToArray());

            var currencyData = formData["coinUnit"] switch
            {
                "THB" => new Dictionary<string, string> {
                    { "key", payResource["THBkey"] },{ "merchantNo", payResource["THBmerchantNo"]}
                    },
                "INR" => new Dictionary<string, string> {
                    { "key", payResource["INRkey"] },{ "merchantNo", payResource["INRmerchantNo"]}
                    }
            };

            var sign = ToMD5($"{queryString}{currencyData["key"]}").ToLower();

            if (sign == formData["sign"])
            {
                if (formData["status"] == "PAID" || formData["status"] == "FINISH")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(formData["payAmount"]);
                    oNotify.ActualAmount = decimal.Parse(formData["payAmount"]);
                    oNotify.ThirdId = formData["orderNo"];
                    oNotify.Comment = formData["status"];
                    oNotify.Message = formData["status"];
                    oNotify.Response = "ok";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = formData["orderNo"];
                    oNotify.Comment = formData["status"];
                    oNotify.Message = formData["status"];
                    oNotify.Response = "ok";
                }
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{formData["sign"]}");

                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = formData["orderNo"];
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
            var PayCurrencyId = inputs["PayCurrencyId"];
            var currencyData = PayCurrencyId switch
            {
                "THB" => new Dictionary<string, string> {
                    { "key", inputs["THBkey"] },{ "merchantNo", inputs["THBmerchantNo"]}, { "host", inputs["THBhost"]}
                    },
                "INR" => new Dictionary<string, string> {
                    { "key", inputs["INRkey"] },{ "merchantNo", inputs["INRmerchantNo"]}, { "host", inputs["INRhost"]}
                    }
            };

            var sUrl = currencyData["host"] + "/api/bank/agentPay/request";
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
            //THB沒有IFSC所以固定代字串IFSC
            dicP.Add("ifsc", PayCurrencyId == "INR" ? inputs["IFSCCode"] : "IFSC");
            dicP.Add("bankName", inputs["BankCode"]);
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
            var PayCurrencyId = inputs["PayCurrencyId"];
            var currencyData = PayCurrencyId switch
            {
                "THB" => new Dictionary<string, string> {
                    { "key", inputs["THBkey"] },{ "merchantNo", inputs["THBmerchantNo"]}, { "host", inputs["THBhost"]}
                    },
                "INR" => new Dictionary<string, string> {
                    { "key", inputs["INRkey"] },{ "merchantNo", inputs["INRmerchantNo"]}, { "host", inputs["INRhost"]}
                    }
            };

            var sUrl = currencyData["host"] + "/api/coin/pay/request";
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