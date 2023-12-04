using Newtonsoft.Json;

using Payment.CoPay.Models;

using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.CoPay
{
    public class PayHelper : PayHelperBase
    {
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var amount = decimal.Parse(inputs["Amount"]);
            var payCode = int.Parse(inputs["PayCode"]);

            var channel = CoPayChannel.BankPayScan;
            if (Enum.IsDefined(typeof(CoPayChannel), payCode))
                channel = (CoPayChannel)payCode;

            var formData = new Dictionary<string, string>()
            {
                { "uid", inputs["MerchantId"] },
                { "orderid", inputs["TransID"] },
                { "channel", ((int)channel).ToString() },
                { "notify_url", inputs["CallBackUrl"] },
                { "return_url", inputs["ReturnUrl"] },
                { "amount", amount.ToString("#") },
                { "userip", inputs["ClientIP"] },
                { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "custom", "" },
            };
            if (channel == CoPayChannel.OnlineBanking || channel == CoPayChannel.BankPayScan)
            {
                var bankId = string.IsNullOrEmpty(inputs["BankCode"]) ? inputs["PayCode"] : inputs["BankCode"];
                formData.Add("bank_id", bankId);
            }
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
            var channel = (int)CoPayChannel.VndBankPayout;
            var formData = new Dictionary<string, string>()
            {
                { "uid", inputs["MerchantId"] },
                { "orderid", inputs["TransID"] },
                { "channel", channel.ToString() },
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

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var expectedSign = formData["sign"];
            formData.Remove("sign");
            var actualSign = Sign(formData, payResource["key"]);

            if (actualSign == expectedSign)
            {
                var status = formData["status"];
                var result = FromJson<CallBackDebitResult>(formData["result"]);
                oNotify.DepositId = result.orderid;
                oNotify.Message = status;
                oNotify.Comment = status;
                if (status == "10000")
                {
                    oNotify.ThirdId = result.transactionid.ToString();
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = result.amount;
                    oNotify.ActualAmount = result.amount;
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var expectedSign = formData["sign"];
            formData.Remove("sign");
            var actualSign = Sign(formData, payResource["key"]);

            var oNotify = new NotifyWithdrawResult();
            if (actualSign == expectedSign)
            {
                var status = formData["status"];
                var result = FromJson<CallBackPayoutResult>(formData["result"]);
                oNotify.WithdrawId = result.orderid;
                oNotify.Message = status;
                if (status == "10000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = result.real_amount;
                    oNotify.ThirdId = result.transactionid.ToString();
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