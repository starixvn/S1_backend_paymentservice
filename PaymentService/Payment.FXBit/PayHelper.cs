using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.FXBit
{
    class PayHelper : PayHelperBase, IExchangeHelper
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region ExchangeRate

        public ExchangeResult GetExchange(Dictionary<string, string> input)
        {
            var baseApiUrl = input["host"];
            var apiKey = input["key"];
            var type = input["ExchangeType"]; // 0: 提款, 1: 存款

            try
            {
                var fromCurrency = input["PayCurrencyId"];
                var toCurrency = input["PayCode"];
                var reqAmount = input["Amount"];

                var rateInfo = GetExchangeRateInfo(baseApiUrl, apiKey, fromCurrency, toCurrency);
                var rate = type.Equals("0") ? rateInfo.BuyRate : rateInfo.SellRate; // FxBit的匯率買入買出和一般是反過來
                var exchangeAmount = GetExchangeAmountString(reqAmount,rate , toCurrency);
                var exchangeRate = new Dictionary<string, string>
                {
                    { "To_Amount", exchangeAmount },
                    { "Rate", rate },
                    { "Updated_timestamp", rateInfo.UpdatedTimestamp.ToUnixTimeSeconds().ToString() }
                };

                return new ExchangeResult(ErrorCode.Success, "Success", exchangeRate);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

                return new ExchangeResult(ex.Code, ex.Message);
            }
        }

        #endregion

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var baseApiUrl = input["host"];
            var apiKey = input["key"];
            //var apiUrl = baseApiUrl + Path.H5;
            var fromCurrency = input["PayCurrencyId"];
            var toCurrency = input["PayCode"];
            var reqAmount = input["Amount"];
            var transId = input["TransID"];
            var depositInfo = input["DepositInfo"];

            try
            {
                // Step 1: 取得當下匯率並換算為加密貨幣金額
                var rateInfo = GetExchangeRateInfo(baseApiUrl, apiKey, fromCurrency, toCurrency);
                var exchangeAmount = GetExchangeAmountString(reqAmount, rateInfo.SellRate, toCurrency);

                // 檢查 DepositInfo
                if (depositInfo == null)
                    throw new CustomException("DepositInfo is empty.");

                // Step 2: 建立交易
                var getTokenReq = new GetReceptionTokenReq
                {
                    MarkedCurrency = toCurrency,
                    MarkedPrice = exchangeAmount,
                    Remark = transId,
                    CallbackUrl = input["CallBackUrl"]
                };
                var getTokenResp = GetReceptionToken(baseApiUrl, apiKey, getTokenReq);

                // Step 3: 取得錢包位址
                var getWalletReq = new GetWalletAddressReq
                {
                    BilledCurrency = toCurrency,
                    ReceiptToken = getTokenResp.ReceiptToken
                };
                var getWalletResp = GetWalletAddress(baseApiUrl, apiKey, getWalletReq);

                // 將匯率與回傳支付金額加入 DepositInfo
                depositInfo = depositInfo
                    .AddOrSetExchangeInfo(rateInfo.SellRate, getWalletResp.BilledPrice.ToString());

                // Step 4: 組成 H5 支付頁網址並回傳
                /*var h5Url = $"{apiUrl}?receipt_token={getTokenResp.ReceiptToken}&wallet_addr={getWalletResp.PublicAddress}";
                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    ThirdId = getTokenResp.ReceiptToken,
                    DepositInfo = depositInfo,
                    Url = h5Url
                };*/

                // 因為 H5 網頁預設為中文，改為使用 url 以 QRCode 呈現
                var rate = GetExchangeRateString(rateInfo.SellRate);
                var formData = new Dictionary<string, string>
                {
                    { "DepositAmount", reqAmount },
                    { "Rate", rate },
                    { "RequestAmount", exchangeAmount },
                    { "Token", toCurrency }
                };

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = getWalletResp.PublicAddress,
                    ThirdId = getTokenResp.ReceiptToken,
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

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> input)
        {
            var apiKey = input["key"];
            var req = FromJson<DepositCallBackReq>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, apiKey, req.Signature))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            var transId = input["TransID"];
            var transStatus = FXBitDefinition.GetDepositTransStatus(req.Status);
            var result = new NotifyDepositResult
            {
                DepositId = transId,
                ThirdId = req.ReceiptToken,
                Comment = req.Status,
                TransStatus = transStatus,
                Response = CallbackResponse.Success
            };

            if (transStatus == TransStatus.Success)
            {
                /*
                 * FxBit 若到帳金額和申請金額不符會自動更新訂單資訊申請金額自動完成訂單
                 * 需取出 DepositInfo 中的匯率資訊確認入帳金額和申請金額是否相符
                 */
                var depositInfo = input["DepositInfo"];
                var jObj = JObject.Parse(depositInfo);

                if (!jObj.TryGetValue("ExchangeRate", StringComparison.OrdinalIgnoreCase, out var jExchangeRate)
                    || !jObj.TryGetValue("ExchangeAmount", StringComparison.OrdinalIgnoreCase, out var jExchangeAmount))
                {
                    return new NotifyDepositResult
                    {
                        TransStatus = TransStatus.Pending,
                        Comment = "DepositInfo has no exchange rate data.",
                        Response = CallbackResponse.Failed
                    };
                }

                var requestAmount = decimal.Parse(input["RequestAmount"]);
                var requestCryptoAmount = jExchangeAmount.ToObject<decimal>();
                var exchangeRate = jExchangeRate.ToObject<decimal>();

                result.ActualAmount =
                    GetActualAmount(requestAmount, requestCryptoAmount, req.ReceivedAmount, exchangeRate);
            }

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
            var apiKey = inputs["key"];
            var payoutKey = inputs["payoutKey"];
            var payoutApiUrl = inputs["payoutUrl"];
            var fromCurrency = inputs["PayCurrencyId"];
            var toCurrency = inputs["PayCode"];
            var reqAmount = inputs["Amount"];
            var transId = inputs["TransID"];
            var withdrawalInfo = inputs["WithdrawalInfo"];

            try
            {
                // Step 1: 取得當下匯率並換算為加密貨幣金額
                var rateInfo = GetExchangeRateInfo(baseApiUrl, apiKey, fromCurrency, toCurrency);
                var exchangeAmount = GetExchangeAmountString(reqAmount, rateInfo.BuyRate, toCurrency);

                // 將匯率資訊加入 WithdrawalInfo
                if (withdrawalInfo == null)
                    throw new CustomException("WithdrawalInfo is empty.");

                withdrawalInfo = withdrawalInfo.AddOrSetExchangeInfo(rateInfo.BuyRate, exchangeAmount);

                // Step 2: 建立交易
                var payoutReq = new Dictionary<string, string>
                {
                    { "currency", toCurrency },
                    { "amount", exchangeAmount },
                    { "address", inputs["AccountNo"] },
                    { "time_stamp", new DateTimeOffset().ToUnixTimeSeconds().ToString() },
                    { "req_payout_id", GetPayoutId(transId) },
                    { "remark", transId },
                    { "type", "2" } // payout 固定為 2
                };

                payoutReq.Add("signature", GetSign(payoutReq, payoutKey));
               
                var payoutResp = Payout(payoutApiUrl, apiKey, payoutReq);
                var response = new WithdrawResult
                {
                    Success = true,
                    WithdrawId = payoutResp.ResPayoutId,
                    RawContent = JsonConvert.SerializeObject(payoutResp),
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            var apiKey = inputs["key"];
            var payoutKey = inputs["payoutKey"];
            var apiUrl = inputs["payoutUrl"];
            var transId = inputs["TransID"];

            var req = new Dictionary<string, string>
            {
                { "payout_id", GetPayoutId(transId) },
                { "time_stamp", new DateTimeOffset().ToUnixTimeSeconds().ToString() },
                { "type", "3" } // payout 查詢固定為 3
            };

            req.Add("signature", GetSign(req, payoutKey));

            var resp = PayoutQuery(apiUrl, apiKey, req);
            var transStatus = FXBitDefinition.GetPayoutTransStatus(resp.Status);

            return new EnquiryWithdrawResult
            {
                TransStatus = transStatus,
                Message = transStatus.ToString(),
                RawContent = JsonConvert.SerializeObject(resp)
            };
        }

        #endregion

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #region Private Methods


        private FxBitExchangeRate GetExchangeRateInfo(string baseApiUrl,
            string apiKey,
            string fromCurrency,
            string toCurrency)
        {
            var fxBitRateList = GetExchangeRate(apiKey, baseApiUrl);

            if (!fxBitRateList.Any())
                throw new CustomException("Response is empty.",
                    ErrorCode.GetExchangeRateFailed);

            var rateInfoList =
                fxBitRateList.FirstOrDefault(x => x.Key == toCurrency).Value;

            if (rateInfoList == null)
                throw new CustomException($"{toCurrency} has no rate data.",
                    ErrorCode.GetExchangeRateFailed);

            var rateInfo =
                rateInfoList.FirstOrDefault(x => x.Key == fromCurrency).Value;

            if (rateInfo == null)
                throw new CustomException($"{fromCurrency} has no rate data.",
                    ErrorCode.GetExchangeRateFailed);

            return rateInfo;
        }

        private IDictionary<string, Dictionary<string, FxBitExchangeRate>> GetExchangeRate(string apiKey,
            string baseApiUrl)
        {
            var apiUrl = baseApiUrl + Path.GetExchangeRate;

            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .WithHeader("x-api-key", apiKey)
                .GetAsync<Dictionary<string, Dictionary<string, FxBitExchangeRate>>>(apiUrl, string.Empty)
                .Result;

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"FxBit GetExchangeRate failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}",
                    ErrorCode.GetExchangeRateFailed);
            }

            return response.Result;
        }

        private GetReceptionTokenResp GetReceptionToken(string baseApiUrl,
            string apiKey,
            GetReceptionTokenReq req)
        {
            var apiUrl = baseApiUrl + Path.CreateTransaction;

            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .WithHeader("x-api-key", apiKey)
                .PostJsonAsync<GetReceptionTokenReq, GetReceptionTokenResp>(apiUrl, req)
                .Result;

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"FxBit GetReceptionToken failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}" +
                    $",errorCode: {response.Result.Error}",
                    ErrorCode.GetReceptionTokenFailed);
            }
            
            if (response.Result.Error != 0)
                throw new CustomException($"FxBit GetReceptionToken response error code: {response.Result.Error}",
                    ErrorCode.GetReceptionTokenFailed);

            return response.Result;
        }

        private GetWalletAddressResp GetWalletAddress(string baseApiUrl,
            string apiKey,
            GetWalletAddressReq req)
        {
            var apiUrl = baseApiUrl + Path.GetWalletAddress;

            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .WithHeader("x-api-key", apiKey)
                .PostJsonAsync<GetWalletAddressReq, GetWalletAddressResp>(apiUrl, req)
                .Result;

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"FxBit GetWalletAddress failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}" +
                    $", error code: {response.Result.Error}, message: {response.Result.ErrorMessage}",
                    ErrorCode.GetWalletAddressFailed);
            }

            if (response.Result.Error != 0)
                throw new CustomException(
                    $"FxBit GetReceptionToken response error: {response.Result.Error}, message: {response.Result.ErrorMessage}",
                    ErrorCode.GetWalletAddressFailed);

            return response.Result;
        }

        private PayoutQueryResp PayoutQuery(string apiUrl,
            string apiKey,
            Dictionary<string, string> req)
        {
            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .WithHeader("x-api-key", apiKey)
                .PostJsonAsync<Dictionary<string, string>, PayoutQueryResp>(apiUrl, req)
                .Result;

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"FxBit PayoutQuery failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}" +
                    $", error code: {response.Result.Error} {FXBitDefinition.GetPayoutErrorMessage(response.Result.Error)}",
                    ErrorCode.PayoutFailed);
            }

            if (response.Result.Error != 0)
                throw new CustomException(
                    $"FxBit PayoutQuery response error: {response.Result.Error} {FXBitDefinition.GetPayoutErrorMessage(response.Result.Error)}",
                    ErrorCode.GetWalletAddressFailed);

            return response.Result;
        }

        private PayoutResp Payout(string apiUrl,
            string apiKey,
            Dictionary<string, string> req)
        {
            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .WithHeader("x-api-key", apiKey)
                .PostJsonAsync<Dictionary<string, string>, PayoutResp>(apiUrl, req)
                .Result;

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"FxBit Payout failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}" +
                    $", error code: {response.Result.Error} {FXBitDefinition.GetPayoutErrorMessage(response.Result.Error)}",
                    ErrorCode.PayoutFailed);
            }

            if (response.Result.Error != 0)
                throw new CustomException(
                    $"FxBit Payout response error: {response.Result.Error} {FXBitDefinition.GetPayoutErrorMessage(response.Result.Error)}",
                    ErrorCode.GetWalletAddressFailed);

            return response.Result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var str = string.Join('&', param
                .Where(x => !x.Key.Equals("signature", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + $"&{apiKey}";

            var sign = base.ToSHA256(str);

            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);
            return sign.Equals(target, StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetActualAmount(decimal requestAmount,
            decimal requestCryptoAmount,
            decimal receivedCryptoAmount,
            decimal exchangeRate)
        {
            if (receivedCryptoAmount == requestCryptoAmount)
                return requestAmount;

            // 使用匯率回推平台金額，保留小數 2 位
            var actualAmount = decimal.Round(receivedCryptoAmount * exchangeRate, 2);
            return actualAmount;
        }

        private static string GetPayoutId(string transId)
            => Regex.Replace(transId, "[^0-9]", "")
                .PadLeft(11, '0');

        private static string GetExchangeAmountString(string reqAmount, string rate, string toCurrency)
        {
            // 小數位數 ETH BTC 8 位，其餘 2 位
            var unit = toCurrency.Equals("ETH", StringComparison.OrdinalIgnoreCase)
                       || toCurrency.Equals("BTC", StringComparison.OrdinalIgnoreCase)
                ? 8 : 2;

            var amount = decimal.Parse(reqAmount) / decimal.Parse(rate);
            
            return amount.ToString($"f{unit}");
        }

        private static string GetExchangeRateString(string rate)
            => (1M / decimal.Parse(rate)).ToString("f15");
        
        #endregion
    }
}
