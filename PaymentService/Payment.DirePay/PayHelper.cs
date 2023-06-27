using Newtonsoft.Json;
using PaymentCore;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

                throw new CustomException("Incorrect exchange amount: " + toAmount,
                    ErrorCode.GetExchangeRateFailed);
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

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDepositReq>(bodyContent);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = payResource["TransID"];
            oNotify.Comment = data.status;
            oNotify.Message = data.status;
            oNotify.Response = "true";

            if (data.status == "completed")
            {
                oNotify.RefThirdId = data.txid;
                oNotify.ThirdId = data.txid;
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.invoice_amount);
                if (data.enable_promotion == "false")
                {
                    oNotify.ActualAmount = decimal.Parse(data.invoice_amount);
                }
                else
                {
                    oNotify.ActualAmount = decimal.Parse(data.invoice_amount) + decimal.Parse(data.promotion_fiat_amount);
                }
            }
            else if (data.status == "too_little" || data.status == "too_much")
            {
                oNotify.RefThirdId = data.txid;
                oNotify.ThirdId = data.txid;
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.invoice_amount);
                if (data.enable_promotion == "false")
                {
                    oNotify.ActualAmount = decimal.Parse(data.paid_fiat);
                }
                else
                {
                    oNotify.ActualAmount = decimal.Parse(data.paid_fiat) + decimal.Parse(data.promotion_fiat_amount);
                }
            }
            else if (data.status == "expired")
            {
                oNotify.TransStatus = TransStatus.Fail;
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
            }

            return oNotify;
        }

        #endregion

        #region Withdrawal

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var getReceptionToken = sUrl + "/api/login";
            var getWalletAddress = sUrl + "/api/callBack";
            var withdrawalInfo = inputs["WithdrawalInfo"];

            var depositGetH5Req = new Dictionary<string, string>();
            depositGetH5Req.Add("email", inputs["Email"]);
            depositGetH5Req.Add("password", inputs["Password"]);
            var sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = null;

            
            var oDepositResult = new DepositResult();
            oDepositResult.Success = false;

            var withdrawResult = new WithdrawResult();
            // 檢查 WithdrawalInfo
            if (withdrawalInfo == null)
            {
                withdrawResult.Success = false;
                withdrawResult.Message = "WithdrawalInfo is empty.";
                return withdrawResult;
            }
            try
            {
                sResponse = HttpPost(getReceptionToken, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower(), true);

                var depositResp = new DepositResp();
                depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);

                if (depositResp != null && depositResp.access_token != null)
                {
                    var withdrawGetWalletReq = new WithdrawGetWalletReq();
                    withdrawGetWalletReq.command = "partner_withdraw";
                    var amount = decimal.Parse(inputs["Amount"]);
                    withdrawGetWalletReq.amount = amount.ToString("#");
                    withdrawGetWalletReq.currency = GetPayCurrency(inputs["PayCurrencyId"]);
                    withdrawGetWalletReq.withdrawal_token = inputs["PayCode"];
                    withdrawGetWalletReq.callback_url = inputs["CallBackUrl"];
                    withdrawGetWalletReq.address = inputs["AccountNo"];
                    withdrawGetWalletReq.customer_uid = inputs["CustId"];
                    withdrawGetWalletReq.client_reference_id = inputs["TransID"];
                    withdrawGetWalletReq.hashCode = ToMD5("partner_withdraw" + inputs["key"]).ToLower();
                    var message = "";
                    var toAmount = GetExchangeAmount(withdrawGetWalletReq.amount, withdrawGetWalletReq.currency,
                        withdrawGetWalletReq.withdrawal_token, inputs, "0");

                    if (toAmount > 0)
                    {
                        withdrawGetWalletReq.withdrawal_amount = toAmount.ToString();
                        var sResponse1 = HttpPost(getWalletAddress, ToJson(withdrawGetWalletReq), "application/json", depositResp.access_token, inputs["TransID"].ToLower(), true);

                        var withdrawGetWalletResp = new WithdrawGetWalletResp();
                        withdrawGetWalletResp = JsonConvert.DeserializeObject<WithdrawGetWalletResp>(sResponse1);

                        withdrawResult.Message = withdrawGetWalletResp.message;
                        withdrawResult.RawContent = withdrawGetWalletResp.message;
                        if (withdrawGetWalletResp.status == "pending")
                        {
                            withdrawResult.Success = true;
                            withdrawResult.WithdrawalInfo =
                                withdrawalInfo.AddOrSetExchangeInfo((toAmount / amount).ToString(), toAmount.ToString()); ;
                        }
                        else
                        {
                            withdrawResult.Success = false;
                        }
                    }
                    else
                    {
                        withdrawResult.Success = false;
                        oDepositResult.Message = message;
                        oDepositResult.RawContent = message;
                    }
                }
                else
                {
                    oDepositResult.Message = depositResp.message;
                    oDepositResult.RawContent = sResponse;
                }
            }
            catch (WebException wEx)
            {
                var err = FromJson<Dictionary<string, string>>(new StreamReader(wEx.Response?.GetResponseStream()).ReadToEnd());
                if (err.ContainsKey("error"))
                {
                    oDepositResult.Message = $"{err["error"]}-{err["error_message"]}";
                    oDepositResult.RawContent = $"{err["error"]}-{err["error_message"]}";
                }
                if (err.ContainsKey("err"))
                {
                    oDepositResult.Message = $"{err["err"]}";
                    oDepositResult.RawContent = $"{err["err"]}";
                }

                _logger.LogError(wEx.InnerException, wEx.Message);
            }

            return withdrawResult;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawReq>(bodyContent);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.client_reference_id;
            oNotify.Message = data.status;
            oNotify.ThirdId = data.txid;

            if (data.status == "completed")
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.Response = "true";
            }
            else if (data.status == "rejected")
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.Response = "true";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = "true";
            }

            return oNotify;
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
                .PostUrlEncoded<Dictionary<string, string>, DepositResp>(loginUrl, request);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay login failed, HttpStatus: {(int)statusCode} {statusCode}" +
                        $", Message: {result.message}",
                    ErrorCode.GetExchangeRateFailed);
            }

            if (string.IsNullOrEmpty(result.access_token))
            {
                throw new CustomException(
                    "DirePay login response access_token is empty",
                    ErrorCode.GetExchangeRateFailed);
            }

            return result.access_token;
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
            var req = new DepositGetWalletReq
            {
                command = "payment",
                line_items = new[]
                {
                    new LineItem
                    {
                        name = "deposit",
                        item_id = method,
                        description = $"Deposit {payCurrency} via {method}",
                        amount = amount.ToString("#"),
                        quantity = "1"
                    }
                },
                currency = payCurrency,
                token = payCode,
                callback_url = inputs["CallBackUrl"],
                customer_uid = inputs["CustId"],
                client_reference_id = inputs["TransID"],
                hashCode = ToMD5("payment" + inputs["key"]).ToLower()
            };

            var resp = CryptoPayinGetWallet(apiUrl, accessToken, req);
            
            var requestAmount = decimal.Parse(resp.InvoiceAmount) * decimal.Parse(resp.Rates);
            var resData = new Dictionary<string, string>
            {
                {"DepositAmount", resp.InvoiceAmount},
                {"Rate", resp.Rates},
                {"RequestAmount", requestAmount.ToString("#")},
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
                DepositInfo = depositInfo.AddOrSetExchangeInfo(resp.Rates, requestAmount.ToString("#"))
            };
        }
        
        private CryptoPayinResp CryptoPayinGetWallet(string apiUrl, string accessToken, DepositGetWalletReq request)
        {
            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncoded<DepositGetWalletReq, CryptoPayinResp>(apiUrl, request);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay GetWallet failed, HttpStatus: {(int)statusCode} {statusCode}" +
                    $", Message: {result.Message}",
                    ErrorCode.GetExchangeRateFailed);
            }
            
            return result;
        }
        
        
        private DepositResult FiatPayin(IDictionary<string, string> inputs, string method)
        {
            var apiUrl = inputs["host"] + Path.Action;
            var amount = decimal.Parse(inputs["Amount"]);
            var payCurrency = inputs["PayCurrencyId"];
            
            var accessToken = GetAccessToken(inputs);
            var req = new Dictionary<string, string>
            {
                { "command", "payment" },
                { "hashCode", ToMD5("fiat_payment" + inputs["key"]).ToLower() },
                { "callback_url", inputs["CallBackUrl"] },
                { "redirect_url", inputs["ReturnUrl"] },
                { "currency", payCurrency },
                { "method", method },
                { "customer_uid", inputs["CustId"] },
                { "depositor_name", inputs["AccountName"] },
                { "amount", amount.ToString("#") },
                { "txid", inputs["TransID"] }
            };

            if (payCurrency is "BDT" or "IDR")
            {
                req.Add("bank_code", inputs["BankCode"]);
            }

            var (resp, statusCode) = _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncoded<Dictionary<string, string>, FiatPayinResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay fiat_payment failed, HttpStatus: {(int)statusCode} {statusCode}" +
                    $", Message: {resp.Message}");
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
                    { "command", "rates" },
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
                    $"DirePay GetExchangeRate failed, HttpStatus: {(int)statusCode} {statusCode}" +
                        $", Message: {result.message}",
                    ErrorCode.GetExchangeRateFailed);
            }

            var exchangeAmount = decimal.Parse(result.amount);

            return exchangeAmount;
        }

        private static (bool IsFiat, string Method) GetMethod(string payCode)
        {
            var splitStr = payCode.Split('-');
            var isFait = splitStr[0].Equals("fait");

            return (isFait, splitStr.Length > 1 ? splitStr[1] : splitStr[0]);
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
