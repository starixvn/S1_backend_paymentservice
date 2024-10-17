using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;

namespace Payment.Bajicash
{
    public class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly string _url;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region debit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var merchantCode = inputs["MerchantCode"];
            var transId = inputs["TransID"];
            var amount = decimal.Parse(inputs["Amount"]);
            var apiKey = inputs["ApiKey"];
            var secretKey = inputs["SecretKey"];
            var apiUrl = $"{inputs["ApiUrl"]}/{merchantCode}/{Definition.Path.Deposit}";
            var header = Crypto(Definition.CryptoMethod.Encrypt, transId, apiKey, secretKey);

            try
            {
                var req = new PaymentRequest
                {
                    TransactionAmount = amount,
                    PaymentCode = inputs["PaymentCode"],
                    UserId = inputs["UserName"],
                    CurrencyCode = inputs["PayCurrencyId"],
                    CallbackUrl = inputs["CallBackUrl"],
                    ReturnUrl = inputs["ReturnUrl"],
                    IPAddress = inputs["ClientIP"],
                };         

                var (result, statusCode) = _httpHelper
                    .WithHeader("X-Encrypted-Transaction", header)
                    .PostJson<PaymentRequest, PaymentResponse>(apiUrl, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Bajicash deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }


                if (result != null && result.Code != 0)
                {
                    throw new CustomException(result.Message);
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = result.Code.ToString(),
                    Url = result.Data.PageUrl,
                    Message = result.Message,
                    ThirdId = result.Data.TransactionNo,
                    DepositInfo = ""
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Bajicash deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var req = FromJson<CallbackRequest>(bodyContent);
            var reqInfo = Crypto(Definition.CryptoMethod.Decrypt, req.Key,payResource["ApiKey"], payResource["SecretKey"]);
            //解完後資料會用&分割，轉換成字典
            var dictReq = reqInfo.Split('&')
                .Select(param => param.Split('='))
                .ToDictionary(keyValue => keyValue[0], keyValue => keyValue[1]);
            var amount = decimal.Parse(dictReq["transaction_actual_amount"]);
            var status = dictReq["transaction_status"];
            var transStatus = Definition.GetTransStatus(status);

            return new NotifyDepositResult
            {
                DepositId = req.TransactionCode,
                ActualAmount = amount,
                ThirdId = req.TransactionNo,
                Comment = status,
                TransStatus = transStatus,
                Response = "SUCCESS"
            };
        }

        #endregion

        #region credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiKey = inputs["ApiKey"];
            var secretKey = inputs["SecretKey"];
            var merchantCode = inputs["MerchantCode"];
            var apiUrl = $"{inputs["ApiUrl"]}/{Definition.Path.Payout}/{merchantCode}";

            var p = new 
            {
                merchant_code = inputs["MerchantCode"],
                transaction_code = inputs["TransID"],
                transaction_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                payout_code = inputs["PayoutCode"],
                transaction_amount = inputs["Amount"],
                user_id = inputs["UserName"],
                currency_code = inputs["PayCurrencyId"],
                bank_account_number = inputs["AccountNo"],
                ifsc_code = inputs["IFSCCode"],
                bank_code = inputs["BankCode"],
                bank_name = inputs["BankName"],
                account_name = inputs["AccountName"],
                callback_url = inputs["CallBackUrl"]
            };

            //參數加密後送出
            var reqStr = ToJson(p);
            var key = Crypto(Definition.CryptoMethod.Encrypt, reqStr, apiKey, secretKey);
            var req = new PayoutRequest
            {
                Key = key,
            };

            var (result, statusCode) = _httpHelper
                .PostJson<PayoutRequest, PayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"Bajicash withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result != null && !result.Success)
            {
                throw new CustomException(result.Message);
            }

            return new WithdrawResult
            {
                Success = true,
                RawContent = JsonConvert.SerializeObject(result)
            };
        }
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var req = FromJson<CallbackRequest>(bodyContent);
            var reqInfo = Crypto(Definition.CryptoMethod.Decrypt, req.Key, payResource["ApiKey"], payResource["SecretKey"]);
            //解完資料是Json，轉字典
            var dictReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(reqInfo);
            var status = dictReq["transaction_status"];
            var transStatus = Definition.GetTransStatus(status);

            return new NotifyWithdrawResult
            {
                ThirdId = "",
                TransStatus = transStatus,
                Response = "SUCCESS",
                WithdrawId = req.TransactionCode
            };
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


        #region private
        private static string Crypto(string action, string payload, string apikey, string secretkey)
        {
            string secret_iv = secretkey;

            string iv = hashSha256(secret_iv).Substring(0, 16);

            SHA256 mySHA256 = SHA256Managed.Create();

            string output = "";
            if (action == "encrypt")
            {
                output = EncryptString(payload, mySHA256.ComputeHash(Encoding.ASCII.GetBytes(apikey)), Encoding.ASCII.GetBytes(iv));
            }
            else if (action == "decrypt")
            {
                output = DecryptString(payload, mySHA256.ComputeHash(Encoding.ASCII.GetBytes(apikey)), Encoding.ASCII.GetBytes(iv));
            }
            return output;
        }
        private static string hashSha256(string payload)
        {
            string hash = "";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] sourceBytes = Encoding.UTF8.GetBytes(payload);
                byte[] hashBytes = sha256Hash.ComputeHash(sourceBytes);
                hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty).ToLower();
            }
            return hash;
        }

        private static string EncryptString(string plainText, byte[] key, byte[] iv)
        {
            Aes encryptor = Aes.Create();
            encryptor.Mode = CipherMode.CBC;
            encryptor.Key = key;
            encryptor.IV = iv;
            MemoryStream memoryStream = new MemoryStream();
            ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);
            return Uri.EscapeDataString(cipherText);
        }

        private static string DecryptString(string cipherText, byte[] key, byte[] iv)
        {
            Aes encryptor = Aes.Create();
            encryptor.Mode = CipherMode.CBC;
            encryptor.Key = key;
            encryptor.IV = iv;
            MemoryStream memoryStream = new MemoryStream();
            ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);
            string plainText = String.Empty;
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(Uri.UnescapeDataString(cipherText));
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                cryptoStream.FlushFinalBlock();
                byte[] plainBytes = memoryStream.ToArray();
                plainText = Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
            }
            finally
            {
                memoryStream.Close();
                cryptoStream.Close();
            }
            return plainText;
        }
    }

    #endregion


}

