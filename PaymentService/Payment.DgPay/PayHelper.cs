using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DgPay
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
            var url = $"{inputs["APIUrl"]}{Definition.Path.Deposit}";
            // PayCode format : {Provider Id}-{Provider Type}
            var provider = inputs["PayCode"].Split('-');
            var providerId = provider[0];
            var providerType = provider[1];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"orderId", inputs["TransID"]},
                    {"providerId", providerId},
                    {"providerType", providerType},
                    {"currency", inputs["PayCurrencyId"] },
                    {"amount ", inputs["Amount"]},
                    {"reqDateTime", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                    {"opCode", inputs["MerchantCode"]}
                };

                req.Add("securityToken", GetSign(req, inputs["SecretKey"]));
                req.Add("callbackUrl", inputs["CallBackUrl"]);

                // '5-10' OnlineBanking
                // '5-20' (QR)
                if (providerType.Equals("10"))
                {
                    var data = new
                    {
                        fromBankCode = inputs["BankCode"],
                        redirectUrl = inputs["ReturnUrl"]
                    };
                    req.Add("data", ToJson(data));
                }
                else
                {
                    var data = new
                    {
                        redirectUrl = inputs["ReturnUrl"]
                    };
                    req.Add("data", ToJson(data));
                }

                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"DgPay deposit failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && result.Code != "0")
                {
                    throw new CustomException(result.Description);
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.PaymentUrl,
                    ThirdId = result.RefId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"DgPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var depositId = formData["orderId"];
            var amount = decimal.Parse(formData["amount"]);
            var status = formData["status"];

            var transStatus = Definition.GetTransStatus(status);

            var responseData = new
            {
                code = "0",
                description = "Success"
            };

            return new NotifyDepositResult
            {
                DepositId = depositId,
                ActualAmount = amount,
                ThirdId = depositId,
                Comment = status,
                TransStatus = transStatus,
                Response = ToJson(responseData)
            };
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["APIUrl"]}{Definition.Path.Withdrawal}";
            // PayCode format : {Provider Id}-{Provider Type}
            var provider = inputs["PayCode"].Split('-');
            var providerId = provider[0];
            var providerType = provider[1];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"orderId", inputs["TransID"]},
                    {"providerId", providerId},
                    {"providerType", providerType},
                    {"currency", inputs["PayCurrencyId"] },
                    {"amount ", inputs["Amount"]},
                    {"reqDateTime", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")},
                    {"opCode", inputs["MerchantCode"]}
                };

                req.Add("securityToken", GetSign(req, inputs["SecretKey"]));
                req.Add("callbackUrl", inputs["CallBackUrl"]);

                // '5-10'  是自动代付
                // '10-10' 是手动代付
                if (providerId.Equals("5"))
                {
                    var data = new
                    {
                        toBankCode = inputs["BankCode"],
                        toAccNo = inputs["AccountNo"],
                        toAccName = inputs["AccountName"]
                    };
                    req.Add("data", ToJson(data));
                }
                else
                {
                    var data = new
                    {
                        bankName = inputs["BankName"],
                        accNo = inputs["AccountNo"],
                        accName = inputs["AccountName"]
                    };
                    req.Add("data", ToJson(data));
                }

                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"DgPay Withdrawal failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && result.Code != "0")
                {
                    throw new CustomException(result.Description);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"DgPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var withdrawalId = formData["orderId"];
            var status = formData["status"];

            var responseData = new
            {
                code = "0",
                description = "Success"
            };

            return new NotifyWithdrawResult
            {
                WithdrawId = withdrawalId,
                ThirdId = withdrawalId,
                TransStatus = Definition.GetTransStatus(status),
                Response = ToJson(responseData)
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

        internal string GetSign(Dictionary<string, string> dic, string secretKey)
        {
            var queryString = string.Join("", dic.Select(x => x.Value).ToArray());
            SignConcatSourceString = queryString + secretKey;
            _logger.LogInformation($"SignSourceString: {SignConcatSourceString}");
            var sign = ToMD5(SignConcatSourceString).ToLower();
            return sign;
        }
    }

}