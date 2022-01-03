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
                    Url = h5Url
                };

                return response;
            }
            catch (FXBitException ex)
            {
                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            // User serivce to pull ticket status because need to get txid
            throw new NotImplementedException();


            var data = FromJson<CallBackDepositReq>(bodyContent);
            var dicP = FromJson<Dictionary<string, string>>(ToJson(data)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("&", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("signature", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Key}={kp.Value}").ToArray()) + $"&{payResource["key"]}";
            var sign = ToSHA256(p).ToLower();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = payResource["TransID"];
            oNotify.ThirdId = data.receipt_token;
            oNotify.Comment = data.status;
            oNotify.Message = data.status;

            if (sign == data.signature)
            {
                if (data.status == "2")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.marked_price);
                    oNotify.ActualAmount = decimal.Parse(data.marked_price);
                    oNotify.Comment = data.status;
                    oNotify.Response = "success";
                }
                else if (data.status == "3")
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
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
            //var sUrl = inputs["host"];
            //var apiKey = inputs["key"];
            //var getReceptionToken = sUrl + "getTransactionDetail";
            //var creditReq = new CreditReq();
            //creditReq.receipt_token = inputs["ThirdID"];

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            //var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var sResponse = HttpGet(getReceptionToken, sParam, "application/json", apiKey, inputs["TransID"]);

            //var creditResp = new CreditResp();
            //creditResp = JsonConvert.DeserializeObject<CreditResp>(sResponse);

            //var enquiryDepositResult = new EnquiryDepositResult();
            //enquiryDepositResult.DepositId = inputs["TransID"];
            //enquiryDepositResult.ThirdId = inputs["ThirdID"];
            //enquiryDepositResult.Comment = creditResp.payment_status;
            //enquiryDepositResult.Message = creditResp.payment_status;

            //if (creditResp.payment_status == "3")
            //{
            //    enquiryDepositResult.TransStatus = TransStatus.Fail;
            //}
            //else if (creditResp.payment_status == "2")
            //{
            //    enquiryDepositResult.TransStatus = TransStatus.Success;
            //    enquiryDepositResult.RequestAmount = decimal.Parse(creditResp.marked_price);
            //    enquiryDepositResult.ActualAmount = decimal.Parse(creditResp.marked_price);
            //    enquiryDepositResult.RefThirdId = creditResp.transaction_hash;
            //}
            //else
            //{
            //    enquiryDepositResult.TransStatus = TransStatus.Pending;
            //}

            //return enquiryDepositResult;
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

        #endregion
    }
}
