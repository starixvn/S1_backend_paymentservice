using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Helpers;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.BixinPay
{
    public class PayHelper : PayHelperBase
    {
        internal static string? SignConcatSourceString;
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }
        #region Debit

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["Host"]}{Definition.Path.Deposit}";
            var amount = decimal.Parse(inputs["Amount"]).ToString("F");
            var dateTimeUtc08 = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0));
            var requestTime = dateTimeUtc08.ToString("yyyyMMddHHmmss");
            var payType = inputs["PayCode"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchno", inputs["MerchNo"]},
                    {"orderId", inputs["TransID"]},
                    {"payType", payType},
                    {"amount", amount},
                    {"requestCurrency", inputs["PayCurrencyId"]},
                    {"asyncUrl", inputs["CallBackUrl"]},
                    {"syncUrl", inputs["ReturnUrl"]},
                    {"requestTime", requestTime},
                    {"apiVersion", "2"},
                    {"attach", payType == "6" ? inputs["AccountName"] : string.Empty}
                };

                req.Add("sign", GetPostSign(req, inputs["SecretKey"]));

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = url,
                    FormData = req
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"BixinPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var receiveSign = formData["sign"];
            var depositId = formData["orderId"];
            var status = formData["status"];
            var amount = decimal.Parse(formData["amount"]);

            if (!VerifySign(formData, payResource["SecretKey"], receiveSign))
            {
                return new NotifyDepositResult
                {
                    DepositId = formData["merchantOrderId"],
                    TransStatus = TransStatus.Pending,
                    Response = $"TransId: {depositId}, Signature verification failed."
                };
            }

            var transStatus = Definition.GetDepositTransStatus(status);
            return new NotifyDepositResult
            {
                DepositId = depositId,
                ActualAmount = amount,
                ThirdId = formData["orderId"],
                Comment = status,
                TransStatus = transStatus,
                Response = CallbackResponse.Success
            };
        }
        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["Host"]}{Definition.Path.Withdrawal}";
            var amount = decimal.Parse(inputs["Amount"]).ToString("F");
            var dateTimeUtc08 = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0));
            var reqDateTime = dateTimeUtc08.ToString("yyyyMMddHHmmss");
            var tradeType = inputs["PayCode"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchno", inputs["MerchantNo"]},
                    {"orderId", inputs["TransID"]},
                    {"amount", amount},
                    {"account", inputs["AccountName"]},
                    {"tradeType", tradeType},
                    {"cardNo", inputs["AccountNo"]},
                    {"bankName", inputs["BankName"] },
                    {"depositBank", "开户支行"},
                    {"asyncUrl", inputs["CallBackUrl"]},
                    {"timestamp", reqDateTime}
                };

                var secretKey = inputs["SecretKey"];
                var privateKey = inputs["PrivateKey"];
                req.Add("sign", GetWithdrawalPostSign(req, secretKey, privateKey));

                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"BixinPay Withdrawal failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && result.ResponseContent.Code != "0")
                {
                    throw new CustomException($"BixinPay Withdrawal failed, Code: {result.ResponseContent.Code}");
                }

                if (result.ResponseContent.Status != "0" || result.ResponseContent.Status != "1")
                {
                    throw new CustomException(
                        $"BixinPay Withdrawal failed, [Response Info] WithdrawId={result.ResponseContent.OrderId}, code={result.ResponseContent.Code}, msg={result.ResponseContent.Msg}, status={result.ResponseContent.Status}");
                }

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = result.ResponseContent.OrderId,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"BixinPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var status = formData["status"];
            var receiveSign = formData["sign"];
            var publicKey = payResource["PublicKey"];
            var secretKey = payResource["SecretKey"];
            var withdrawalId = formData["orderId"];

            if (!VerifyWithdrawalSign(formData, secretKey, publicKey, receiveSign))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = withdrawalId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{withdrawalId}, Signature does not match."
                };
            }

            return new NotifyWithdrawResult
            {
                WithdrawId = withdrawalId,
                ThirdId = formData["orderNo"],
                TransStatus = Definition.GetWithdrawalTransStatus(status),
                Response = CallbackResponse.Success
            };
        }

        #endregion

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

        internal string GetPostSign(Dictionary<string, string> dic, string secretKey)
        {
            var sortedParameters = dic.OrderBy(p => p.Key);
            var signSourceParams = string.Join("&",
                sortedParameters
                    .Where(x => !string.IsNullOrEmpty(x.Value)
                                && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"{x.Key}={x.Value}"));
            SignConcatSourceString = $"{signSourceParams}&secretKey={secretKey}";
            _logger.LogInformation($"SignSourceString: {SignConcatSourceString}");
            var sign = ToMD5(SignConcatSourceString).ToLower();
            return sign;
        }

        internal string GetWithdrawalPostSign(Dictionary<string, string> req, string secretKey, string privateKey)
        {
            var md5Result = GetPostSign(req, secretKey);
            return RsaSignPkcs8PrivateKey(md5Result, privateKey, HashAlgorithmName.SHA256);
        }

        internal bool VerifyWithdrawalSign(Dictionary<string, string> formData, string secretKey, string publicKey, string receiveSign)
        {
            var md5Result = GetPostSign(formData, secretKey);
            return RsaHelper.RsaVerifyData(md5Result, receiveSign, publicKey, HashAlgorithmName.SHA256);
        }

        private bool VerifySign(Dictionary<string, string> formData, string secretKey, string receiveSign)
        {
            var md5Result = GetPostSign(formData, secretKey);
            return receiveSign.Equals(md5Result, StringComparison.OrdinalIgnoreCase);
        }
    }

}