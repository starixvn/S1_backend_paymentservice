using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using System.Web;

namespace Payment.PAPay
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

        #region Credit 提款

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"];
            var apiKey = inputs["key"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("#");
            var currencyId = inputs["PayCurrencyId"];
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var bankCode = inputs["BankCode"];
            var merchantCode = inputs["merchantCode"];
            var notifyUrl = inputs["CallBackUrl"];

            var extra = JsonConvert.SerializeObject(new PayoutExtraModel()
            {
                account_num = inputs["AccountNo"],
                bank_name = inputs["BankName"],
            });

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"mch_id", merchantCode},
                    {"out_trade_no", transId},
                    {"account_code", bankCode}, //
                    {"account_name", accountName},
                    {"agent_amount", strAmount},
                    {"currency", currencyId}, //
                    {"extra", extra}, //
                    {"notify_url", notifyUrl},
                    {"nonce_str", Guid.NewGuid().ToString("N")},
                };

                req.Add("sign", GetSign(req, apiKey));

                var res = Payout(apiUrl, req).Result;
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            //var apiKey = payResource["key"];
            //var req = FromJson<WithdrawalCallBackRequest>(ToJson(formData));

            //if (!VerifySign(formData, apiKey, req.sign))
            //{
            //    return new NotifyWithdrawResult
            //    {
            //        TransStatus = TransStatus.Pending,
            //        Response = CallbackResponse.Failed
            //    };
            //}

            //var transStatus = PattayaPayDefinition.GetPayoutTransStatus(req.status);
            //var result = new NotifyWithdrawResult
            //{
            //    ThirdId = req.transferno,
            //    TransStatus = transStatus,
            //    Response = CallbackResponse.Success,
            //    WithdrawId = req.outtransferno
            //};

            //return result;
            return null;
        }

        /// <summary>
        /// NoUse
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Debit 存款

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"];
            var apiKey = inputs["key"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("#.##");
            var transId = inputs["TransID"];
            var merchantCode = inputs["merchantCode"];
            var notifyUrl = inputs["CallBackUrl"];
            var payCode = inputs["PayCode"];
            var ip = inputs["ClientIP"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"mch_id", merchantCode},
                    {"pay_type", payCode},
                    //{"describe", payCode}, //
                    {"out_trade_no", transId},
                    //{"attach", notifyUrl}, //
                    {"total_fee", strAmount},
                    //{"back_url", notifyUrl}, //
                    {"notify_url", notifyUrl},
                    {"ip", ip},
                    {"nonce_str", Guid.NewGuid().ToString("N") },
                    //{"extra", notifyUrl} //
                };

                req.Add("sign", GetSign(req, apiKey));

                var res = CreateOrder(apiUrl, req).Result;

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.PayUrl,
                    ThirdId = res.OrderId
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var apiKey = payResource["key"];
            var req = FromJson<DepositCallBackRequest>(bodyContent);

            if (!VerifySign(FromJson<Dictionary<string, string>>(bodyContent), apiKey, req.sign))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            var transStatus = PAPayDefinition.GetDepositTransStatus(req.code);
            var result = new NotifyDepositResult
            {
                ThirdId = req.order_id,
                DepositId = req.out_trade_no,
                Comment = req.code_msg,
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(req.total_fee),
                Response = CallbackResponse.Success
            };

            return result;
        }

        /// <summary>
        /// NoUse
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #region Private Methods

        private async Task<PAPayCreateOrderResponse> CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreatePay;

            var (result, statusCode) = await _httpHelper
                .PostJsonAsync<Dictionary<string, string>, PAPayCreateOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"PAPay CreatOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Status != "200")
                throw new CustomException($"PAPay CreatOrder response error: {result.Status} {HttpUtility.UrlDecode(result.Message)}");

            return result;
        }

        private async Task<PAPayPayoutResponse> Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreatePayout;
            var (result, statusCode) = await _httpHelper
                .PostJsonAsync<Dictionary<string, string>, PAPayPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"PAPay Payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Status != "200")
                throw new CustomException(
                    $"PAPay Payout response error: {result.Status} {HttpUtility.UrlDecode(result.Message)}");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var str = string.Join("&", param
                .Where(x => !x.Key.Equals("sign"))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

            _logger.LogInformation(str);
            var sign = ToMD5(str).ToUpper();
            _logger.LogInformation(sign);
            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);
            return sign.Equals(target);
        }

        #endregion

    }
}