using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Helpers;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.Coin2Pay
{
    class PayHelper : PayHelperBase, IExchangeHelper, IPayHelperNoCallBackUrl, IVerifyTransaction
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region ExchangeRate

        /// <summary>
        /// 這家沒提供匯率，我問號
        /// </summary>
        public ExchangeResult GetExchange(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var payCode = input["PayCode"];
            var reqAmount = input["Amount"];
            var depositInfo = input["DepositInfo"];

            try
            {
                var addr = GetWalletAddress(input);
                var (token, _) = PayCode2TokenInfo(payCode);
                var obj = JObject.Parse(depositInfo);

                if (!obj.TryGetValue("ExchangeRate", StringComparison.OrdinalIgnoreCase, out var jExchangeRate)
                    || !obj.TryGetValue("ExchangeAmount", StringComparison.OrdinalIgnoreCase, out var jExchangeAmount))
                {
                    throw new CustomException("No exchange info.");
                }

                var formData = new Dictionary<string, string>
                {
                    { "DepositAmount", reqAmount },
                    { "Rate", jExchangeRate.ToObject<string>() },
                    { "RequestAmount", jExchangeAmount.ToObject<string>() },
                    { "Token", token }
                };

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = addr.Address,
                    ThirdId = string.Empty,
                    FormData = formData,
                    DepositInfo = depositInfo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var mPublicKey = input["key"];
            var dic = FromJson<Dictionary<string, string>>(bodyContent);
            var transId = dic["uid"];

            var str =
                $"{transId}{dic["txid"]}{dic["address"]}{dic["token"]}{dic["quantity"]}{dic["amount_in_usdt"]}{dic["deposit_fee"]}{dic["given_Credit"]}{dic["credit_Rate"]}{dic["timestamp"]}{dic["currency"]}";

            if (!VerifySign(str, dic["sign"], mPublicKey))
            {
                _logger.LogInformation($"Coin2Pay {transId}, signature verification failed.");

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            var depositInfo = input["DepositInfo"];
            var jObj = JObject.Parse(depositInfo);

            if (!jObj.TryGetValue("ExchangeRate", StringComparison.OrdinalIgnoreCase, out var jExchangeRate)
                || !jObj.TryGetValue("ExchangeAmount", StringComparison.OrdinalIgnoreCase, out var jExchangeAmount))
            {
                _logger.LogError($"Coin2Pay {transId}, depositInfo has no exchange rate data.");

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Comment = "DepositInfo has no exchange rate data.",
                    Response = CallbackResponse.Failed
                };
            }

            var requestAmount = decimal.Parse(input["RequestAmount"]);
            var requestCryptoAmount = jExchangeAmount.ToObject<decimal>();
            var exchangeRate = jExchangeRate.ToObject<decimal>();

            var result = new NotifyDepositResult
            {
                DepositId = transId,
                ThirdId = dic["txid"],
                TransStatus = TransStatus.Success,
                Response = CallbackResponse.Success
            };

            result.ActualAmount =
                GetActualAmount(requestAmount, requestCryptoAmount, decimal.Parse(dic["quantity"]), exchangeRate);

            return result;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var baseApiUrl = inputs["host"];
            var currency = inputs["PayCurrencyId"];
            var payCode = inputs["PayCode"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var transId = inputs["TransID"];
            var platformId = inputs["PlatformID"];
            var privateKey = inputs["PrivateKey"];
            var withdrawalInfo = inputs["WithdrawalInfo"];

            try
            {
                var addr = GetWalletAddress(inputs);
                var (token, network) = PayCode2TokenInfo(payCode);
                var req = new Dictionary<string, string>
                {
                    {"platformID", platformId},
                    {"currency", currency},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()},
                    {"withdrawal_credit", reqAmount.ToString("f8")}
                };

                req.Add("sign", GetSign(req, privateKey));

                var withdrawalEstimate = GetWithdrawalEstimate(baseApiUrl, req);
                var estVal = withdrawalEstimate.Data.AssetEstimate
                    .FirstOrDefault(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase));

                if (estVal == null)
                {
                    throw new CustomException("Get withdrawal estimation asset data not exists. token: " + token);
                }

                if (withdrawalInfo == null)
                    throw new CustomException("WithdrawalInfo is empty.");

                req = new Dictionary<string, string>
                {
                    { "platformID", platformId },
                    { "from_address", addr.Address },
                    { "to_address", inputs["AccountNo"] },
                    { "order_id", transId },
                    { "creditamount", withdrawalEstimate.Data.WithdrawalCredit.ToString("f8") }, // 要用withdrawal_Credit分數點送金額
                    { "token", token },
                    { "network", network },
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() }
                };

                req.Add("sign", GetSign(req, privateKey));

                var withdrawalResp = Withdrawal(baseApiUrl, req);

                // 將匯率資訊加入 WithdrawalInfo
                var rate = CalculateRate(reqAmount, estVal.EstimateValue);
                withdrawalInfo = withdrawalInfo.AddOrSetExchangeInfo(rate.ToString(), withdrawalResp.Data.ActualAmountReceived.ToString());

                var response = new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId,
                    RawContent = JsonConvert.SerializeObject(withdrawalResp),
                    WithdrawalInfo = withdrawalInfo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

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
            var mPublicKey = payResource["key"];
            var dic = FromJson<Dictionary<string, string>>(bodyContent);
            var transId = dic["uid"];

            var str =
                $"{dic["txid"]}{dic["order_id"]}{dic["token"]}{dic["amount"]}{dic["withdrawal_amount"]}{dic["withdrawal_fee"]}{dic["is_green"]}{dic["timestamp"]}";

            if (!VerifySign(str, dic["sign"], mPublicKey))
            {
                _logger.LogInformation($"Coin2Pay {transId}, signature verification failed.");

                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Message = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            var isGreen = dic["is_green"] == "1"; // 交易已上鏈

            if (isGreen)
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    ThirdId = dic["txid"],
                    TransStatus = TransStatus.Success,
                    Message = dic["is_green"],
                    Response = CallbackResponse.Success
                };
            }

            return new NotifyWithdrawResult
            {
                WithdrawId = transId,
                TransStatus = TransStatus.Pending,
                Message = "Signature verification failed.",
                Response = CallbackResponse.Success
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var req = FormJsonHandling<DepositCallBackReq>(formData, bodyContent);

            return req.TransId;
        }

        #endregion

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public (string TransId, IDictionary<string, string> Data) FetchTransIdAndDataBeforeVerify(
            IDictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers)
        {
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            return (dicReq["UID"], dicReq);
        }

        public VerifyTransactionResult VerifyTransaction(IDictionary<string, string> data, IDictionary<string, string> input)
        {
            var transExists = bool.Parse(input["TransIdExists"]);

            if (!transExists)
            {
                return new VerifyTransactionResult
                {
                    ContentType = ContentType.Json,
                    Response = JsonConvert.SerializeObject(new VerifyWithdrawalResp
                    {
                        Message = $"Transaction UID: {data["UID"]} not exists.",
                        Verified = false
                    })
                };
            }

            var str = $"{data["UID"]}{data["order_id"]}{data["token"]}{data["network"]}{data["creditamount"]}{data["timestamp"]}";

            if (!VerifySign(str, data["sign"], input["key"]))
            {
                return new VerifyTransactionResult
                {
                    ContentType = ContentType.Json,
                    Response = JsonConvert.SerializeObject(new VerifyWithdrawalResp
                    {
                        Message = $"Transaction UID: {data["UID"]} signature verification failed.",
                        Verified = false
                    })
                };
            }

            return new VerifyTransactionResult
            {
                ContentType = ContentType.Json,
                Response = JsonConvert.SerializeObject(new VerifyWithdrawalResp
                {
                    Message = "Success.",
                    Verified = true
                })
            };
        }

        #region Private Methods

        private GetAddressData GetWalletAddress(IDictionary<string, string> input)
        {
            var baseApiUrl = input["host"];
            var currency = input["PayCurrencyId"];
            var payCode = input["PayCode"];
            var merchant = input["Merchant"];
            var platformId = input["PlatformID"];
            var transId = input["TransID"];
            var privateKey = input["PrivateKey"];

            var req = new Dictionary<string, string>
            {
                { "platformID", platformId },
                { "UID", transId },
                { "UserName", merchant },
                { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                { "currency", currency }
            };

            req.Add("sign", GetSign(req, privateKey));

            var (token, network) = PayCode2TokenInfo(payCode);
            var resp = GetAddress(baseApiUrl, req);
            var addr = resp.Data
                .FirstOrDefault(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase)
                                     && x.Network.Equals(network, StringComparison.OrdinalIgnoreCase));

            if (addr == null)
            {
                throw new CustomException("Address data not exists. network: " + payCode);
            }

            return addr;
        }

        private GetAddressResp GetAddress(string baseApiUrl, IDictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.GetAddress;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", ContentType.Json)
                .PostJson<IDictionary<string, string>, GetAddressResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay GetAddress failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Result)
                throw new CustomException(
                    $"Coin2Pay GetAddress response error code: {result.Code}, msg: {result.Message}");

            return result;
        }

        private GetWithdrawalEstimateResp GetWithdrawalEstimate(string baseApiUrl, IDictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.GetWithdrawalEstimate;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", ContentType.Json)
                .PostJson<IDictionary<string, string>, GetWithdrawalEstimateResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay GetWithdrawalEstimate failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Result)
                throw new CustomException(
                    $"Coin2Pay GetWithdrawalEstimate response error: {result.Code}, message: {result.Message}");

            return result;
        }

        private WithdrawalResp Withdrawal(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.RequestWithdrawal;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", ContentType.Json)
                .PostJson<Dictionary<string, string>, WithdrawalResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay Payout failed, HttpStatus: {(int)statusCode} {statusCode}" +
                    $", error code: {result.Code} {result.Message}");
            }

            if (!result.Result)
                throw new CustomException(
                    $"Coin2Pay Payout response error: {result.Code} {result.Message}");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string privateKey)
        {
            var str = string.Join(string.Empty, param
                .Select(x => x.Value));

            _logger.LogInformation("str: " + str);

            var signHash = RsaSign(str, privateKey, HashAlgorithmName.SHA256);
            var sign = ReplaceBase64(signHash);

            return sign;
        }

        private static bool VerifySign(string str, string target, string publicKey)
        {
            var targetBase64Str = RevertBase64(target);
            var result = RsaHelper.RsaVerifyData(str, targetBase64Str, publicKey, HashAlgorithmName.SHA256);
            
            return result;
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

        private static string RevertBase64(string str)
            => PadBase64(str).Replace('-', '+')
                .Replace('_', '/')
                .Replace(" ", string.Empty);

        private static string ReplaceBase64(string str)
            => str.Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);


        private static string PadBase64(string base64String)
        {
            var gapBase64 = base64String.Length % 4;
            if (gapBase64 == 0)
                return base64String;

            gapBase64 = 4 - gapBase64;
            while (gapBase64 > 0)
            {
                base64String += "=";
                gapBase64--;
            }

            return base64String;
        }

        private static (string Token, string Network) PayCode2TokenInfo(string payCode)
        {
            var tokenInfo = payCode.Split('_');

            return (tokenInfo[0], tokenInfo[1]);
        }

        private static T FormJsonHandling<T>(IDictionary<string, string> formData, string bodyContent)
        {
            var temp = formData.FirstOrDefault().Key; // C2P的ContentType有問題，先做此處理
            var req = FromJson<T>(string.IsNullOrEmpty(temp)
                ? bodyContent : temp.Replace("\\\"", "\""));

            return req;

        }

        private static decimal CalculateRate(decimal amount, decimal cryptoAmount)
            => cryptoAmount / amount;

        #endregion
    }
}