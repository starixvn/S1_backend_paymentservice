using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PomPay
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
                var host = inputs["Host"];
                var clientId = inputs["ClientId"];
                var clientSecret = inputs["ClientSecret"];

                var req = new Dictionary<string, string>
                {
                    { "clientId", clientId },
                    { "transactionId", inputs["TransID"] },
                    { "custName", inputs["AccountName"] },
                    { "amount", decimal.Parse(inputs["Amount"]).ToString("F2") },
                    { "callbackUrl", inputs["CallBackUrl"] },
                    { "paymentMethod", inputs["PayCode"] },
                    { "bankAcc", inputs["PayAccount"] },
                    { "custBank", inputs["BankCode"] }
                };
                req.Add("hashVal", Hash("Debit", req, clientId, clientSecret));
                var url = $"{host.TrimEnd('/')}{Definition.Path.Deposit}";
                var res = CreateOrder(url, req);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.QrString,
                    ThirdId = res.ReferenceId
                };
            }
            catch (CustomException exception)
            {
                _logger.LogInformation($"OneWallet deposit failed, Code:{exception.Code}, Message:{exception.Message}");

                return new DepositResult(false, exception.Code, exception.Message);
            }
        }

        private DepositResponse CreateOrder(string url, Dictionary<string, string> req)
        {
            var (response, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, DepositResponse>(url, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"PomPay CreateOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.ErrorCode != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"PomPay CreateOrder failed: expected ErrorCode={Definition.SuccessCode}, actual ErrorCode={response.ErrorCode} Message={response.Message}");
            }

            return response;
        }

        public override NotifyDepositResult CallBackDebit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var clientId = payResource["ClientId"];
            var clientSecret = payResource["ClientSecret"];

            if (!ValidateHash("CallBackDebit", formData, formData["hashVal"], clientId, clientSecret))
            {
                return new NotifyDepositResult
                {
                    DepositId = formData["transactionId"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetDepositTransStatus(formData["status"]);
            var result = new NotifyDepositResult
            {
                ThirdId = formData["referenceId"],
                DepositId = formData["transactionId"],
                Comment = formData["status"],
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(formData["amount"]),
                Response = Definition.CallbackResponse
            };

            return result;
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
                var host = inputs["Host"];
                var clientId = inputs["ClientId"];
                var clientSecret = inputs["ClientSecret"];

                var req = new Dictionary<string, string>
                {
                    { "clientId", clientId },
                    { "transactionId", inputs["TransID"] },
                    { "custName", inputs["AccountName"] },
                    { "custBank", inputs["BankCode"] },
                    { "custBankAcc", inputs["AccountNo"] },
                    { "amount", decimal.Parse(inputs["Amount"]).ToString("F2") },
                    { "callbackUrl", inputs["CallBackUrl"] }
                };
                req.Add("hashVal", Hash("Credit", req, clientId, clientSecret));

                var url = $"{host.TrimEnd('/')}{Definition.Path.Withdrawal}";
                var res = CreatePayout(url, req);
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

        private WithdrawalResponse CreatePayout(string url, Dictionary<string, string> req)
        {
            var (response, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, WithdrawalResponse>(url, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"PomPay CreatePayout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!response.Status.Equals(Definition.CreditSuccessStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new CustomException(
                    $"PomPay CreatePayout failed: expected Status={Definition.CreditSuccessStatus}, actual Status={response.Status} ErrorCode={response.ErrorCode} Message={response.Message}");
            }

            return response;
        }

        public override NotifyWithdrawResult CallBackCredit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var clientId = payResource["ClientId"];
            var clientSecret = payResource["ClientSecret"];

            var req = FromJson<Dictionary<string, string>>(bodyContent);

            if (!ValidateHash("CallBackCredit", req, req["hashVal"], clientId, clientSecret))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req["transactionId"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetWithdrawalTransStatus(req["status"]);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = req["transactionId"],
                ThirdId = req["referenceId"],
                TransStatus = transStatus,
                Response = Definition.CallbackResponse,
                Message = req["status"]
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

        internal string Hash(string type, Dictionary<string, string> parameters, string clientId, string clientSecret)
        {
            var hashKeys = Definition.HashKeys[type];

            var signConcatString = string.Join("", hashKeys
                .Select(key => parameters.ContainsKey(key) ? parameters[key] : string.Empty));
            signConcatString = $"{clientId}{signConcatString}{clientSecret}";

            SignConcatSourceString = signConcatString;
            var result = ToSHA256(signConcatString).ToLower();

            _logger.LogInformation($"SignConcatSourceString:{signConcatString} Hash result:{result}");

            return result;
        }

        public bool ValidateHash(
            string type,
            Dictionary<string, string> parameters,
            string hash,
            string clientId,
            string clientSecret)
        {
            var calculatedHash = Hash(type, parameters, clientId, clientSecret);
            return calculatedHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
