using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.MbmPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;
        internal string? SignConcatSourceString { get; private set; }

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region Debit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            try
            {
                var merchantId = inputs["MerchantId"];
                var key = inputs["Key"];
                var host = inputs["Host"];

                var req = new Dictionary<string, string>
                {
                    { "mchId", merchantId },
                    { "signType", "md5" },
                    { "mchOrderNo", inputs["TransID"] },
                    { "amount", decimal.Parse(inputs["Amount"]).ToString("F0") },
                    { "channelType", inputs["PayCode"] },
                    { "terminalType", inputs["DeviceType"].Equals("1") ? "PC" : "MOBILE" },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "rand", GetRandomString() }
                };
                req.Add("sign", Sign(req, key));

                var apiUrl = $"{host.TrimEnd('/')}{Definition.Path.Deposit}";
                var res = CreateOrder(apiUrl, req);
                var result = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Message = res.Message,
                    Url = res.Data
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation($"MbmPay deposit failed, Code:{exception.Code}, Message:{exception.Message}");

                return new DepositResult(false, exception.Code, exception.Message);
            }
        }

        private DepositResponse CreateOrder(string apiUrl, Dictionary<string,string> req)
        {
            var response = MbmPayRequest<DepositResponse>("CreateOrder", apiUrl, req);

            return response;
        }

        public override NotifyDepositResult CallBackDebit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var key = payResource["Key"];
            if (!ValidateSign(formData, formData["sign"], key))
            {
                return new NotifyDepositResult
                {
                    DepositId = formData["mchOrderNo"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetDebitTransStatus(formData["status"]);
            var result = new NotifyDepositResult
            {
                DepositId = formData["mchOrderNo"],
                ThirdId = formData["orderId"],
                Comment = formData["status"],
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(formData["amountApply"]),
                Response = Definition.CallbackResponse
            };

            return result;
        }

        internal bool ValidateSign(Dictionary<string,string> parameters, string sign, string key)
        {
            var calculatedSign = Sign(parameters, key);

            return calculatedSign.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var merchantId = inputs["MerchantId"];
                var key = inputs["Key"];
                var host = inputs["Host"];

                var req = new Dictionary<string, string>
                {
                    { "mchId", merchantId },
                    { "signType", "md5" },
                    { "mchOrderNo", inputs["TransID"] },
                    { "amount", decimal.Parse(inputs["Amount"]).ToString("F0") },
                    { "channelType", inputs["PayCode"] },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "bankName", inputs["BankName"] },
                    { "bankCode", inputs["BankCode"] },
                    { "cardName", inputs["AccountName"] },
                    { "cardNo", inputs["AccountNo"] },
                    { "rand", GetRandomString() }
                };
                req.Add("sign", Sign(req, key));

                var apiUrl = $"{host.TrimEnd('/')}{Definition.Path.Withdrawal}";
                var res = CreatePayout(apiUrl, req);
                var result = new WithdrawResult
                {
                    Success = true,
                    Message = res.Message
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new WithdrawResult(exception.Message);
            }
        }

        private WithdrawalResponse CreatePayout(string apiUrl, Dictionary<string, string> req)
        {
            var response = MbmPayRequest<WithdrawalResponse>("CreatePayout", apiUrl, req);

            return response;
        }

        public override NotifyWithdrawResult CallBackCredit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var key = payResource["Key"];
            if (!ValidateSign(formData, formData["sign"], key))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = formData["mchOrderNo"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetCreditTransStatus(formData["status"]);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = formData["mchOrderNo"],
                ThirdId = formData["orderId"],
                TransStatus = transStatus,
                Message = formData["status"],
                Response = Definition.CallbackResponse
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

        internal string Sign(Dictionary<string,string> parameters, string key)
        {
            var queryString = GetQueryString(parameters);
            var signConcatString = $"{queryString}&key={key}";
            SignConcatSourceString = signConcatString;

            var result = ToMD5(signConcatString).ToLower();
            _logger.LogInformation($"SignConcatSourceString:{signConcatString} Sign result:{result}");

            return result;
        }

        private string GetQueryString(Dictionary<string,string> parameters)
        {
            var sortedParameters = parameters
                .Where(pair => !pair.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                .Where(pair => pair.Value != null && pair.Value.Any())
                .OrderBy(pair => pair.Key);
            var queryString = string.Join("&", sortedParameters
                .Select(param => $"{param.Key}={param.Value}"));
            return queryString;
        }

        internal virtual string GetRandomString()
        {
            return base.GetRandomString4(32);
        }

        private T MbmPayRequest<T>(string action, string apiUrl, Dictionary<string,string> req)
            where T : MbmPayResponseBase
        {
            
            var (response, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, T>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"MbmPay {action} failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.Code != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"MbmPay {action} failed: expected code={Definition.SuccessCode}, actual code={response.Code} msg={response.Message}");
            }

            return response;
        }
    }
}
