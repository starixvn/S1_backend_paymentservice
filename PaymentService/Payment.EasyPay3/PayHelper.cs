using Flurl.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using static Payment.EasyPay3.Definition;

namespace Payment.EasyPay3
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }


        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var apiUrl = input["Host"];
            var apiToken = input["Apikey"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var notifyUrl = input["CallBackUrl"];
            var payCode = input["PayCode"];
            var depositInfo = input["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"Apikey", apiToken},
                    {"chargeType", payCode},
                    {"callback", notifyUrl},
                    {"amount", strAmount},
                    {"requestId", transId}
                };

                var res = CreateOrder(apiUrl, req);


                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data.QrUrl,
                    ThirdId = res.Data.Id.ToString(),
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
            var depositInfo = input["DepositInfo"];
            var transId = input["TransID"];

            var jObj = JObject.Parse(depositInfo);

            if (!jObj.TryGetValue("PayCode", StringComparison.OrdinalIgnoreCase, out var jPayCode))
            {
                _logger.LogInformation($"{transId} DepositInfo has no PayCode information.");

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Failed."
                };
            }

            var payCode = jPayCode.ToObject<string>();
            var apiToken = "";
            var callbackToken = "";
            var req = FromJson<CallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                {"amount", req.Amount},
                {"out_trade_no", req.TransId},
                {"state", req.State},
                {"trade_no", req.ThirdId}
            };

            if (!VerifySign(req.Sign, dic, apiToken, callbackToken))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = req.ThirdId,
                DepositId = req.TransId,
                Comment = req.State,
                TransStatus = req.State.Equals("completed") ? TransStatus.Success : TransStatus.Pending,
                ActualAmount = decimal.Parse(req.Amount),
                Response = Definition.CallbackResponse
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
            var apiUrl = inputs["Host"];
            var apiToken = inputs["Card"];
            var callbackToken = inputs["CardCallbackToken"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];

            try
            {

                var req = new Dictionary<string, string>
                {
                    {"account_number", accountNo},
                    {"amount", strAmount},
                    {"bank_id", bankCode},
                    {"bank_owner", accountName},
                    {"callback_url", notifyUrl},
                    {"out_trade_no", transId},
                    {"verify_channel_no", "1"}
                };

                req.Add("sign", GetSign(req, apiToken, callbackToken));

                var res = Payout(apiUrl, apiToken, req);
                var response = new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(res)
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
            var apiToken = payResource["Card"];
            var callbackToken = payResource["CardCallbackToken"];
            var req = FromJson<CallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                {"amount", req.Amount},
                {"out_trade_no", req.TransId},
                {"state", req.State},
                {"trade_no", req.ThirdId}
            };

            if (!VerifySign(req.Sign, dic, apiToken, callbackToken))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetTransStatus(req.State);
            var result = new NotifyWithdrawResult
            {
                ThirdId = req.ThirdId,
                TransStatus = transStatus,
                Response = Definition.CallbackResponse,
                WithdrawId = req.TransId,
                Message = req.State
            };

            return result;
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

        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Definition.Path.CreateOrder;
            var parameters = string.Join("&", req.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var (result, statusCode) = _httpHelper
                 .Get<CreatOrderResponse>(apiUrl, parameters);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"EasyPay3 creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }
            if (!ErrorCode.Success.Equals(result.Status))
            {
                throw new CustomException(result.Msg, $"{result.Status}/{result.ExtraStatus}");
            }

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, string token, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Definition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(token)
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"EasyPay3 create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Success)
                return result;

            var str = JsonConvert.SerializeObject(result.Errors);
            var msg = System.Text.RegularExpressions.Regex.Unescape(str);

            throw new CustomException(msg);
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiToken, string callbackToken)
        {
            var str = string.Join("&", param
                .Select(x => $"{x.Key}={x.Value}")) + apiToken + callbackToken;

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToLower();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string apiToken, string callbackToken)
        {
            var sign = GetSign(param, apiToken, callbackToken);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}