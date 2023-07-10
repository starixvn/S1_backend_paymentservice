using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.DirePay
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

        #region Exchange Rate
        
        ExchangeResult IExchangeHelper.GetExchange(Dictionary<string, string> input)
        {
            try
            {
                var fromCurrency = GetPayCurrency(input["PayCurrencyId"]);
                var toCurrency = input["PayCode"];
                var amount = input["Amount"];
                var type = input["ExchangeType"];
                var toAmount = GetExchangeAmount(amount, fromCurrency, toCurrency, input, type);
        
                if (toAmount > 0)
                {
                    return new ExchangeResult(ErrorCode.Success, "success", new CryptoExchangeInfo
                    {
                        Rate = toAmount / decimal.Parse(amount),
                        ExchangeAmount = toAmount,
                        Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                    });
                }

                throw new CustomException("Incorrect exchange amount: " + toAmount);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

                return new ExchangeResult(ex.Code, ex.Message);
            }
        }

        #endregion

        #region Deposit
        
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var payCode = inputs["PayCode"];
            var (isFiat, method) = GetMethod(payCode);

            try
            {
                return isFiat ? FiatPayin(inputs, method) : CryptoPayin(inputs, method);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            /*if (!VerifySign(payResource["key"], bodyContent, headers))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }*/

            var isCrypto = payResource["DepositType"] == "4";
            
            var result = isCrypto
                ? CryptoPayinCallBack(bodyContent)
                : FiatPayinCallBack(bodyContent);

            result.DepositId = payResource["TransID"];
            result.Response = "true";
            
            return result;
        }

        #endregion

        #region Withdrawal
        
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var isCrypto = inputs["WithdrawalType"] == "4";
                return isCrypto ? CryptoPayout(inputs) : FiatPayout(inputs);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            /*if (!VerifySign(payResource["key"], bodyContent, headers))
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }*/

            NotifyWithdrawResult result;
            
            var isCrypto = payResource["WithdrawalType"] == "4";
            if (isCrypto)
            {
                var data = FromJson<CryptoPayoutCallBackReq>(bodyContent);
                var transStatus = DirePayDefinition.GetCryptoPayoutTransStatus(data.Status);
            
                result = new NotifyWithdrawResult
                {
                    TransStatus = transStatus,
                    ThirdId = data.ThirdId,
                    WithdrawId = data.TransId,
                    Message = data.Status,
                    RequestAmount = data.Amount
                };
            }
            else
            {
                var data = FromJson<FiatPaymentCallBackReq>(bodyContent);
                var transStatus = DirePayDefinition.GetFiatPayoutTransStatus(data.Status);
            
                result = new NotifyWithdrawResult
                {
                    TransStatus = transStatus,
                    ThirdId = data.ThirdId,
                    WithdrawId = data.ThirdId,
                    Message = data.Status,
                    RequestAmount = data.ActualAmount
                };
            }

            result.Response = "true";
            
            return result;
        }

        #endregion

        #region Not Implemented

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private string GetAccessToken(IDictionary<string, string> inputs)
        {
            var baseApiUrl = inputs["host"];
            var loginUrl = baseApiUrl + Path.Login;

            var request = new Dictionary<string, string>
            {
                { "email", inputs["Email"] },
                { "password", inputs["Password"] }
            };

            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, GetAccessTokenResp>(loginUrl, request);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay login failed, HttpStatus: {(int)statusCode} {statusCode} Message: {result.Message}");
            }

            if (string.IsNullOrEmpty(result.AccessToken))
            {
                throw new CustomException("DirePay login response access_token is empty");
            }

            return result.AccessToken;
        }
        
        private DepositResult CryptoPayin(IDictionary<string, string> inputs, string method)
        {
            var apiUrl = inputs["host"] + Path.Action;
            var payCode = inputs["PayCode"];
            var amount = decimal.Parse(inputs["Amount"]);
            var payCurrency = GetPayCurrency(inputs["PayCurrencyId"]);
            var depositInfo = inputs["DepositInfo"];
            
            // 檢查 DepositInfo
            if (depositInfo == null)
            {
                return new DepositResult("DepositInfo is empty.");
            }
            
            var accessToken = GetAccessToken(inputs);
            var req = new PayinGetWalletReq
            {
                command = Command.Payment,
                line_items = new[]
                {
                    new LineItem
                    {
                        name = "deposit",
                        item_id = method,
                        description = $"Deposit {payCurrency} via {method}",
                        amount = amount.ToString("G29"),
                        quantity = "1"
                    }
                },
                currency = payCurrency,
                token = payCode,
                callback_url = inputs["CallBackUrl"],
                customer_uid = inputs["CustId"],
                client_reference_id = inputs["TransID"],
                txid = inputs["TransID"],
                hashCode = ToMD5(Command.Payment + inputs["key"]).ToLower()
            };

            var resp = CryptoPayinGetWallet(apiUrl, accessToken, req);
            
            var requestAmount = resp.InvoiceAmount * resp.Rates;
            var resData = new Dictionary<string, string>
            {
                {"DepositAmount", resp.InvoiceAmount.ToString("G29")},
                {"Rate", resp.Rates.ToString("G29")},
                {"RequestAmount", requestAmount.ToString("G29")},
                {"Token", resp.PaymentToken}
            };
            
            if (resp.EnablePromotion)
            {
                resData.Add("Promotion", "1");
                resData.Add("PromotionRate", resp.PromotionRate);
                resData.Add("PromotionAmount", resp.PromotionFiatAmount);
            }
            else
            {
                resData.Add("Promotion", "0");
            }

            return new DepositResult
            {
                Success = true,
                Url = payCode.Equals("XRP")
                    ? GetPayinXRPAddress(resp.PaymentAddress)
                    : resp.PaymentAddress,
                ThirdId = resp.ThirdId,
                FormData = resData,
                DepositInfo = depositInfo.AddOrSetExchangeInfo(resp.Rates.ToString("G29"), requestAmount.ToString("G29"))
            };
        }
        
        private CryptoPayinResp CryptoPayinGetWallet(string apiUrl, string accessToken, PayinGetWalletReq request)
        {
            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostJson<PayinGetWalletReq, CryptoPayinResp>(apiUrl, request);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay GetWallet failed, HttpStatus: {(int)statusCode} {statusCode} Message: {result.Message}");
            }
            
            return result;
        }
        
        
        private DepositResult FiatPayin(IDictionary<string, string> inputs, string method)
        {
            var apiUrl = inputs["host"] + Path.Action;
            var amount = decimal.Parse(inputs["Amount"]);
            var payCurrency = inputs["PayCurrencyId"];
            var splitMethod = method.Split("-");
            
            var accessToken = GetAccessToken(inputs);
            var req = new Dictionary<string, string>
            {
                { "command", Command.FiatPayment },
                { "hashCode", ToMD5(Command.FiatPayment + inputs["key"]).ToLower() },
                { "callback_url", inputs["CallBackUrl"] },
                { "redirect_url", inputs["ReturnUrl"] },
                { "currency", payCurrency },
                { "method", splitMethod[0] },
                { "customer_uid", inputs["CustId"] },
                { "depositor_name", inputs["AccountName"] },
                { "amount", amount.ToString("G29") },
                { "txid", inputs["TransID"] },
                { "bank_code", splitMethod.Length > 1 ? splitMethod[1] : inputs["BankCode"] }
            };

            var (resp, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncoded<Dictionary<string, string>, FiatPayinResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay fiat_payment failed, HttpStatus: {(int)statusCode} {statusCode} Message: {resp.Message}");
            }
           
            return new DepositResult
            {
                Success = true,
                ErrorCode = ErrorCode.Success,
                Url = resp.PayUrl,
                ThirdId = resp.ThirdId
            };
        }

        private decimal GetExchangeAmount(string amount, string fromCurrency, string toCurrency, IDictionary<string, string> inputs, string type = "1")
        {
            var baseApiUrl = inputs["host"];
            var getExchangeUrl = baseApiUrl + Path.Action;

            var accessToken = GetAccessToken(inputs);
            var request = new Dictionary<string, string>
                {
                    { "command", Command.Rates },
                    { "amount", amount },
                    { "from_currency", fromCurrency },
                    { "to_currency", toCurrency },
                    { "type", _dicExchangeType[type] }
                };

            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncoded<Dictionary<string, string>, ExchangeResp>(getExchangeUrl, request);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay GetExchangeRate failed, HttpStatus: {(int)statusCode} {statusCode} Message: {result.Message}");
            }

            var exchangeAmount = decimal.Parse(result.Amount);

            return exchangeAmount;
        }
        
        private bool VerifySign(string key, string content, IDictionary<string, string> headers)
        {
            var target = headers.FirstOrDefault(x => x.Key.Equals("Signature")).Value;
            _logger.LogInformation("Signature: " + target);
            var sign = ToMD5(key + content).ToLower();
            
            return target.Equals(sign);
        }

        private NotifyDepositResult CryptoPayinCallBack(string json)
        {
            var data = FromJson<CryptoPayinCallBackReq>(json);
            var transStatus = DirePayDefinition.GetCryptoPayinTransStatus(data.Status);
            var result = new NotifyDepositResult
            {
                TransStatus = transStatus,
                ThirdId = data.ThirdId,
                Comment = data.Status
            };
                
            if (transStatus == TransStatus.Success)
            {
                switch (status: data.Status, data.EnablePromotion)
                {
                    case (DirePayCryptoPaymentStatus.Completed, false):
                        result.ActualAmount = data.InvoiceAmount;
                        return result;
                    
                    case (DirePayCryptoPaymentStatus.Completed, true):
                        result.ActualAmount = data.InvoiceAmount + data.PromotionFiatAmount;
                        return result;
                    
                    case (DirePayCryptoPaymentStatus.TooLittle, false):
                    case (DirePayCryptoPaymentStatus.TooMuch, false):
                        result.ActualAmount = data.PaidFiat;
                        return result;
                    
                    case (DirePayCryptoPaymentStatus.TooLittle, true):
                    case (DirePayCryptoPaymentStatus.TooMuch, true):
                        result.ActualAmount = data.PaidFiat + data.PromotionFiatAmount;
                        return result;
                }
            }

            return result;
        }
        
        private NotifyDepositResult FiatPayinCallBack(string json)
        {
            var data = FromJson<FiatPaymentCallBackReq>(json);
            var transStatus = DirePayDefinition.GetFiatPayinTransStatus(data.Status);
            
            return new NotifyDepositResult
            {
                TransStatus = transStatus,
                ThirdId = data.ThirdId,
                Comment = data.Status,
                ActualAmount = data.ActualAmount
            };
        }
        
        private WithdrawResult CryptoPayout(IDictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"] + Path.Action;
            var payCode = inputs["PayCode"];
            var amount = decimal.Parse(inputs["Amount"]);
            var strAmount = amount.ToString("G29");
            var payCurrency = GetPayCurrency(inputs["PayCurrencyId"]);
            var withdrawalInfo = inputs["WithdrawalInfo"];
            var transId = inputs["TransID"];
            
            if (withdrawalInfo == null)
            {
                return new WithdrawResult("WithdrawalInfo is empty.");
            }

            var accessToken = GetAccessToken(inputs);
            var toAmount = GetExchangeAmount(strAmount, payCurrency, payCode, inputs, "0");
            var req = new Dictionary<string, string>
            {
                {"command", Command.PartnerWithdraw},
                {"amount", strAmount},
                {"currency", payCurrency},
                {"withdrawal_token", payCode},
                {"callback_url", inputs["CallBackUrl"]},
                {"customer_uid", inputs["CustId"]},
                {"address", inputs["AccountNo"]},
                {"client_reference_id", transId},
                {"withdrawal_amount", toAmount.ToString("G29")},
                {"hashCode", ToMD5(Command.PartnerWithdraw + inputs["key"]).ToLower()}
            };

            var resp = CryptoPayoutGetWallet(apiUrl, accessToken, req);
            
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = resp.Message,
                WithdrawalInfo = withdrawalInfo
                    .AddOrSetExchangeInfo((toAmount / amount).ToString("G29"), toAmount.ToString("G29"))
            };
        }
        
        private PayoutGetWalletResp CryptoPayoutGetWallet(string apiUrl, string accessToken, Dictionary<string, string> request)
        {
            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncoded<Dictionary<string, string>, PayoutGetWalletResp>(apiUrl, request);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay GetWallet failed, HttpStatus: {(int)statusCode} {statusCode} Message: {result.Message}");
            }
            
            if (result.Status != DirePayCryptoPaymentStatus.Pending)
            {
                throw new CustomException(
                    $"DirePay GetWallet failed, Status: {result.Status} Message: {result.Message}");
            }
            
            return result;
        }
        
        private WithdrawResult FiatPayout(IDictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"] + Path.Action;
            var amount = decimal.Parse(inputs["Amount"]);
            var strAmount = amount.ToString("G29");
            var payCurrency = inputs["PayCurrencyId"];
            var transId = inputs["TransID"];
            

            var accessToken = GetAccessToken(inputs);
            var req = new Dictionary<string, string>
            {
                {"command", Command.FiatWithdrawal},
                {"amount", strAmount},
                {"currency", payCurrency},
                {"callback_url", inputs["CallBackUrl"]},
                {"customer_uid", inputs["CustId"]},
                {"bank_account_number", inputs["AccountNo"]},
                {"bank_account_name", inputs["AccountName"]},
                {"bank_name", inputs["BankName"]},
                {"bank_code", inputs["BankCode"]},
                {"txid", transId},
                {"hashCode", ToMD5(Command.FiatWithdrawal + inputs["key"]).ToLower()}
            };

            var (resp, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncoded<Dictionary<string, string>, FiatPayoutResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay fiat_payment failed, HttpStatus: {(int)statusCode} {statusCode} Message: {resp.Message}");
            }
            
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = resp.Message
            };
        }

        private static (bool IsFiat, string Method) GetMethod(string payCode)
        {
            var splitStr = payCode.Split(':');
            var isFait = splitStr[0].Equals("fiat");

            return (isFait, splitStr.Length > 1 ? splitStr[1] : payCode);
        }

        private static string GetPayCurrency(string payCurrencyId)
        {
            var splitStr = payCurrencyId.Split('_');

            return splitStr[0];
        }
        
        private static string GetPayinXRPAddress(string addr)
        {
            var splitStr = addr.Split(':');

            return splitStr[0];
        }

        private static readonly Dictionary<string, string> _dicExchangeType = new()
        {
            {"0", "SELL"},
            {"1", "BUY"},
        };

        #endregion
    }
}
