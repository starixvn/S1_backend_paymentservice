using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.MangoPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }
        private class CallBackDebitReq
        {
            public int merchantId { get; set; }
            public string orderSn { get; set; }
            public string queryId { get; set; }
            public string goodsName { get; set; }
            public decimal amount { get; set; }
            public int status { get; set; }
            public decimal realMoney { get; set; }
            public string message { get; set; }
            public string sign { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(bodyContent);

            var p = $"{data.merchantId}{data.orderSn}{data.amount}{payResource["key"]}";
            var sign = ToMD5(p).ToLower();

            oNotify.DepositId = data.orderSn;
            oNotify.ThirdId = data.queryId;
            oNotify.Message = data.message;

            if (sign == data.sign)
            {
                if (data.status == 2 || data.status == 3)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = data.amount / 100;
                    oNotify.ActualAmount = data.amount / 100;
                    oNotify.Message = data.message;
                    oNotify.Comment = $"{data.status}-{data.message}";
                    oNotify.Response = "success";
                }
                else if (data.status == 1)
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Message = data.message;
                    oNotify.Comment = $"{data.status}-{data.message}";
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Message = data.message;
                    oNotify.Comment = $"{data.status}-{data.message}";
                    oNotify.Response = "success";
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

        private class WithdrawalRes
        {
            public long code { get; set; }

            public string message { get; set; }            
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var formData = new Dictionary<string, string>
            {
                { "merchantId", inputs["merchant_no"] },
                { "orderSn", inputs["TransID"] },
                { "currency", inputs["PayCurrencyId"] }
            };

            var amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            formData.Add("type", "bank");
            formData.Add("userName", inputs["AccountName"]);
            formData.Add("cardNo", inputs["AccountNo"]);
            formData.Add("bankCode", inputs["BankCode"]);
            formData.Add("bankName", inputs["BankCode"]);
            formData.Add("bankBranch", inputs["BankCode"]);
            formData.Add("branchAddress", inputs["BankCode"]);
            formData.Add("province", inputs["BankCode"]);
            formData.Add("city", inputs["BankCode"]);
            formData.Add("unionCode", inputs["BankCode"]);
            formData.Add("bankMobile", inputs["BankCode"]);

            var p = $"{formData["merchantId"]}{formData["orderSn"]}{formData["currency"]}{formData["amount"]}{formData["type"]}{formData["userName"]}{formData["cardNo"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderSn"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.code == 200)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.message;
                withdrawResult.RawContent = withdrawalRes.message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.message;
                withdrawResult.RawContent = withdrawalRes.message;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string code { get; set; }
            public string message { get; set; }
            public DepositResData data { get; set; }
        }

        public class DepositResData
        {
            public string gopayUrl { get; set; }
            public int type { get; set; }
            public string qrCode { get; set; }

        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = "";
            var formData = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(inputs["PayCode"]))
            {
                sUrl = inputs["host"];
                formData.Add("type", "2");
                formData.Add("scanType", inputs["PayCode"]);
            }
            else
            {
                sUrl = inputs["hostgateway"];
                formData.Add("type", "1");
            }

            formData.Add("merchantId", inputs["merchant_no"]);
            formData.Add("orderSn", inputs["TransID"]);
            formData.Add("currency", inputs["PayCurrencyId"]);
            var amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            formData.Add("returnUrl", inputs["ReturnUrl"]);
            formData.Add("notifyUrl", inputs["CallBackUrl"]);
            formData.Add("language", inputs["PayLangId"]);
            formData.Add("goodsName", "N/A");
            
            var p = $"{formData["merchantId"]}{formData["orderSn"]}{formData["currency"]}{formData["amount"]}{formData["notifyUrl"]}{formData["type"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderSn"]);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.code == "200")
            {
                if (depositRes.data.type == 1)
                {
                    oDepositResult.Url = depositRes.data.gopayUrl;
                    oDepositResult.Success = true;
                    oDepositResult.FormData = null;
                    oDepositResult.Message = "";
                }
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.message;
            }

            return oDepositResult;
        }

        private class EnquiryWithdrawRes
        {
            public string code { get; set; }
            public string message { get; set; }
            public EnquiryWithdrawResData data { get; set; }
        }

        public class EnquiryWithdrawResData
        {
            public string orderSn { get; set; }
            public string upOrderSn { get; set; }
            public string amount { get; set; }
            public string status { get; set; }

        }

        Dictionary<string, string> payCurrencyIdMapping = new Dictionary<string, string>()
        {
            { "4","5"}
        };

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOutQuery"];
            var formData = new Dictionary<string, string>
            {
                { "merchantId", inputs["merchant_no"] },
                { "orderSn", inputs["TransID"] },
                { "currency", payCurrencyIdMapping[inputs["CurrencyId"]] }
            };
            var amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            formData.Add("type", "bank");

            var p = $"{formData["merchantId"]}{formData["orderSn"]}{formData["currency"]}{formData["amount"]}{formData["type"]}{inputs["key"]}";
            formData.Add("sign", ToMD5(p).ToLower());

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", formData["orderSn"]);

            var enquiryWithdrawRes = new EnquiryWithdrawRes();
            enquiryWithdrawRes = JsonConvert.DeserializeObject<EnquiryWithdrawRes>(sResponse);
            var oNotify = new EnquiryWithdrawResult();
            if (enquiryWithdrawRes.code == "200")
            {
                if (enquiryWithdrawRes.data.status == "2" || enquiryWithdrawRes.data.status == "3")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(enquiryWithdrawRes.data.amount) / 100;
                    oNotify.ThirdId = enquiryWithdrawRes.data.upOrderSn;
                }
                else if (enquiryWithdrawRes.data.status == "4")
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = enquiryWithdrawRes.data.upOrderSn;
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                }
            }
            else if(enquiryWithdrawRes.code == "201")
            {
                oNotify.TransStatus = TransStatus.Fail;
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
            }
            
            oNotify.Message = enquiryWithdrawRes.message;
            return oNotify;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
