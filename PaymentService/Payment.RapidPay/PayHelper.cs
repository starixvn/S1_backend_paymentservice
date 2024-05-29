using System.Net;

using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.RapidPay
{
    public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        #region Model
        private class BankResp
        {

        }
        private class BankReq
        {
            public string currency { get; set; }
            public string username { get; set; }
        }
        private class CurrencyResp
        {
            public bool status { get; set; }
            public IEnumerable<Rate> rate { get; set; }
        }
        private class Rate
        {
            public string currency { get; set; }
            public decimal min { get; set; }
            public decimal max { get; set; }
        }
        private class CurrencyReq
        {
            public string username { get; set; }
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
            public string order_id { get; set; }
            public string pp { get; set; }
        }

        #region Withdrawal
        private class WithdrawResp
        {
            public bool status { get; set; }
            public string message { get; set; }
        }
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
        #endregion

        #region Deposit
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
        private class CallBackDebitReq
        {
            public string order_id { set; get; }
            public string amount { set; get; }
            public string currency { set; get; }
            public string order_status { set; get; }
            public bool status { set; get; }
            public string token { set; get; }
            public string charge { set; get; }
            public string payment_type { get; set; }
        }
        #endregion

        #endregion

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(bodyContent);
            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = data.order_id
            };

            var secretkey = data.currency switch
            {
                "MYR" => payResource["wMYRSecret"],
                "THB" => payResource["wTHBSecret"],
                "IDR" => payResource["wIDRSecret"]
            };
            var p = secretkey + data.order_id;
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
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
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
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.order_id;

            var secretKeyName = GetDebitCallBackSecretKey(data.currency, data.payment_type);
            if (secretKeyName.Contains("Error"))
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.order_id;
                oNotify.Comment = "PaymentType error";
                oNotify.Message = "PaymentType error";
                oNotify.Response = secretKeyName;
                return oNotify;
            }
            var s = payResource[secretKeyName] + data.order_id;
            var sign = ToMD5(s).ToLower();
            
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
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
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
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var oDepositResult = new WithdrawResult();

            try
            {
                var PayCurrencyId = inputs["PayCurrencyId"];

                var currencyData = PayCurrencyId switch
                {
                    "MYR" => new Dictionary<string, string> {
                    { "User", inputs["wMYRUser"] },{ "APIKey", inputs["wMYRAPI"]}
                    },
                    "THB" => new Dictionary<string, string> {
                    { "User", inputs["wTHBUser"] },{ "APIKey", inputs["wTHBAPI"]}
                    },
                    "IDR" => new Dictionary<string, string> {
                    { "User", inputs["wIDRUser"] },{ "APIKey", inputs["wIDRAPI"]}
                    }
                };

                var authReq = new AuthReq();
                authReq.username = currencyData["User"];
                authReq.api_key = currencyData["APIKey"];
                var currencyReq = new CurrencyReq();
                currencyReq.username = currencyData["User"];
                var amount = decimal.Parse(inputs["Amount"]);
                var dicP = FromJson<Dictionary<string, string>>(ToJson(currencyReq));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var sResponse = HttpPost(sUrl + "merchant/currency", sParam, "application/x-www-form-urlencoded", "0");
                var currencyResp = new CurrencyResp();
                currencyResp = JsonConvert.DeserializeObject<CurrencyResp>(sResponse);

                #region 檢核CurrencyLimit
                if (!currencyResp.status)
                {
                    oDepositResult.Message = "";
                    oDepositResult.RawContent = sResponse;
                    return oDepositResult;
                }
                else if (!currencyResp.rate.ToList().Exists(x => x.currency == PayCurrencyId))
                {
                    oDepositResult.Message = "";
                    oDepositResult.RawContent = sResponse;
                    return oDepositResult;
                }
                else
                {
                    var currencydata = currencyResp.rate.ToList().Find(x => x.currency == PayCurrencyId);
                    if (currencydata.min > amount || currencydata.max < amount)
                    {
                        oDepositResult.Message = "";
                        oDepositResult.RawContent = sResponse;
                        return oDepositResult;
                    }
                }
                #endregion
                dicP = FromJson<Dictionary<string, string>>(ToJson(authReq));
                sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                sResponse = HttpPost(sUrl + "merchant/auth", sParam, "application/x-www-form-urlencoded", "0");
                var authResp = new AuthResp();
                authResp = JsonConvert.DeserializeObject<AuthResp>(sResponse);
                if (authResp.status)
                {
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


        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var currencyReq = new CurrencyReq();
            var authReq = new AuthReq();
            var oDepositResult = new DepositResult();
            var currencyEWalletData = new Dictionary<string, string>();
            var currencyData = new Dictionary<string, string>();
            
            var PayCurrencyId = inputs["PayCurrencyId"];
            var payCode = inputs["PayCode"];
            var isEWallet = !string.IsNullOrEmpty(payCode);


            if (isEWallet)
            {
                currencyEWalletData = PayCurrencyId switch
                {
                    "MYR" => new Dictionary<string, string>
                    {
                        { "User", inputs["DuitNowUser"] }, { "APIKey", inputs["DuitNowAPI"] }
                    },
                    "THB" => new Dictionary<string, string>
                    {
                        { "User", inputs["PromptPayUser"] }, { "APIKey", inputs["PromptPayAPI"] }
                    },
                    "IDR" => new Dictionary<string, string>
                    {
                        { "User", inputs["QRISUser"] }, { "APIKey", inputs["QRISAPI"] }
                    },
                    "INR" => new Dictionary<string,string>()
                    {
                        { "User", inputs["UPIUser"] }, { "APIKey", inputs["UPIAPI"] }
                    }
                };
            }
            else
            {
                currencyData = PayCurrencyId switch
                {
                    "MYR" => new Dictionary<string, string>
                    {
                        { "User", inputs["dMYRUser"] }, { "APIKey", inputs["dMYRAPI"] }
                    },
                    "THB" => new Dictionary<string, string>
                    {
                        { "User", inputs["dTHBUser"] }, { "APIKey", inputs["dTHBAPI"] }
                    },
                    "IDR" => new Dictionary<string, string>
                    {
                        { "User", inputs["dIDRUser"] }, { "APIKey", inputs["dIDRAPI"] }
                    }
                };
            }
            

            if (isEWallet)
            {
                currencyReq.username = currencyEWalletData["User"];
                authReq.username = currencyEWalletData["User"];
                authReq.api_key = currencyEWalletData["APIKey"]; 
            }
            else
            {
                currencyReq.username = currencyData["User"];
                authReq.username = currencyData["User"];
                authReq.api_key = currencyData["APIKey"];
            }
            
            var sUrl = inputs["host"];
            string sResponse = null;
            oDepositResult.Success = false;
            var amount = decimal.Parse(inputs["Amount"]);
            try
            {
                var dicP = FromJson<Dictionary<string, string>>(ToJson(currencyReq));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                sResponse = HttpPost(sUrl + "merchant/currency", sParam, "application/x-www-form-urlencoded", "0");
                var currencyResp = new CurrencyResp();
                currencyResp = JsonConvert.DeserializeObject<CurrencyResp>(sResponse);

                #region 檢核CurrencyLimit
                if (!currencyResp.status)
                {
                    oDepositResult.Message = "";
                    oDepositResult.RawContent = sResponse;
                    return oDepositResult;
                }
                else if (!currencyResp.rate.ToList().Exists(x => x.currency == PayCurrencyId))
                {
                    oDepositResult.Message = "";
                    oDepositResult.RawContent = sResponse;
                    return oDepositResult;
                }
                else
                {
                    var currencydata = currencyResp.rate.ToList().Find(x => x.currency == PayCurrencyId);
                    if (currencydata.min > amount || currencydata.max < amount)
                    {
                        oDepositResult.Message = "";
                        oDepositResult.RawContent = sResponse;
                        return oDepositResult;
                    }
                }
                #endregion

                dicP = FromJson<Dictionary<string, string>>(ToJson(authReq));
                sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                sResponse = HttpPost(sUrl + "merchant/auth", sParam, "application/x-www-form-urlencoded", "0");

                var authResp = new AuthResp();
                authResp = JsonConvert.DeserializeObject<AuthResp>(sResponse);
                if (authResp.status)
                {
                    var depositReq = new DepositReq();
                    depositReq.username = isEWallet ? currencyEWalletData["User"] :currencyData["User"];
                    depositReq.auth = authResp.auth;
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


        private string GetDebitCallBackSecretKey(string currency, string paymentType)
        {
            return paymentType switch
            {
                "EWallet" => currency switch
                {
                    "MYR" => "DuitNowSecret",
                    "INR" => "UPISecret",
                    _ => "Error - Invalid currency for EWallet"
                },
                "QR" => currency switch
                {
                    "THB" => "PromptPaySecret",
                    "IDR" => "QRISSecret",
                    _ => "Error - Invalid currency for QR"
                },
                "Online Banking" => currency switch
                {
                    "MYR" => "dMYRSecret",
                    "THB" => "dTHBSecret",
                    "IDR" => "dIDRSecret",
                    _ => "Error - Invalid currency for Online Banking"
                },
                _ => "Error - Invalid paymentType"
            };
        }
    }
}
