using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;

namespace Payment.FXBit
{
    class PayHelper : PayHelperBase, IExchangeHelper
    {
        #region ExchangeRate

        public ExchangeResult GetExchange(Dictionary<string, string> input)
        {
            var baseApiUrl = input["host"];
            var apiKey = input["key"];

            try
            {
                var fromCurrency = input["PayCurrencyId"];
                var toCurrency = input["PayCode"];
                var reqAmount = input["Amount"];

                var rateInfo = GetExchangeRateInfo(baseApiUrl, apiKey, fromCurrency, toCurrency);
                var exchangeAmount = decimal.Parse(reqAmount) / decimal.Parse(rateInfo.BuyRate);
                var exchangeRate = new Dictionary<string, string>
                {
                    { "To_Amount", exchangeAmount.ToString() },
                    { "Rate", rateInfo.BuyRate },
                    { "Updated_timestamp", rateInfo.UpdatedTimestamp.ToUnixTimeSeconds().ToString() }
                };

                return new ExchangeResult(ErrorCode.Success, "Success", exchangeRate);
            }
            catch (FXBitException ex)
            {
                return new ExchangeResult(ex.Code, ex.Message);
            }
        }

        #endregion

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var baseApiUrl = input["host"];
            var apiKey = input["key"];
            var apiUrl = baseApiUrl + Path.H5;
            var fromCurrency = input["PayCurrencyId"];
            var toCurrency = input["PayCode"];
            var reqAmount = input["Amount"];
            var transId = input["TransID"];
            var depositInfo = input["DepositInfo"];

            try
            {
                // Step 1: 取得當下匯率並換算為加密貨幣金額
                var rateInfo = GetExchangeRateInfo(baseApiUrl, apiKey, fromCurrency, toCurrency);
                var exchangeAmount = decimal.Parse(reqAmount) / decimal.Parse(rateInfo.BuyRate);
                
                // 將匯率資訊加入 DepositInfo
                var jObj = JObject.Parse(depositInfo);
                jObj.Add("ExchangeRate", rateInfo.BuyRate);
                
                // Step 2: 建立交易
                var getTokenReq = new GetReceptionTokenReq
                {
                    MarkedCurrency = toCurrency,
                    MarkedPrice = exchangeAmount,
                    Remark = transId,
                    CallbackUrl = input["CallBackUrl"]
                };
                var getTokenResp = GetReceptionToken(baseApiUrl, apiKey, getTokenReq, transId);

                // Step 3: 取得錢包位址
                var getWalletReq = new GetWalletAddressReq
                {
                    BilledCurrency = toCurrency,
                    ReceiptToken = getTokenResp.ReceiptToken
                };
                var getWalletResp = GetWalletAddress(baseApiUrl, apiKey, getWalletReq, transId);

                // 將回傳支付金額加入 DepositInfo
                jObj.Add("ExchangeAmount", getWalletResp.BilledPrice);
                depositInfo = jObj.ToString();

                // Step 4: 組成 H5 支付頁網址並回傳
                var h5Url = $"{apiUrl}?receipt_token={getTokenResp.ReceiptToken}&wallet_addr={getWalletResp.PublicAddress}";
                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    ThirdId = getTokenResp.ReceiptToken,
                    DepositInfo = depositInfo,
                    Url = h5Url
                };

                return response;
            }
            catch (FXBitException ex)
            {
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
            throw new NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
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
                throw new FXBitException("Response is empty.",
                    ErrorCode.GetExchangeRateFailed);

            var rateInfoList =
                fxBitRateList.FirstOrDefault(x => x.Key == toCurrency).Value;

            if (rateInfoList == null)
                throw new FXBitException($"{toCurrency} has no rate data.",
                    ErrorCode.GetExchangeRateFailed);

            var rateInfo =
                rateInfoList.FirstOrDefault(x => x.Key == fromCurrency).Value;

            if (rateInfo == null)
                throw new FXBitException($"{fromCurrency} has no rate data.",
                    ErrorCode.GetExchangeRateFailed);

            return rateInfo;
        }

        private IDictionary<string, Dictionary<string, FxBitExchangeRate>> GetExchangeRate(string apiKey,
            string baseApiUrl,
            string transId = "")
        {
            var apiUrl = baseApiUrl + Path.GetExchangeRate;
            try
            {
                var responseStr = HttpGet(apiUrl, string.Empty, MediaType.Json, apiKey, transId);

                if (string.IsNullOrEmpty(responseStr))
                    throw new FXBitException("GetExchangeRate response is empty.",
                        ErrorCode.GetExchangeRateFailed);

                var fxBitRateList = JsonConvert
                    .DeserializeObject<Dictionary<string, Dictionary<string, FxBitExchangeRate>>>(responseStr);

                return fxBitRateList;
            }
            catch (WebException ex)
            {
                throw new FXBitException(ex.Message, ErrorCode.GetExchangeRateFailed);
            }
        }

        private GetReceptionTokenResp GetReceptionToken(string baseApiUrl,
            string apiKey,
            GetReceptionTokenReq req,
            string transId)
        {
            var apiUrl = baseApiUrl + Path.CreateTransaction;
            var response = GeneralFxBitPost<GetReceptionTokenReq, GetReceptionTokenResp>(apiUrl,
                apiKey,
                req,
                ErrorCode.GetReceptionTokenFailed,
                transId);

            if (response.Error != 0)
                throw new FXBitException($"GetReceptionToken response error code: {response.Error}",
                    ErrorCode.GetReceptionTokenFailed);

            return response;
        }

        private GetWalletAddressResp GetWalletAddress(string baseApiUrl,
            string apiKey,
            GetWalletAddressReq req,
            string transId)
        {
            var apiUrl = baseApiUrl + Path.GetWalletAddress;
            var response = GeneralFxBitPost<GetWalletAddressReq, GetWalletAddressResp>(apiUrl,
                apiKey,
                req,
                ErrorCode.GetWalletAddressFailed,
                transId);

            if (response.Error != 0)
                throw new FXBitException(
                    $"GetReceptionToken response error: {response.Error}, message: {response.ErrorMessage}",
                    ErrorCode.GetWalletAddressFailed);

            return response;
        }

        private TResponse GeneralFxBitPost<TRequest, TResponse>(string apiUrl,
            string apiKey,
            TRequest request,
            string errorCode,
            string transId = "")
        {
            try
            {
                var responseStr = HttpPost(apiUrl, ToJson(request), MediaType.Json, apiKey, transId);

                if (string.IsNullOrEmpty(responseStr))
                    throw new FXBitException("Response is empty.", errorCode);

                var response = JsonConvert
                    .DeserializeObject<TResponse>(responseStr);

                return response;
            }
            catch (WebException ex)
            {
                throw new FXBitException("WebException: " + ex.Message, errorCode);
            }
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

        private decimal GetActualAmount(decimal requestAmount,
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

        #endregion
    }
}
