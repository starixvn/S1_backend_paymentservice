using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.CommsPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private string _currentDateTimeOffset;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        internal string CurrentTimeStamp
        {
            get => _currentDateTimeOffset ?? DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            set => _currentDateTimeOffset = value;
        }

        internal string SignConcatSourceString { get; private set; }

        public override NotifyWithdrawResult CallBackCredit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var apiKey = payResource["ApiKey"];
            var secretKey = payResource["SecretKey"];

            var requestRaw = FromJson<CallBackReqRaw>(bodyContent);
            Dictionary<string, string> request;
            try
            {
                var decryptString = Decrypt(requestRaw.Key, apiKey, secretKey);
                request = FromJson<Dictionary<string, string>>(decryptString);
            }
            catch (Exception)
            {
                _logger.LogInformation($"decrypt fail, raw Key:{requestRaw.Key}");
                return new NotifyWithdrawResult
                {
                    WithdrawId = payResource["TransId"],
                    TransStatus = TransStatus.Pending,
                    Response = "Decrypt failed"
                };
            }

            var transStatus = Definition.GetTransStatus(request["transaction_status"]);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = request["transaction_code"],
                ThirdId = request["transaction_no"],
                TransStatus = transStatus,
                Response = Definition.CallBackResponse,
                Message = request["transaction_status"]
            };

            return result;
        }

        public override NotifyDepositResult CallBackDebit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var apiKey = payResource["ApiKey"];
            var secretKey = payResource["SecretKey"];

            var requestRaw = FromJson<CallBackReqRaw>(bodyContent);
            Dictionary<string, string> request;
            try
            {
                var decryptString = Decrypt(requestRaw.Key, apiKey, secretKey);
                request = ConvertToRequest(decryptString);
            }
            catch (Exception)
            {
                _logger.LogInformation($"decrypt fail, raw Key:{requestRaw.Key}");
                return new NotifyDepositResult
                {
                    DepositId = payResource["TransID"],
                    TransStatus = TransStatus.Pending,
                    Response = "Decrypt failed"
                };
            }
            
            var transStatus = Definition.GetTransStatus(request["transaction_status"]);
            return new NotifyDepositResult
            {
                ThirdId = requestRaw.TransactionNo,
                DepositId = requestRaw.TransactionCode,
                Comment = request["transaction_status"],
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(request["transaction_actual_amount"]),
                Response = Definition.CallBackResponse
            };
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var host = inputs["Host"];
            var merchantCode = inputs["MerchantCode"];
            var apiKey = inputs["ApiKey"];
            var secretKey = inputs["SecretKey"];

            try
            {
                var request = new WithdrawalPost
                {
                    MerchantCode = merchantCode,
                    TransactionCode = inputs["TransID"],
                    TransactionTimestamp = CurrentTimeStamp,
                    PayoutCode = inputs["PayCode"],
                    TransactionAmount = double.Parse(inputs["Amount"]),
                    UserId = inputs["CustId"],
                    CurrencyCode = inputs["PayCurrencyId"],
                    BankAccountNumber = inputs["AccountNo"],
                    IfscCode = inputs["IFSCCode"],
                    BankCode = inputs["BankCode"],
                    BankName = inputs["BankName"],
                    AccountName = inputs["AccountName"],
                    CallbackUrl = inputs["CallBackUrl"]
                };

                var signConcatSourceString = ToJson(request);
                SignConcatSourceString = signConcatSourceString;

                var key = Encrypt(signConcatSourceString, apiKey, secretKey);
                var url = $"{host.TrimEnd('/')}/api/v1/payout/{merchantCode}";

                var withdrawalResponse = CreatePayout(url, key);
                var result = new WithdrawResult
                {
                    Success = true,
                    Message = withdrawalResponse.Message
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new WithdrawResult(exception.Message);
            }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var host = inputs["Host"];
            var merchantCode = inputs["MerchantCode"];
            var apiKey = inputs["ApiKey"];
            var secretKey = inputs["SecretKey"];

            var request = new Dictionary<string, string>
            {
                { "merchant_code", merchantCode },
                { "merchant_api_key", apiKey },
                { "transaction_code", inputs["TransID"] },
                { "transaction_timestamp", CurrentTimeStamp },
                { "transaction_amount", int.Parse(inputs["Amount"]).ToString("F0") },
                { "payment_code", inputs["PayCode"] },
                { "user_id", inputs["CustId"] },
                { "currency_code", inputs["PayCurrencyId"] },
                { "bank_code", string.Empty },
                { "callback_url", inputs["CallBackUrl"] },
                { "return_url", inputs["ReturnUrl"] },
                { "random_bank_code", string.Empty }
            };

            var signConcatSourceString = ConvertToQueryString(request);
            SignConcatSourceString = signConcatSourceString;

            var key = Encrypt(signConcatSourceString, apiKey, secretKey);
            var url = $"{host.TrimEnd('/')}/{merchantCode}/v2/dopayment?key={key}";

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = url
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private static string Decrypt(string source, string apiKey, string secretKey)
        {
            var (key, iv) = GetKeyBytes(apiKey, secretKey);

            var encryptor = Aes.Create();
            encryptor.Mode = CipherMode.CBC;
            encryptor.Key = key;
            encryptor.IV = iv;

            using var memoryStream = new MemoryStream();
            using var aesDecryptor = encryptor.CreateDecryptor();
            using var cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

            var cipherBytes = Convert.FromBase64String(Uri.UnescapeDataString(source));
            cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
            cryptoStream.FlushFinalBlock();
            var plainBytes = memoryStream.ToArray();

            return Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length);
        }

        private static string Encrypt(string plainText, string apiKey, string secretKey)
        {
            var (key, iv) = GetKeyBytes(apiKey, secretKey);

            var encryptor = Aes.Create();
            encryptor.Mode = CipherMode.CBC;
            encryptor.Key = key;
            encryptor.IV = iv;

            using var memoryStream = new MemoryStream();
            using var aesEncryptor = encryptor.CreateEncryptor();
            using var cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
            var cipherBytes = memoryStream.ToArray();
            var cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

            return Uri.EscapeDataString(cipherText);
        }

        private static (byte[] key, byte[] iv) GetKeyBytes(string apiKey, string secretKey)
        {
            var sha256 = SHA256.Create();
            var key = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));

            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
            var ivSource = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower()[..16];
            var iv = Encoding.UTF8.GetBytes(ivSource);

            return (key, iv);
        }

        private string ConvertToQueryString(Dictionary<string, string> input)
        {
            var result = string.Join('&', input
                .Select(pair => $"{pair.Key}={pair.Value}"));

            _logger.LogInformation(result);
            return result;
        }

        private Dictionary<string, string> ConvertToRequest(string decryptString)
        {
            _logger.LogInformation(decryptString);
            return decryptString
                .Split('&')
                .Select(item => item.Split('='))
                .ToDictionary(str => str[0], str => str[1]);
        }

        private WithdrawalResponse CreatePayout(string url, string key)
        {
            var data = new WithdrawalPostRaw
            {
                Key = key
            };
            var (response, _) = _httpHelper
                .PostJson<WithdrawalPostRaw, WithdrawalResponse>(url, data);

            if (!response.Success)
            {
                throw new CustomException(
                    $"CommsPay create payout failed: Success={response.Success}, Message={response.Message}");
            }

            return response;
        }
    }
}
