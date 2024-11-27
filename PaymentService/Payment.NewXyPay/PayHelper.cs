using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.NewXyPay
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

        /// <summary>
        /// 沒提供匯率
        /// </summary>
        public ExchangeResult GetExchange(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["Host"]}{Definition.Path.Deposit}";
            var payType = GetPayType(inputs["PayCode"]);
            var usdtType = GetUsdtType(inputs["PayCode"]);
            try
            {
                var amount = decimal.Parse(inputs["Amount"]).ToString("F2");
                if (IsCrypto(usdtType))
                {
                    var depositInfo = inputs["DepositInfo"];
                    if (depositInfo == null)
                    {
                        throw new CustomException("DepositInfo is empty.");
                    }

                    var rateInfo = JsonConvert.DeserializeObject<CryptoRateInfo>(depositInfo);
                    if (rateInfo == null)
                    {
                        throw new CustomException("CryptoRateInfo is empty.");
                    }

                    amount = rateInfo.ExchangeAmount.ToString("F2");
                }

                var req = new Dictionary<string, string>
                {
                    {"storeCode", inputs["StoreCode"]},
                    {"storeOrderNo", inputs["TransID"]},
                    {"payType", payType},
                    {"totalAmt", amount },
                    {"backUrl", inputs["ReturnUrl"]},
                    {"notifyUrl", inputs["CallBackUrl"]},
                    {"playerName", inputs["AccountName"]},
                    {"usdtType",usdtType}
                };

                req.Add("sign", GetPostSign(req, inputs["SecretKey"]));
                var (result, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"NewXyPay deposit failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && !string.IsNullOrEmpty(result.ErrorCode))
                {
                    throw new CustomException($"NewXyPay deposit failed, errorCode={result.ErrorCode} errorMsg={result.ErrorMsg})");
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.PayUrl,
                    ThirdId = result.OrderNo
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"NewXyPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var secretKey = payResource["SecretKey"];
            var receiveModel = FromJson<ReceiveModel>(bodyContent);
            var dic = FromJson<Dictionary<string, string>>(bodyContent);

            var depositId = receiveModel.StoreOrderNo;
            if (!VerifySign(dic, secretKey, receiveModel.Sign))
            {
                return new NotifyDepositResult
                {
                    DepositId = depositId,
                    TransStatus = TransStatus.Pending,
                    Response = $"TransId: {depositId}, Signature verification failed."
                };
            }

            var transStatus = Definition.GetTransStatus(receiveModel.PayResult);

            return new NotifyDepositResult
            {
                DepositId = depositId,
                ActualAmount = decimal.Parse(receiveModel.TotalAmt),
                ThirdId = receiveModel.OrderNo,
                Comment = receiveModel.PayResult,
                TransStatus = transStatus,
                Response = CallbackResponse.Success
            };
        }



        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var withdrawalInfo = inputs["WithdrawalInfo"];
            var payCode = inputs["PayCode"];

            try
            {
                Dictionary<string, string> req;
                if (IsCrypto(payCode))
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

                    req = new Dictionary<string, string>
                    {
                        {"accountNo", inputs["AccountNo"]},
                        {"transAmt", inputs["amount"]},
                        {"usdtType", payCode}
                    };
                }
                else
                {
                    req = new Dictionary<string, string>
                    {
                        {"accountName", inputs["AccountName"]},
                        {"AccountNo", inputs["AccountNo"]},
                        {"transAmt", decimal.Parse(inputs["Amount"]).ToString("F2")},
                        {"bankCode", inputs["BankCode"]},
                        {"bankBranchName", "bankBranchName"},
                        {"bankAccountType" , "1"},
                        {"province" , "province"},
                        {"city" , "city"},
                        {"userIdCard" , "userIdCard"},
                        {"userMobile" , "userMobile"}
                    };
                }

                req.Add("storeCode", inputs["StoreCode"]);
                req.Add("storeOrderNo", inputs["TransID"]);
                req.Add("notifyUrl", inputs["CallBackUrl"]);
                req.Add("sign", GetPostSign(req, inputs["SecretKey"]));
                var url = $"{inputs["Host"]}{Definition.Path.Withdrawal}";
                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, PaymentResponse> (url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"NewXyPay withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (result != null && !string.IsNullOrEmpty(result.ErrorCode))
                {
                    throw new CustomException($"NewXyPay withdrawal failed, errorCode={result.ErrorCode}, errorMsg={result.ErrorMsg}");
                }

                var response = new WithdrawResult
                {
                    Success = true,
                    WithdrawId = result.StoreOrderNo,
                    RawContent = JsonConvert.SerializeObject(result),
                    WithdrawalInfo = withdrawalInfo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"NewXyPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var secretKey = payResource["SecretKey"];
            var receiveModel = FromJson<ReceiveModel>(bodyContent);
            var dic = FromJson<Dictionary<string, string>>(bodyContent);

            var withdrawalId = receiveModel.StoreOrderNo;
            if (!VerifySign(dic, secretKey, receiveModel.Sign))
            {
                throw new CustomException($"{withdrawalId}, signature verification failed.");
            }

            var transStatus = Definition.GetTransStatus(receiveModel.PayResult);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = withdrawalId,
                ThirdId = receiveModel.OrderNo,
                TransStatus = transStatus,
                Response = CallbackResponse.Success
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

        internal string GetPostSign(Dictionary<string, string> dic, string secretKey)
        {
            SignConcatSourceString = string.Join('&', dic
                    .Where(x => !string.IsNullOrEmpty(x.Value)) // 過濾掉 null 或空字串的項目
                    .OrderBy(x => x.Key, StringComparer.Ordinal) // 使用 ASCII 排序
                    .Select(x => $"{x.Key}={x.Value}")) + $"&key={secretKey}";
            _logger.LogInformation($"SignSourceString: {SignConcatSourceString}");
            var sign = ToMD5(SignConcatSourceString).ToUpper();
            return sign;
        }

        private bool VerifySign(Dictionary<string, string> dic, string secretKey, string receiveSign)
        {
            return receiveSign.Equals(GetPostSign(dic, secretKey), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCrypto(string target)
        {
            return !string.IsNullOrEmpty(target);
        }

        private static string GetPayType(string payCode)
        {
            var result = payCode.Split(':');

            return result[0];
        }

        private static string GetUsdtType(string payCode)
        {
            var result = payCode.Split(':');

            return result.Length > 1 ? result[1] : string.Empty;
        }
    }
}