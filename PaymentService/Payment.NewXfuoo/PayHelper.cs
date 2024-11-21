using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.NewXfuoo
{
    public class PayHelper : PayHelperBase, IExchangeHelper
    {
        internal static string? SignConcatSourceString;
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region ExchangeRate
        public ExchangeResult GetExchange(Dictionary<string, string> inputs)
        {
            try
            {
                var exchangeRateData = GetExchangeRate(inputs);

                var dataRate = decimal.Parse(exchangeRateData.Rate); // 1 USDT = xxx CNY
                var rate = decimal.Parse((1m / dataRate).ToString("F15")); // 1CNY = 0.xxxx USDT
                var amount = decimal.Parse(inputs["Amount"]);
                var toAmount = amount * rate;

                return new ExchangeResult("0", "success", new CryptoExchangeInfo
                {
                    Rate = rate,
                    ExchangeAmount = toAmount,
                    Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                });
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

                return new ExchangeResult(ex.Code, ex.Message);
            }
        }

        #endregion

        #region Debit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var payCode = inputs["PayCode"];
            var amount = decimal.Parse(inputs["Amount"]);
            var depositInfo = inputs["DepositInfo"];

            try
            {
                var exchangeRateData = GetExchangeRate(inputs);
                var rate = 1m / decimal.Parse(exchangeRateData.Rate);
                var requestAmount = (amount * rate).ToString("F2");

                // 檢查 DepositInfo
                if (depositInfo == null)
                {
                    throw new CustomException("DepositInfo is empty.");
                }

                var req = new Dictionary<string, string>
                {
                    {"merchantNo", inputs["MerchantNo"]},
                    {"outTradeNo", inputs["TransID"] },
                    {"type", payCode},
                    {"amount", requestAmount},
                    {"notifyUrl", inputs["CallBackUrl"]},
                    {"lang", "zh"}
                };

                req.Add("sign", GetPostSign(req, inputs["SecretKey"]));
                var url = $"{inputs["Host"]}{Definition.Path.Deposit}";

                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, Response<DepositData>>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"NewXfuoo deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (result != null && result.Status != 0)
                {
                    throw new CustomException($"NewXfuoo deposit failed, code={result.Code}, msg={result.Msg}");
                }

                // 將匯率與回傳支付金額加入 DepositInfo
                depositInfo = depositInfo
                    .AddOrSetExchangeInfo(rate.ToString("F15"), requestAmount);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.Data.PayUrl,
                    DepositInfo = depositInfo
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"NewXfuoo deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }

        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var status = formData["status"];
            var depositId = formData["outTradeNo"];
            var thirdPartyId = formData["sn"];
            var parameters = new Dictionary<string, string>
            {
                {"merchantNo", formData["merchantNo"]},
                {"outTradeNo", depositId },
                {"type", formData["type"]},
                {"amount", formData["amount"]},
                {"status", status},
                {"extra", formData["extra"]},
                {"sn", thirdPartyId},
                {"createTime", formData["createTime"]}
            };

            if (!VerifySign(parameters, payResource["SecretKey"], formData["sign"]))
            {
                throw new CustomException($"{payResource["TransID"]}, signature verification failed.");
            }

            var transStatus = Definition.GetDepositTransStatus(status);
            var result = new NotifyDepositResult
            {
                DepositId = depositId,
                ThirdId = thirdPartyId,
                Comment = status,
                TransStatus = transStatus,
                Response = "success"
            };

            if (transStatus == TransStatus.Success)
            {
                var depositInfo = payResource["DepositInfo"];
                var jObj = JObject.Parse(depositInfo);

                if (!jObj.TryGetValue("ExchangeRate", StringComparison.OrdinalIgnoreCase, out var jExchangeRate)
                    || !jObj.TryGetValue("ExchangeAmount", StringComparison.OrdinalIgnoreCase, out var jExchangeAmount))
                {
                    return new NotifyDepositResult
                    {
                        TransStatus = TransStatus.Pending,
                        Comment = "DepositInfo has no exchange rate data.",
                        Response = "success"
                    };
                }

                var receivedCryptoAmount = decimal.Parse(formData["amount"]);
                var requestAmount = decimal.Parse(payResource["RequestAmount"]);
                var requestCryptoAmount = jExchangeAmount.ToObject<decimal>();
                var exchangeRate = jExchangeRate.ToObject<decimal>();
                result.ActualAmount =
                    GetActualAmount(requestAmount, requestCryptoAmount, receivedCryptoAmount, exchangeRate);
            }

            return result;
        }

        #endregion

        #region Withdrawal
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var withdrawalInfo = inputs["WithdrawalInfo"];

            try
            {
                if (withdrawalInfo == null)
                {
                    throw new CustomException("WithdrawalInfo is empty.");
                }

                var rateInfo = JsonConvert.DeserializeObject<CryptoRateInfo>(withdrawalInfo);

                if (rateInfo == null)
                {
                    throw new CustomException("CryptoRateInfo is empty.");
                }

                var amount = rateInfo.TempExchangeAmount.ToString("F2");
                withdrawalInfo = withdrawalInfo
                    .AddOrSetExchangeInfo(rateInfo.TempExchangeRate.ToString("G29"), amount);

                var req = new Dictionary<string, string>
                {
                    {"merchantNo", inputs["MerchantNo"]},
                    {"outTradeNo", inputs["TransID"] },
                    {"amount", amount },
                    {"type", inputs["PayCode"] },
                    {"address", inputs["AccountNo"] },
                    {"notifyUrl", inputs["CallBackUrl"] },
                };

                req.Add("sign", GetPostSign(req, inputs["SecretKey"]));
                var url = $"{inputs["Host"]}{Definition.Path.Withdrawal}";
                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, Response<WithdrawalData>>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"NewXfuoo withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (result != null && result.Status != 0)
                {
                    throw new CustomException($"NewXfuoo withdrawal failed, code={result.Code}, msg={result.Msg}");
                }

                var response = new WithdrawResult
                {
                    Success = true,
                    WithdrawId = result.Data.TradeId,
                    RawContent = JsonConvert.SerializeObject(result),
                    WithdrawalInfo = withdrawalInfo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"NewXfuoo Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var status = formData["status"];
            var withdrawalId = formData["outTradeNo"];
            var thirdPartyId = formData["sn"];
            var parameters = new Dictionary<string, string>
            {
                {"merchantNo", formData["merchantNo"]},
                {"outTradeNo", withdrawalId },
                {"amount", formData["amount"]},
                {"status", status},
                {"extra", formData["extra"]},
                {"sn", thirdPartyId}
            };

            if (!VerifySign(parameters, payResource["SecretKey"], formData["sign"]))
            {
                throw new CustomException($"{payResource["TransID"]}, signature verification failed.");
            }

            var transStatus = Definition.GetWithdrawalTransStatus(status);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = withdrawalId,
                ThirdId = thirdPartyId,
                TransStatus = transStatus,
                Response = "success"
            };

            return result;
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


        #region Private Methods
        internal string GetPostSign(Dictionary<string, string> parameters, string secretKey)
        {
            var sortedParameters = parameters.OrderBy(p => p.Key);
            var signSourceParams = string.Join("&",
                sortedParameters
                    .Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase)
                                && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"{x.Key}={x.Value}"));
            SignConcatSourceString = $"{signSourceParams}&signKey={secretKey}";
            _logger.LogInformation(SignConcatSourceString);

            var sign = ToMD5(SignConcatSourceString).ToUpper();
            _logger.LogInformation(sign);
            return sign;
        }

        private bool VerifySign(Dictionary<string, string> parameters, string secretKey, string target)
        {
            return target.Equals(GetPostSign(parameters, secretKey), StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetActualAmount(decimal requestAmount,
            decimal requestCryptoAmount,
            decimal receivedCryptoAmount,
            decimal exchangeRate)
        {
            if (receivedCryptoAmount == requestCryptoAmount)
                return requestAmount;

            // 使用匯率回推平台金額，保留小數 2 位
            var actualAmount = decimal.Round(receivedCryptoAmount / exchangeRate, 2);
            return actualAmount;
        }

        private ExchangeRateData GetExchangeRate(Dictionary<string, string> inputs)
        {
            var req = new Dictionary<string, string>
            {
                {"merchantNo", inputs["MerchantNo"]},
                {"fiatCurrency", inputs["PayCurrencyId"] }
            };

            req.Add("sign", GetPostSign(req, inputs["SecretKey"]));
            var url = $"{inputs["Host"]}{Definition.Path.GetExchangeRate}";

            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, Response<ExchangeRateData>>(url, req);
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"NewXfuoo GetExchangeRate failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result != null && result.Status != 0)
            {
                throw new CustomException($"NewXfuoo GetExchangeRate failed, code={result.Code}, msg={result.Msg}");
            }

            return result.Data;
        }

        #endregion
    }
}