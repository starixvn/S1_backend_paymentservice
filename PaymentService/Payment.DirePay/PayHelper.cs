using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;

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

        /*ExchangeResult IExchangeHelper.GetExchange(Dictionary<string, string> payResource)
        {
            var oExchangeResult = new ExchangeResult
            {
                ErrorCode = "9000"
            };

            var from_currency = GetPayCurrency(payResource["PayCurrencyId"]);
            var to_currency = payResource["PayCode"];
            var amount = payResource["Amount"];
            string message;

            var toAmount = getExchange(amount, from_currency, to_currency, payResource, out message);
            if (toAmount > 0)
            {
                var ResData = new Dictionary<string, string>();
                ResData.Add("To_Amount", toAmount.ToString());
                ResData.Add("Rate", (toAmount / decimal.Parse(amount)).ToString());
                ResData.Add("Updated_timestamp", new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString());

                oExchangeResult.FormData = ResData;
                oExchangeResult.ErrorCode = "0";
            }
            else
            {
                oExchangeResult.Message = message;
            }

            return oExchangeResult;
        }*/

        ExchangeResult IExchangeHelper.GetExchange(Dictionary<string, string> input)
        {
            try
            {
                var fromCurrency = GetPayCurrency(input["PayCurrencyId"]);
                var toCurrency = input["PayCode"];
                var amount = input["Amount"];
                var toAmount = GetExchangeAmountAsync(amount, fromCurrency, toCurrency, input).Result;
                var strToAmount = toAmount.ToString();

                if (toAmount > 0)
                {
                    var rateData = new Dictionary<string, string>
                    {
                        { "To_Amount", strToAmount },
                        { "Rate", (toAmount / decimal.Parse(amount)).ToString() },
                        { "Updated_timestamp", new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString() }
                    };

                    return new ExchangeResult(ErrorCode.Success, "success", rateData);
                }

                throw new CustomException("Incorrect exchange amount: " + strToAmount,
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
            var sUrl = inputs["host"];
            var getReceptionToken = sUrl + "/api/login";
            var getWalletAddress = sUrl + "/api/callBack";

            var depositGetH5Req = new Dictionary<string, string>();
            depositGetH5Req.Add("email", inputs["Email"]);
            depositGetH5Req.Add("password", inputs["Password"]);
            var sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var oDepositResult = new DepositResult();
            oDepositResult.Success = false;
            try
            {
                var sResponse = HttpPost(getReceptionToken, sParam, "application/x-www-form-urlencoded",
                    inputs["TransID"].ToLower(), true);

                var depositResp = new DepositResp();
                depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);
                var payCode = inputs["PayCode"];
                var itemId = GetItemId(payCode);

                if (depositResp != null && depositResp.access_token != null)
                {
                    var depositGetWalletReq = new DepositGetWalletReq();
                    depositGetWalletReq.command = "payment";
                    depositGetWalletReq.line_items = new line_items[1];
                    depositGetWalletReq.line_items[0] = new line_items();
                    depositGetWalletReq.line_items[0].name = "deposit";
                    depositGetWalletReq.line_items[0].item_id = itemId;
                    depositGetWalletReq.line_items[0].description = $"Deposit {currMapping(inputs["CurrencyId"])} via {itemId}";
                    var amount = decimal.Parse(inputs["Amount"]);
                    depositGetWalletReq.line_items[0].amount = amount.ToString("#");
                    depositGetWalletReq.line_items[0].quantity = "1";
                    depositGetWalletReq.currency = currMapping(inputs["CurrencyId"]);
                    depositGetWalletReq.token = payCode;
                    depositGetWalletReq.callback_url = inputs["CallBackUrl"];
                    depositGetWalletReq.customer_uid = inputs["CustId"];
                    depositGetWalletReq.client_reference_id = inputs["TransID"];
                    depositGetWalletReq.hashCode = ToMD5("payment" + inputs["key"]).ToLower();
                    var sResponse1 = HttpPost(getWalletAddress, ToJson(depositGetWalletReq), "application/json",
                    depositResp.access_token, inputs["TransID"].ToLower(), true);

                    var depositGetWalletResp = new DepositGetWalletResp();
                    depositGetWalletResp = JsonConvert.DeserializeObject<DepositGetWalletResp>(sResponse1);

                    if (depositGetWalletResp != null && depositGetWalletResp.txid != null)
                    {
                        var ResData = new Dictionary<string, string>();
                        ResData.Add("DepositAmount", depositGetWalletResp.invoice_amount);
                        ResData.Add("Rate", depositGetWalletResp.rates);
                        var requestAmount = decimal.Parse(depositGetWalletResp.invoice_amount) * decimal.Parse(depositGetWalletResp.rates);
                        ResData.Add("RequestAmount", requestAmount.ToString());
                        ResData.Add("Token", depositGetWalletResp.payment_token);

                        if (depositGetWalletResp.enable_promotion)
                        {
                            ResData.Add("Promotion", "1");
                            ResData.Add("PromotionRate", depositGetWalletResp.promotion_rate);
                            ResData.Add("PromotionAmount", depositGetWalletResp.promotion_fiat_amount);
                        }
                        else
                        {
                            ResData.Add("Promotion", "0");
                        }

                        oDepositResult.Success = true;
                        oDepositResult.Url = depositGetWalletResp.payment_address;
                        oDepositResult.ThirdId = depositGetWalletResp.txid;
                        oDepositResult.FormData = ResData;
                    }
                    else
                    {
                        oDepositResult.Message = $"{depositGetWalletResp.message}";
                        oDepositResult.RawContent = sResponse1;
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

            return oDepositResult;
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

            var depositGetH5Req = new Dictionary<string, string>();
            depositGetH5Req.Add("email", inputs["Email"]);
            depositGetH5Req.Add("password", inputs["Password"]);
            var sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            string sResponse = null;

            var oDepositResult = new DepositResult();
            oDepositResult.Success = false;

            var withdrawResult = new WithdrawResult();
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
                    withdrawGetWalletReq.currency = currMapping(inputs["CurrencyId"]);
                    withdrawGetWalletReq.withdrawal_token = inputs["PayCode"];
                    withdrawGetWalletReq.callback_url = inputs["CallBackUrl"];
                    withdrawGetWalletReq.address = inputs["AccountNo"];
                    withdrawGetWalletReq.customer_uid = inputs["CustId"];
                    withdrawGetWalletReq.client_reference_id = inputs["TransID"];
                    withdrawGetWalletReq.hashCode = ToMD5("partner_withdraw" + inputs["key"]).ToLower();
                    var message = "";
                    var toAmount = GetExchangeAmountAsync(withdrawGetWalletReq.amount, withdrawGetWalletReq.currency, withdrawGetWalletReq.withdrawal_token, inputs).Result;

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

        /*private decimal GetExchangeRate(string amount, string from_currency, string to_currency, Dictionary<string, string> inputs, out string message)
        {
            message = "";
            var sUrl = inputs["host"];
            var getReceptionTokenUrl = sUrl + "/api/login";
            var getExchangeUrl = sUrl + "/api/callBack";

            var depositGetTokenReq = new Dictionary<string, string>();
            depositGetTokenReq.Add("email", inputs["Email"]);
            depositGetTokenReq.Add("password", inputs["Password"]);
            var sParam = string.Join("&", depositGetTokenReq.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var oDepositResult = new DepositResult();
            oDepositResult.Success = false;
            try
            {
                _logger.LogInformation("getExchange > getReceptionToken START");
                var sResponse = HttpPost(getReceptionTokenUrl, sParam, "application/x-www-form-urlencoded", "", true);
                var depositResp = new DepositResp();
                depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);
                _logger.LogInformation("getExchange > getReceptionToken END");

                if (depositResp != null && depositResp.access_token != null)
                {

                    _logger.LogInformation("getExchange > getExchange START");
                    var exchangeReq = new ExchangeReq();
                    exchangeReq.command = "rates";
                    exchangeReq.amount = amount.ToString();
                    exchangeReq.from_currency = from_currency;
                    exchangeReq.to_currency = to_currency;
                    exchangeReq.type = "SELL";
                    //_logger.LogInformation("getExchange > getExchange url: " + getExchangeUrl);
                    //_logger.LogInformation("getExchange > getExchange req: " + base.ToJson(exchangeReq));
                    //_logger.LogInformation("getExchange > getExchange token: " + depositResp.access_token);
                    var sResponse1 = HttpPost(getExchangeUrl, ToJson(exchangeReq), "application/json", depositResp.access_token, "", true);
                    _logger.LogInformation("getExchange > getExchange END");
                    var exchangeResp = new ExchangeResp();
                    exchangeResp = JsonConvert.DeserializeObject<ExchangeResp>(sResponse1);

                    if (exchangeResp != null && exchangeResp.amount != null)
                    {
                        return decimal.Parse(exchangeResp.amount.ToString());
                    }
                    else
                    {
                        message = exchangeResp.message;
                    }
                }
            }
            catch (WebException wex)
            {
                message = wex.ToString();
                _logger.LogError(message);
            }
            return 0;
        }*/

        private async Task<string> GetAccessTokenAsync(Dictionary<string, string> inputs)
        {
            var baseApiUrl = inputs["host"];
            var loginUrl = baseApiUrl + Path.Login;

            var request = new Dictionary<string, string>
            {
                { "email", inputs["Email"] },
                { "password", inputs["Password"] }
            };

            var result = await _httpHelper
                .PostUrlEncodedAsync<Dictionary<string, string>, DepositResp>(loginUrl, request);

            if (!result.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay login failed, HttpStatus: {(int)result.StatusCode} {result.StatusCode}" +
                        $", Message: {result.Result.message}",
                    ErrorCode.GetExchangeRateFailed);
            }

            if (string.IsNullOrEmpty(result.Result.access_token))
            {
                throw new CustomException(
                    "DirePay login response access_token is empty",
                    ErrorCode.GetExchangeRateFailed);
            }

            return result.Result.access_token;
        }

        private async Task<decimal> GetExchangeAmountAsync(string amount, string fromCurrency, string toCurrency, Dictionary<string, string> inputs)
        {
            var baseApiUrl = inputs["host"];
            var getExchangeUrl = baseApiUrl + Path.ExchangeRate;

            var accessToken = await GetAccessTokenAsync(inputs);
            var request = new Dictionary<string, string>
                {
                    { "command", "rates" },
                    { "amount", amount },
                    { "from_currency", fromCurrency },
                    { "to_currency", toCurrency },
                    { "type", "SELL" }
                };

            var result = await _httpHelper
                .WithOAuthBearerToken(accessToken)
                .PostUrlEncodedAsync<Dictionary<string, string>, ExchangeResp>(getExchangeUrl, request);

            if (!result.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DirePay GetExchangeRate failed, HttpStatus: {(int)result.StatusCode} {result.StatusCode}" +
                        $", Message: {result.Result.message}",
                    ErrorCode.GetExchangeRateFailed);
            }

            var exchangeAmount = decimal.Parse(result.Result.amount);

            return exchangeAmount;
        }

        private string GetItemId(string payCode)
        {
            var splitStr = payCode.Split('-');

            return splitStr[1];
        }

        private string GetPayCurrency(string payCurrencyId)
        {
            var splitStr = payCurrencyId.Split('_');

            return splitStr[0];
        }

        #endregion
    }
}
