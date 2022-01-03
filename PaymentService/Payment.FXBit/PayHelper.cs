using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
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

            try
            {
                // Step 1: 取得當下匯率並換算為加密貨幣金額
                var rateInfo = GetExchangeRateInfo(baseApiUrl, apiKey, fromCurrency, toCurrency);
                var exchangeAmount = decimal.Parse(reqAmount) / decimal.Parse(rateInfo.BuyRate);

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

                // Step 4: 組成 H5 支付頁網址並回傳
                var h5Url = $"{apiUrl}?receipt_token={getTokenResp.ReceiptToken}&wallet_addr={getWalletResp.PublicAddress}";
                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    ThirdId = getTokenResp.ReceiptToken,
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
        
        private string GetSign(IDictionary<string, string> param, string apiKey)
        {
            var sortedParam = param.OrderBy(x => x.Key);
            var str = sortedParam.Select(x => $"{x.Key}={x.Value}")
                      + $"&{apiKey}";
            var sign = base.ToSHA256(str);

            return sign;
        }

        private bool VerifySign(IDictionary<string, string> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);
            return sign.Equals(target, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
