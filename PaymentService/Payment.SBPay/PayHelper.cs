using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.SBPay
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
            var payCode = input["PayCode"];
            var (method, code) = GetMethod(payCode);
            var apiToken = input[method];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var notifyUrl = input["CallBackUrl"];
            var depositInfo = input["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"amount", strAmount},
                    {"callback_url", notifyUrl},
                    {"out_trade_no", transId}
                };

                if (payCode.Equals("OnlineBanking"))
                {
                    req.Add("bank", input["BankCode"]);
                }
                
                if (method.Equals("QR"))
                {
                    req.Add("bank", input["code"]);
                }

                var res = CreateOrder(apiUrl, apiToken, req);

                depositInfo = depositInfo.AddOrSetJProperty("PayCode", payCode);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data.Url,
                    ThirdId = res.Data.ThirdId,
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
                _logger.LogError($"{transId} DepositInfo has no PayCode information.");

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Failed."
                };
            }

            var payCode = jPayCode.ToObject<string>();
            var (method, _) = GetMethod(payCode);
            var apiToken = input[method];
            var callbackToken = input[$"{method}_CallbackToken"];
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
                TransStatus = TransStatus.Success, // SBPay只回調成功的單
                ActualAmount = decimal.Parse(req.Amount),
                Response = SBPayDefinition.CallbackResponse
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
            var callbackToken = inputs["Card_CallbackToken"];
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
            var callbackToken = payResource["Card_CallbackToken"];
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

            var transStatus = SBPayDefinition.GetTransStatus(req.State);
            var result = new NotifyWithdrawResult
            {
                ThirdId = req.ThirdId,
                TransStatus = transStatus,
                Response = SBPayDefinition.CallbackResponse,
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

        private CreatOrderResponse CreateOrder(string baseApiUrl, string token, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + SBPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(token)
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"SBPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Success)
                throw new CustomException("SBPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, string token, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + SBPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithOAuthBearerToken(token)
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"SBPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Success)
                throw new CustomException("SBPay create payout failed.");

            return result;
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

        private static (string method, string code) GetMethod(string payCode)
        {
            var result = payCode.Split('-');

            return (result[0], result.Length > 0 ? result[1] : string.Empty);
        }

        #endregion
    }
}