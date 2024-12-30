using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.MultiPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;
        private const string PayName = "MultiPay";

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        internal string? SignConcatSourceString { get; private set; }

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            try
            {
                var apiKey = inputs["ApiKey"];
                var publicKey = inputs["PublicKey"];
                var host = inputs["Host"];

                var req = new Dictionary<string, object>
                {
                    { "amount", int.Parse(inputs["Amount"]).ToString("F0") },
                    { "callback_url", inputs["CallBackUrl"] },
                    { "channel_code", inputs["PayCode"] },
                    { "order_id", inputs["TransID"] },
                    { "timestamp", long.Parse(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()) }
                };
                req.Add("sign", Encrypt(req, publicKey));

                var apiUrl = $"{host.TrimEnd('/')}{Definition.Path.Deposit}";
                var response = CreateOrder(apiUrl, req, apiKey);
                var result = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Message = response.Message,
                    Url = response.Data.PayUrl
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation($"{PayName} deposit failed, Code:{exception.Code}, Message:{exception.Message}");

                return new DepositResult(false, exception.Code, exception.Message);
            }
        }

        private DepositResponse CreateOrder(string apiUrl, Dictionary<string, object> req, string apiKey)
        {
            var (response, statusCode) = _httpHelper
                .WithHeader("apikey", apiKey)
                .PostJson<Dictionary<string, object>, DepositResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"{PayName} CreateOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.Code != Definition.SuccessCode
                || response.Data.Status == Definition.FailDataStatus)
            {
                throw new CustomException(
                    $"{PayName} CreateOrder failed: expected code is {Definition.SuccessCode} expected status not {Definition.FailDataStatus}, actual code={response.Code} msg={response.Message} data.status={response.Data.Status}");
            }

            return response;
        }

        public override NotifyDepositResult CallBackDebit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var privateKey = payResource["PrivateKey"];
            var parameters = FromJson<Dictionary<string, object>>(bodyContent);

            var (validateResult, _) = ValidateSign(parameters, privateKey);
            if (!validateResult)
            {
                return new NotifyDepositResult
                {
                    DepositId = parameters["order_id"].ToString(),
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var status = parameters["status"].ToString();
            var transStatus = Definition.GetTransStatus(status);
            var result = new NotifyDepositResult
            {
                DepositId = parameters["order_id"].ToString(),
                ThirdId = parameters["platform_order_id"].ToString(),
                Comment = status,
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(parameters["amount"].ToString()),
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
                var apiKey = inputs["ApiKey"];
                var publicKey = inputs["PublicKey"];
                var host = inputs["Host"];

                var req = new Dictionary<string, object>
                {
                    { "amount", int.Parse(inputs["Amount"]).ToString("F0") },
                    { "currency", inputs["PayCurrencyId"] },
                    { "account_name", inputs["AccountName"] },
                    { "card_number", inputs["AccountNo"] },
                    { "bank_code", inputs["IFSCCode"] },
                    { "order_id", inputs["TransID"] },
                    { "channel_code", inputs["PayCode"] },
                    { "timestamp", long.Parse(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()) },
                    { "callback_url", inputs["CallBackUrl"] }
                };
                req.Add("sign", Encrypt(req, publicKey));

                var apiUrl = $"{host.TrimEnd('/')}{Definition.Path.Withdrawal}";
                var res = CreatePayout(apiUrl, req, apiKey);
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

        private WithdrawalResponse CreatePayout(string apiUrl, Dictionary<string, object> req, string apiKey)
        {
            var (response, statusCode) = _httpHelper
                .WithHeader("apikey", apiKey)
                .PostJson<Dictionary<string, object>, WithdrawalResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"{PayName} CreatePayout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.Code != Definition.SuccessCode
                || response.Data.Status == Definition.FailDataStatus)
            {
                throw new CustomException(
                    $"{PayName} CreatePayout failed: expected code is {Definition.SuccessCode} expected status not {Definition.FailDataStatus}, actual code={response.Code} msg={response.Message} data.status={response.Data.Status}");
            }

            return response;
        }

        public override NotifyWithdrawResult CallBackCredit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var privateKey = payResource["PrivateKey"];
            var parameters = FromJson<Dictionary<string, object>>(bodyContent);

            var (validateResult, _) = ValidateSign(parameters, privateKey);
            if (!validateResult)
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = parameters["order_id"].ToString(),
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var status = parameters["status"].ToString();
            var transStatus = Definition.GetTransStatus(status);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = parameters["order_id"].ToString(),
                ThirdId = parameters["platform_order_id"].ToString(),
                TransStatus = transStatus,
                Response = Definition.CallbackResponse,
                Message = status
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

        internal string Encrypt(Dictionary<string, object> parameters, string publicKey)
        {
            var queryString = GetQueryString(parameters);
            SignConcatSourceString = queryString;
            return EncryptData(queryString, publicKey);
        }

        internal (bool, string) ValidateSign(Dictionary<string, object> parameters, string privateKey)
        {
            var queryString = GetQueryString(parameters);
            var signature = parameters["sign"].ToString();
            var decryptData = DecryptData(signature, privateKey);

            return (decryptData == queryString, decryptData);
        }

        private string DecryptData(string signature, string privateKey)
        {
            var privateKeyBytes = Convert.FromBase64String(privateKey);

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _); // Requires .NET 5 or later

            var encryptedBytes = Convert.FromBase64String(signature);
            var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private string EncryptData(string httpQuery, string publicKey)
        {
            var publicKeysBytes = Convert.FromBase64String(publicKey);

            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKeysBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(httpQuery);
            var encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedBytes);
        }

        private string GetQueryString(Dictionary<string, object> parameters)
        {
            var sortedParameters = parameters
                .Where(pair => !pair.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                .OrderBy(pair => pair.Key);
            var queryString = string.Join("&", sortedParameters
                .Select(pair => $"{pair.Key}={pair.Value}"));
            return queryString;
        }
    }
}