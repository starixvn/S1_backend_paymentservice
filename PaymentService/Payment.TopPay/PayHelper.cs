using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.TopPay
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

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["Host"];
            var merchantPrivateKey = inputs["MerchantPrivateKey"];
            var merchantNo = inputs["MerchantNo"];
            var payCode = inputs["PayCode"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var depositInfo = inputs["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantCode", merchantNo },
                    { "orderType", "0" },
                    { "method", bankCode },
                    { "orderNum", transId },
                    { "payMoney", strAmount },
                    { "productDetail", transId },
                    { "notifyUrl", notifyUrl },
                    { "dateTime", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "expiryPeriod", "120" },
                    { "name", accountName },
                    { "email", "" },
                    { "phone", "" },
                };
                
                req.Add("sign" , GetSign(req,merchantPrivateKey));

                var res = CreateOrder(apiUrl, req);

                depositInfo = depositInfo.AddOrSetJProperty("PayCode", payCode);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Url,
                    Message = res.PlatRespMessage,
                    ThirdId = res.PlatOrderNum,
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
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var depositInfo = payResource["DepositInfo"];
            var transId = payResource["TransID"];
            var paymentPublicKey = payResource["TopPayPublicKey"];

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
            
            var req = FromJson<DebitCallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                { "code", req.Code },
                { "msg", req.Msg },
                { "status", req.Status },
                { "platOrderNum", req.PlatOrderNum },
                { "orderNum", req.OrderNum },
                { "method", req.Method },
                { "name", req.Name },
                { "payMoney", req.PayMoney },
                { "payFee", req.PayFee },
                { "email", req.Email },
                { "phone", req.Phone },
                { "platSign", req.PlatSign }
            };

            if (!VerifySign(dic, paymentPublicKey))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }
            
            var transStatus = TopPayDefinition.GetPayInTransStatus(req.Status);

            var result = new NotifyDepositResult
            {
                ThirdId = req.PlatOrderNum,
                DepositId = req.OrderNum,
                Comment = req.Msg,
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(req.PayMoney),
                Response = TopPayDefinition.CallbackResponse
            };

            return result;
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["Host"];
            var merchantPrivateKey = inputs["MerchantPrivateKey"];
            var merchantNo = inputs["MerchantNo"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var accountNo = inputs["AccountNo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantCode", merchantNo },
                    { "orderType", "0" },
                    { "method", "Transfer" },
                    { "orderNum", transId },
                    { "money", strAmount },
                    { "feeType", "1"}, 
                    { "bankCode", bankCode },
                    { "number", accountNo },
                    { "name", accountName},
                    { "mobile", "" },
                    { "email", "" },
                    { "notifyUrl", notifyUrl },
                    { "dateTime", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "description", transId },
                };
                
                req.Add("sign" , GetSign(req,merchantPrivateKey)); ;
                
                var res = Payout(apiUrl, req);
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
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var key = payResource["TopPayPublicKey"];
            var req = FromJson<PayOutCallBackResponse>(bodyContent);
            var dic4Sign = new Dictionary<string, string> 
            {
                { "platOrderNum", req.PlatOrderNum },
                { "orderNum", req.OrderNum },
                { "money", req.Money },
                { "fee", req.Fee },
                { "feeType", req.FeeType },
                { "name", req.Name },
                { "number", req.Number },
                { "bankCode", req.BankCode },
                { "status", req.Status },
                { "statusMsg", req.StatusMsg },
                { "description", req.Description },
                { "platSign", req.PlatSign }
            };

            if (!VerifySign(dic4Sign, key))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = req.OrderNum,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = TopPayDefinition.GetPayoutTransStatus(req.Status);

            return new NotifyWithdrawResult
            {
                ThirdId = req.PlatOrderNum,
                TransStatus = transStatus,
                Message = req.StatusMsg,
                Response = TopPayDefinition.CallbackResponse,
                WithdrawId = req.OrderNum
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

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string rsaKey)
        {
            string result;
            var signStr = string.Join("", param.OrderBy(kp => kp.Key)
                  .Select(x => $"{x.Value}"));
            
            _logger.LogInformation(signStr);

            var getPrivateKey = GetPrivateKey(rsaKey);
            result = PrivateEncrypt(signStr, getPrivateKey);
            
            _logger.LogInformation($"TopPay Sign:{result}");
            
            return result;
        }

        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + TopPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJson<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"TopPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.PlatRespCode == "FAIL")
                throw new CustomException("TopPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl , Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + TopPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJson<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"TopPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.PlatRespCode == "FAIL")
                throw new CustomException("TopPay create payout failed.");

            return result;
        }
        
        #endregion
        
        
        #region Sign
        
        private bool VerifySign(Dictionary<string, string> paramsDict, string publicKey)
        {
            try
            {
                var platSign = paramsDict["platSign"];
                paramsDict.Remove("platSign");
                
                var paramNameList = paramsDict.Keys.ToList();
                paramNameList.Sort();
                
                var stringBuilder = new StringBuilder();
                foreach (var name in paramNameList)
                {
                    stringBuilder.Append(paramsDict[name]);
                }
               
                _logger.LogInformation("TopPay DecryptKeys:" + stringBuilder);
                
                var decryptSign = PublicDecrypt(platSign, GetPublicKey(publicKey));
                _logger.LogInformation("TopPay DecryptSign:" + decryptSign);
                
                return stringBuilder.ToString().Equals(decryptSign);
            }
            catch (Exception e)
            {
                throw new Exception("Verify Sign Error", e);
            }
        }
        
        private static string PrivateEncrypt(string data, RsaPrivateCrtKeyParameters privateKey)
        {
            try
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var cipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                cipher.Init(true, privateKey); // true for encryption mode
                var encryptedBytes = RsaSplitCodec(cipher, true, dataBytes, privateKey.Modulus.BitLength);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception e)
            {
                throw new Exception("Encountered an exception when decrypting the string [" + data + "]", e);
            }
        }
        
        private static RsaKeyParameters GetPublicKey(string publicKey)
        {
            try
            {
                // 将Base64编码的公钥字符串转换为字节流
                var keyBytes = Convert.FromBase64String(publicKey);

                // 使用X509EncodedKeySpec转换为RSA公钥
                var pubKey = (RsaKeyParameters)PublicKeyFactory.CreateKey(keyBytes);

                return pubKey;
            }
            catch (Exception e)
            {
                throw new ArgumentException("Unable to parse the public key. Please ensure a valid X509 encoded public key is provided.", e);
            }
        }
        
        private static string PublicDecrypt(string data, RsaKeyParameters publicKey)
        {
            try
            {
                // 将Base64编码的字符串转换为字节
                var dataBytes = Convert.FromBase64String(data);

                // 初始化Cipher为解密模式
                var cipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
                cipher.Init(false, publicKey); // false for decryption mode

                // 解密数据
                var decryptedBytes = RsaSplitCodec(cipher, false, dataBytes, publicKey.Modulus.BitLength);

                // 将解密后的字节转换为字符串
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception e)
            {
                throw new Exception("Encountered an exception when decrypting the string [" + data + "]", e);
            }
        } 

        private static RsaPrivateCrtKeyParameters GetPrivateKey(string privateKey)
        {
            // 将Base64编码的私钥字符串转换为字节流
            byte[] keyBytes = Convert.FromBase64String(privateKey);

            // 通过BouncyCastle的PrivateKeyFactory创建私钥对象
            var privateKeyParam = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(keyBytes);

            return privateKeyParam;
        }

        private static byte[] RsaSplitCodec(IBufferedCipher cipher, bool isEncryptMode, byte[] data, int keySize)
        {
            var maxBlock = isEncryptMode ? keySize / 8 - 11 : keySize / 8;
            using (MemoryStream outStream = new MemoryStream())
            {
                var offSet = 0;
                byte[] buff;
                var i = 0;
                try
                {
                    while (data.Length > offSet)
                    {
                        if (data.Length - offSet > maxBlock)
                        {
                            buff = cipher.DoFinal(data, offSet, maxBlock);
                        }
                        else
                        {
                            buff = cipher.DoFinal(data, offSet, data.Length - offSet);
                        }
                        outStream.Write(buff, 0, buff.Length);
                        i++;
                        offSet = i * maxBlock;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("An exception occurred while encrypting/decrypting data with the threshold : " + maxBlock, e);
                }
                return outStream.ToArray();
            }
        }
        
        #endregion
        
    }
}