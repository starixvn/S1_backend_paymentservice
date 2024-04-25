using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.TopPayWorld
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
            var merchantNo = inputs["MerchantCode"];
            var amount = inputs["Amount"];
            var transId = inputs["TransID"];
            var notifyUrl = inputs["CallBackUrl"];
            var payCode = inputs["PayCode"];
            var depositInfo = inputs["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantCode", merchantNo },
                    { "outTradeNo", transId },
                    { "requestNo", transId },
                    { "version", "v1" },
                    { "payType", "COLLECTION" },
                    { "payMethod", "PAYLINK" },
                    { "payAmount", amount},
                    { "payProductCode", payCode },
                    { "payCategory", "BANK" },
                    { "notifyUrl", notifyUrl }
                };
                
                req.Add("signature" , GetSign(req,merchantPrivateKey));

                var res = CreateOrder(apiUrl, req);
                
                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = res.Code,
                    Url = res.Data.PaymentValue,
                    Message = res.Msg,
                    ThirdId = res.Data.TradeNo,
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
            var transId = payResource["TransID"];
            var paymentPublicKey = payResource["TopPayPublicKey"];

            var (dic, req ,signature) = GetCallBackRequest(bodyContent);
            
            if (!VerifySign(dic, paymentPublicKey ,signature))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }
            
            var transStatus = TopPayWorldDefinition.GetPaymentTransStatus(req.Data.Status);
            
            var result = new NotifyDepositResult
            {
                ThirdId = req.Data.TradeNo,
                DepositId = req.Data.OutTradeNo,
                Comment = req.Data.Status,
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(req.Data.PayAmount),
                Response = TopPayWorldDefinition.CallbackResponse
            };

            return result;
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["Host"];
            var merchantPrivateKey = inputs["MerchantPrivateKey"];
            var merchantNo = inputs["MerchantCode"];
            var reqAmount = inputs["Amount"];
            var transId = inputs["TransID"];
            var payCode = inputs["PayCode"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var accountNo = inputs["AccountNo"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantCode", merchantNo },
                    { "outTradeNo", transId },
                    { "requestNo", transId },
                    { "version", "v1" },
                    { "payType", "PAYMENT" },
                    { "payAmount", reqAmount},
                    { "payProductCode", payCode },
                    { "payCategory", "BANK" },
                    { "accountNo", accountNo },
                    { "accountType", bankCode },
                    { "notifyUrl", notifyUrl }
                };
                req.Add("signature" , GetSign(req,merchantPrivateKey));
                
                var res = CreateOrder(apiUrl, req);
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
            var paymentPublicKey = payResource["TopPayPublicKey"];
            
            var (dic, req ,signature) = GetCallBackRequest(bodyContent);
            
            if (!VerifySign(dic, paymentPublicKey ,signature))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = req.Data.TradeNo,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }
            
            var transStatus = TopPayWorldDefinition.GetPaymentTransStatus(req.Data.Status);
            
            return new NotifyWithdrawResult
            {
                ThirdId = req.Data.TradeNo,
                TransStatus = transStatus,
                Message = req.Data.Status,
                WithdrawId = req.Data.OutTradeNo,
                Response = TopPayWorldDefinition.CallbackResponse,
            };
        }

        #endregion

        #region UnUsed

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
        
        #endregion
        
        #region Private
        
        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string rsaKey)
        {
            var signStr = string.Join("&", param.OrderBy(x => x.Key ,StringComparer.Ordinal).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            try
            {
                 //将PKCS8格式的私钥转换为C#中的RSA对象
                 var rsa = RSA.Create();
                 rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(rsaKey), out _);
                 
                 //初始化算法SHA256
                 using var sha256 = SHA256.Create();
                 var signData = Encoding.UTF8.GetBytes(signStr);
                 var hash = sha256.ComputeHash(signData);
                 
                 //使用RSA签名算法进行签名
                 var signatureBytes = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                 
                 //签名值byte数组转字符串用BASE64
                 var strSign = Convert.ToBase64String(signatureBytes);
                 
                return strSign;
            }
            catch (Exception e)
            {
                throw new Exception("Encountered an exception when decrypting the string [" + signStr + "]", e);
            }
        }
        
        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var signStr = string.Join("&", param.OrderBy(x => x.Key, StringComparer.Ordinal).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
             
            // 待验证签名值base64解码
            var sign = Convert.FromBase64String(target);
            // 初始化算法SHA256
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(signStr));
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(apiKey), out _);
            var result = rsa.VerifyHash(hash, sign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return result;
        }
        
        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + TopPayWorldDefinition.Path.CreateOrder;
            
            var (result, statusCode) = _httpHelper
                .WithHeader("Content-Type", MediaType.Json)
                .WithHeader("Accept-Timezone", AcceptTimezone.Timezone)
                .PostJson<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"TopPayWorld creat order failed, HttpStatus: {(int)statusCode}");
            }

            if (result.Code != "0000")
            {
                throw new CustomException($"TopPayWorld creat order failed, HttpStatus: {(int)statusCode} , resultCode :{result.Code} , Msg :{result.Msg}");
            }
            
            return result;
        }

        private (Dictionary<string,string>?, CallBackRequest ,string) GetCallBackRequest(string bodyContent)
        {
            var requestModel = FromJson<CallBackRequest>(bodyContent);
            var sign = requestModel.Data.Signature;
            
            var jsonObj = JObject.Parse(bodyContent);
            var dataOut = jsonObj["data"]?.ToObject<JObject>();
            var dic = dataOut?.Properties().ToDictionary(x => x.Name, x => x.Value.ToString());
            dic?.Remove("signature");

            return (dic, requestModel, sign);
        }
        
        #endregion
    }
}