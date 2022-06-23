using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Payment.CoPay.Models;
using PaymentCore;

namespace Payment.CoPay
{
    public class PayHelper : PayHelperBase
    {
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var amount = decimal.Parse(inputs["Amount"]);
            var formData = new Dictionary<string, string>()
            {
                { "uid", inputs["MerchantId"] },
                { "orderid", inputs["TransID"] },
                { "channel", inputs["PayCode"] },
                { "notify_url", inputs["CallBackUrl"] },
                { "return_url", inputs["ReturnUrl"] },
                { "amount", amount.ToString("#") },
                { "userip", inputs["ClientIP"] },
                { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "custom", "" }
            };
            formData.Add("sign", Sign(formData, inputs["key"]));
            
            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"]);
            
            
            var depositRes = JsonConvert.DeserializeObject<PayResponse>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.status == 10000)
            {
                oDepositResult.ThirdId = depositRes.result.transactionid;
                oDepositResult.Url = depositRes.result.payurl;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = depositRes.status.ToString();
            }

            return oDepositResult;
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var amount = decimal.Parse(inputs["Amount"]);
            var formData = new Dictionary<string, string>()
            {
                { "uid", inputs["MerchantId"] },
                { "orderid", inputs["TransID"] },
                { "channel", "712" },
                { "notify_url", inputs["CallBackUrl"] },
                { "amount", amount.ToString("#") },
                { "userip", inputs["ClientIP"] },
                { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "custom", "" },
                { "bank_account", inputs["AccountName"] },
                { "bank_no", inputs["AccountNo"] },
                { "bank_id", inputs["BankCode"] },
                { "bank_province", "" },
                { "bank_city", "" },
                { "bank_sub", "" },
            };
            formData.Add("sign", Sign(formData, inputs["key"]));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"]);
            
            var withdrawResult = new WithdrawResult();
            var withdrawalRes = JsonConvert.DeserializeObject<PayoutResponse>(sResponse);

            withdrawResult.Success = withdrawalRes.status == 10000;
            withdrawResult.Message = withdrawalRes.status.ToString();

            return withdrawResult;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var expectedSign = formData["sign"];
            formData.Remove("sign");
            var actualSign = Sign(formData, payResource["key"]);
            
            if (actualSign == expectedSign)
            {
                var result = FromJson<CallBackDebitResult>(formData["result"]);
                var status = formData["status"];
                if (status == "10000" && result != null)
                {
                    oNotify.DepositId = result.orderid;
                    oNotify.ThirdId = result.transactionid.ToString();
                    oNotify.Message = status;
                    oNotify.Comment = status;
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = result.amount;
                    oNotify.ActualAmount = result.real_amount;

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
                oNotify.Response = "Signature error";
            }

            return oNotify;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var expectedSign = formData["sign"];
            formData.Remove("sign");
            var actualSign = Sign(formData, payResource["key"]);

            var oNotify = new NotifyWithdrawResult();
            if (actualSign == expectedSign)
            {
                var result = FromJson<CallBackPayoutResult>(formData["result"]);
                var status = formData["status"];
                if (status == "10000" && result != null)
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = result.real_amount;
                    oNotify.WithdrawId = result.orderid;
                    oNotify.ThirdId = result.transactionid.ToString();
                    oNotify.Message = status;
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

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new System.NotImplementedException();
        }

        public string Sign(Dictionary<string, string> para, string key)
        {
            var properties = para.OrderBy(a => a.Key)
                .Select(p => p.Key + "=" + p.Value);
            var queryString = string.Join("&", properties.ToArray());
            return ToMD5($"{queryString}&key={key}").ToUpper();
        }
    }
}