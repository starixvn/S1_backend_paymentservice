using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.OneWallet
{
    public class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            provider.GetRequiredService<IFlurlHttpHelper>();
        }

        protected PayHelper()
        {
        }

        #region Debit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            try
            {
                var key = inputs["Key"];
                var host = inputs["Host"];

                var req = new DepositPost
                {
                    ShOrderNo = inputs["TransID"],
                    OrderAmount = decimal.Parse(inputs["Amount"]),
                    OrderType = inputs["PayCode"],
                    BankCode = inputs["BankCode"]
                };

                var url = $"{host.TrimEnd('/')}{Path.Deposit}";
                var res = CreateOrder(url, req, key);
                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data.Url.First()
                };
            }
            catch (CustomException exception)
            {
                _logger.LogInformation($"OneWallet deposit failed, Code:{exception.Code}, Message:{exception.Message}");

                return new DepositResult(false, exception.Code, exception.Message);
            }
        }

        private DepositResponse CreateOrder(string url, DepositPost req, string key)
        {
            var encryptString = Encrypt(req, key);

            var (receiveModel, errorMessage) =
                OneWalletRequest<DepositResponse>("CreateOrder", url, key, encryptString);

            if (receiveModel?.ErrorCode != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"OneWallet CreateOrder failed: expected ErrorCode={Definition.SuccessCode}, actual ErrorCode={receiveModel?.ErrorCode} Data.Message={receiveModel?.Data.Message ?? errorMessage}");
            }

            return receiveModel;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var key = payResource["Key"];
            var (decryptSuccess, decryptString, receiveModel, errorMessage) =
                TryDecrypt<DepositCallBackRequest>(bodyContent, key);
            _logger.LogInformation($"{decryptSuccess} decrypt rawResponse:{bodyContent} decryptString:{decryptString}");

            if (!decryptSuccess)
            {
                throw new CustomException(
                    $"OneWallet CallBackDebit failed to decrypt rawResponse:{bodyContent}");
            }

            var transStatus = Definition.GetTransStatus(receiveModel.OrderStatus);
            var result = new NotifyDepositResult
            {
                ThirdId = receiveModel.ShOrderNo,
                DepositId = receiveModel.ShOrderNo,
                Comment = receiveModel.OrderStatus,
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(receiveModel.PaidAmount),
                Response = Encrypt(new CallbackResponse(), key)
            };

            return result;
        }
        #endregion

        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var key = inputs["Key"];
                var host = inputs["Host"];

                var req = new WithdrawalPost
                {
                    ShOrderNo = inputs["TransID"],
                    BankCode = inputs["BankCode"],
                    BankAccount = inputs["AccountNo"],
                    BankUserName = inputs["AccountName"],
                    OrderAmount = decimal.Parse(inputs["Amount"]),
                    NotifyUrl = inputs["CallBackUrl"],
                    OperateType = inputs["PayCode"]
                };

                var url = $"{host.TrimEnd('/')}{Path.Withdrawal}";
                var res = CreatePayout(url, req, key);
                var result = new WithdrawResult
                {
                    Success = true,
                    Message = res.ErrorCode
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new WithdrawResult(exception.Message);
            }
        }

        private WithdrawalResponse CreatePayout(string url, WithdrawalPost req, string key)
        {
            var encryptString = Encrypt(req, key);

            var (receiveModel, errorMessage) =
                OneWalletRequest<WithdrawalResponse>("CreatePayout", url, key, encryptString);

            if (receiveModel?.ErrorCode != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"OneWallet CreatePayout failed: expected ErrorCode={Definition.SuccessCode}, actual ErrorCode={receiveModel?.ErrorCode}");
            }

            return receiveModel;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var key = payResource["Key"];
            var (decryptSuccess, decryptString, receiveModel, errorMessage) =
                TryDecrypt<WithdrawalCallBackRequest>(bodyContent, key);
            _logger.LogInformation($"{decryptSuccess} decrypt rawResponse:{bodyContent} decryptString:{decryptString}");

            if (!decryptSuccess)
            {
                throw new CustomException(
                    $"OneWallet CallBackCredit failed to decrypt rawResponse:{bodyContent}");
            }

            var transStatus = Definition.GetTransStatus(receiveModel.OrderStatus);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = receiveModel.ShOrderNo,
                ThirdId = receiveModel.ShOrderNo,
                TransStatus = transStatus,
                Response = Encrypt(new CallbackResponse(), key),
                Message = receiveModel.OrderStatus
            };

            return result;
        }
        #endregion

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        internal virtual string GetRandomBase64String()
        {
            var rand = new Random();
            var bytes = new byte[16];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        internal string Encrypt<T>(T parameters, string key)
            where T : class
        {
            var sourceString = ToJson(parameters);
            return AesEncrypt(sourceString, key, GetRandomBase64String());
        }

        private static string AesEncrypt(string sourceString, string key, string iv)
        {
            var toEncryptArray = Encoding.UTF8.GetBytes(sourceString);

            var rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.KeySize = 128;
            rijndaelManaged.BlockSize = 128;
            rijndaelManaged.Key = Convert.FromBase64String(key);
            rijndaelManaged.IV = Convert.FromBase64String(iv);
            rijndaelManaged.Mode = CipherMode.CBC;
            rijndaelManaged.Padding = PaddingMode.PKCS7;

            var cryptoTransform = rijndaelManaged.CreateEncryptor();
            var resultArray = cryptoTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            var data = new AesModel
            {
                iv = iv,
                value = Convert.ToBase64String(resultArray, 0, resultArray.Length)
            };
            var serialData = JsonConvert.SerializeObject(data);
            var result = Convert.ToBase64String(Encoding.UTF8.GetBytes(serialData));

            return result;
        }

        private (T? receiveModel, string errorMessage) OneWalletRequest<T>(string action, string url, string key, string encryptString)
            where T : class
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(encryptString, Encoding.UTF8, "text/plain");
                var responseResult = client.PostAsync(url, content).Result;

                if (responseResult.IsSuccessStatusCode)
                {
                    var responseBody = responseResult.Content.ReadAsStringAsync().Result;
                    var (decryptSuccess, decryptString, receiveModel, errorMessage) = TryDecrypt<T>(responseBody, key);
                    _logger.LogInformation($"{decryptSuccess} decrypt rawResponse:{responseBody} decryptString:{decryptString}");

                    if (!decryptSuccess)
                    {
                        throw new CustomException(
                            $"OneWallet {action} failed to decrypt rawResponse:{responseBody}");
                    }

                    return (receiveModel, errorMessage);
                }

                throw new CustomException(
                    $"OneWallet {action} failed, HttpStatus: {(int)responseResult.StatusCode} {responseResult.StatusCode}");
            }
        }

        internal (bool decryptSuccess, string decryptString, TReceive? receiveModel, string errorMessage) TryDecrypt<TReceive>(string rawResponse, string key)
            where TReceive : class
        {
            var decryptString = string.Empty;
            TReceive? receiveModel = null;
            var errorMessage = string.Empty;
            try
            {
                decryptString = Decrypt(rawResponse, key);
                receiveModel = FromJson<TReceive>(decryptString);
                return (true, decryptString, receiveModel, errorMessage);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return (false, decryptString, receiveModel, errorMessage);
            }
        }

        private string Decrypt(string encryptedSource, string key)
        {
            var encoding = new UTF8Encoding();
            var jsonString = encoding.GetString(Convert.FromBase64String(encryptedSource));
            var rawModel = JsonConvert.DeserializeObject<RawModel>(jsonString);

            if (rawModel.ErrorCode != null)
            {
                var message = rawModel.Data.Message;
                throw new ArgumentException(message);
            }

            return AESDecrypt(rawModel.Iv, rawModel.Value, key);
        }

        private string AESDecrypt(string iv, string value, string key)
        {
            var result = string.Empty;
            var cypher = Convert.FromBase64String(value);
            var base64Key = Convert.FromBase64String(key);
            var base64Iv = Convert.FromBase64String(iv);

            using (var rj = new RijndaelManaged())
            {
                try
                {
                    rj.Padding = PaddingMode.PKCS7;
                    rj.Mode = CipherMode.CBC;
                    rj.KeySize = 128;
                    rj.BlockSize = 128;
                    rj.Key = base64Key;
                    rj.IV = base64Iv;

                    var ms = new MemoryStream(cypher);

                    using (var cs = new CryptoStream(ms, rj.CreateDecryptor(base64Key, base64Iv), CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            var tmp = sr.ReadLine();
                            while (tmp != null)
                            {
                                result = result.Length > 0 ? result + "\n" + tmp : tmp;
                                tmp = sr.ReadLine();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("DecryptRJ128 Error:" + exception);
                }
                finally
                {
                    rj.Clear();
                }
            }

            return result;
        }
    }
}