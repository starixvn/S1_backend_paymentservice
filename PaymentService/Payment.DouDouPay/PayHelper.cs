using Flurl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DouDouPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region model

        #region Debit
        public class DebitRequest
        {
            public string mchCode { get; set; }
            public string orderId { get; set; }
            public decimal amount { get; set; }
            public string paymode { get; set; }
            public string notifyUrl { get; set; }
            public string user { get; set; }
            public string payerName { get; set; }
            public string sign { get; set; }
        }
        public class DebitMD5Data
        {
            public string mchCode { get; set; }
            public string orderId { get; set; }
            public int amount { get; set; }
            public string paymode { get; set; }
            public string notifyUrl { get; set; }
        }
        public class DebitResp
        {
            public int retcode { get; set; }
            public string retdesc { get; set; }
            public decimal amount { get; set; }
            public decimal payAmount { get; set; }
            public string payUrl { get; set; }
        }
        public class CallBackDebitMD5Data
        {
            public string mchCode { get; set; }
            public string orderId { get; set; }
            public int amount { get; set; }
        }
        #endregion

        #region Credit
        public class CreditMD5Data
        {
            public string mchCode { get; set; }
            public string orderId { get; set; }
            public int amount { get; set; }
            public string cardId { get; set; }
            public string accountName { get; set; }
            public string notifyUrl { get; set; }
        }

        public class CreditResp
        {
            public int retcode { get; set; }
            public string retdesc { get; set; }
            public int amount { get; set; }
            public int payAmount { get; set; }
        }
        public class CallBackCreditMD5Data
        {
            public string mchCode { get; set; }
            public string orderId { get; set; }
            public int amount { get; set; }
        }
        #endregion

        #endregion

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var oDepositResult = new DepositResult();
            var toMD5Data = new DebitMD5Data()
            {
                mchCode = inputs["merchantNo"],
                orderId = inputs["TransID"],
                amount = int.Parse(inputs["Amount"]),
                paymode = "OTC",
                notifyUrl = inputs["CallBackUrl"]
            };
            var p = JsonConvert.SerializeObject(toMD5Data) + inputs["key"];
            var sign = ToMD5(p);
            var sUrl = $"{inputs["host"]}/gateway/pay/deposit";

            var req = new Dictionary<string, string>
            {
                {"mchCode", inputs["merchantNo"]},
                {"orderId", inputs["TransID"]},
                {"amount", inputs["Amount"]},
                {"paymode", "OTC"},
                {"notifyUrl", inputs["CallBackUrl"]},
                {"user", inputs["UserName"]},
                {"payerName", inputs["AccountName"]},
                {"sign", sign}
            };

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", "application/json")
                .PostJson<Dictionary<string, string>, DebitResp>(sUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                oDepositResult.Success = false;
                return oDepositResult;
            }

            if (result.retcode == 0)
            {
                oDepositResult.Success = true;
                oDepositResult.Message = result.retdesc;
                oDepositResult.Url = result.payUrl;
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.Message = result.retdesc;
            }
            return oDepositResult;
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var oWithdrawResult = new WithdrawResult();

            var toMD5Data = new CreditMD5Data()
            {
                mchCode = inputs["merchantNo"],
                orderId = inputs["TransID"],
                amount = int.Parse(inputs["Amount"]),
                cardId = inputs["AccountNo"],
                accountName = inputs["AccountName"],
                notifyUrl = inputs["CallBackUrl"]
            };

            var p = JsonConvert.SerializeObject(toMD5Data) + inputs["key"];
            var sign = ToMD5(p);
            var sUrl = $"{inputs["host"]}/gateway/pay/withdraw";

            var req = new Dictionary<string, string>
            {
                {"mchCode", inputs["merchantNo"]},
                {"orderId", inputs["TransID"]},
                {"amount", inputs["Amount"]},
                {"currency", inputs["PayCurrencyId"]},
                {"cardId", inputs["AccountNo"]},
                {"accountName", inputs["AccountName"]},
                {"bankName", inputs["BankName"]},
                {"notifyUrl", inputs["CallBackUrl"]},
                {"sign", sign}
            };

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", "application/json")
                .PostJson<Dictionary<string, string>, CreditResp>(sUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                oWithdrawResult.Success = false;
                return oWithdrawResult;
            }

            if (result.retcode == 0)
            {
                oWithdrawResult.Success = true;
                oWithdrawResult.Message = result.retdesc;
            }
            else
            {
                oWithdrawResult.Success = false;
                oWithdrawResult.Message = result.retdesc;
            }

            return oWithdrawResult;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult()
            {
                DepositId = formData["orderId"]
            };
            var toMD5Data = new CallBackDebitMD5Data()
            {
                mchCode = formData["mchCode"],
                orderId = formData["orderId"],
                amount = int.Parse(formData["amount"])
            };
            var p = JsonConvert.SerializeObject(toMD5Data) + payResource["key"];
            var sign = ToMD5(p);

            if (sign == formData["sign"])
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(formData["amount"]);
                oNotify.ActualAmount = decimal.Parse(formData["payAmount"]);
                oNotify.Response = "success";
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{formData["sign"]}");
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "sign error";
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyWithdrawResult()
            {
                WithdrawId = formData["orderId"]
            };

            var toMD5Data = new CallBackCreditMD5Data()
            {
                mchCode = formData["mchCode"],
                orderId = formData["orderId"],
                amount = int.Parse(formData["amount"])
            };

            var p = JsonConvert.SerializeObject(toMD5Data) + payResource["key"];
            var sign = ToMD5(p);

            if (sign == formData["sign"])
            {
                oNotify.TransStatus = int.Parse(formData["status"]) == 0 ? TransStatus.Success : TransStatus.Fail;
                oNotify.Response = int.Parse(formData["status"]) == 0 ? "success" : "fail";
            }
            else
            {
                _logger.LogInformation($"sign error, sign:{sign} data:{formData["sign"]}");
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "sign error";
                oNotify.Response = "sign error";
            }

            return oNotify;
        }
    }
}