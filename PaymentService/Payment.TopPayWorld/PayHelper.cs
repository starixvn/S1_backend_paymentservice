using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            
            var req = FromJson<DebitCallBackRequest>(bodyContent);
            var sign = req.Data.Signature;
            
            var dic = FromJson<Dictionary<string, string>>(bodyContent);
            dic.Remove("platSign");
            
            if (!VerifySign(dic, paymentPublicKey ,sign))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }
            
            var transStatus = TopPayWorldDefinition.GetPayInTransStatus(req.Data.Status);
            
            var result = new NotifyDepositResult
            {
                ThirdId = req.Data.TradeNo,
                DepositId = req.Data.OutTradeNo,
                Comment = req.Msg,
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
            // var apiUrl = inputs["Host"];
            // var merchantPrivateKey = inputs["MerchantPrivateKey"];
            // var merchantNo = inputs["MerchantNo"];
            // var reqAmount = decimal.Parse(inputs["Amount"]);
            // var strAmount = reqAmount.ToString("G29");
            // var transId = inputs["TransID"];
            // var accountName = inputs["AccountName"];
            // var notifyUrl = inputs["CallBackUrl"];
            // var bankCode = inputs["BankCode"];
            // var accountNo = inputs["AccountNo"];
            //
            // try
            // {
            //     var req = new Dictionary<string, string>
            //     {
            //         { "merchantCode", merchantNo },
            //         { "orderType", "0" },
            //         { "method", "Transfer" },
            //         { "orderNum", transId },
            //         { "money", strAmount },
            //         { "feeType", "1"}, 
            //         { "bankCode", bankCode },
            //         { "number", accountNo },
            //         { "name", accountName},
            //         { "mobile", "" },
            //         { "email", "" },
            //         { "notifyUrl", notifyUrl },
            //         { "dateTime", DateTime.Now.ToString("yyyyMMddHHmmss") },
            //         { "description", transId },
            //     };
            //     
            //     req.Add("sign" , GetSign(req,merchantPrivateKey)); ;
            //     
            //     var res = Payout(apiUrl, req);
            //     var response = new WithdrawResult
            //     {
            //         Success = true,
            //         RawContent = JsonConvert.SerializeObject(res)
            //     };
            //
            //     return response;
            // }
            // catch (CustomException ex)
            // {
            //     _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");
            //
            //     return new WithdrawResult
            //     {
            //         Success = false,
            //         Message = ex.Message
            //     };
            // }
            return null;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            // var key = payResource["TopPayPublicKey"];
            // var req = FromJson<PayOutCallBackResponse>(bodyContent);
            // var dic4Sign = new Dictionary<string, string> 
            // {
            //     { "platOrderNum", req.PlatOrderNum },
            //     { "orderNum", req.OrderNum },
            //     { "money", req.Money },
            //     { "fee", req.Fee },
            //     { "feeType", req.FeeType },
            //     { "name", req.Name },
            //     { "number", req.Number },
            //     { "bankCode", req.BankCode },
            //     { "status", req.Status },
            //     { "statusMsg", req.StatusMsg },
            //     { "description", req.Description },
            //     { "platSign", req.PlatSign }
            // };
            //
            // if (!VerifySign(dic4Sign, key))
            // {
            //     return new NotifyWithdrawResult
            //     {
            //         ThirdId = req.OrderNum,
            //         TransStatus = TransStatus.Pending,
            //         Response = "Signature does not match."
            //     };
            // }
            //
            // var transStatus = TopPayWorldDefinition.GetPayoutTransStatus(req.Status);
            //
            // return new NotifyWithdrawResult
            // {
            //     ThirdId = req.PlatOrderNum,
            //     TransStatus = transStatus,
            //     Message = req.StatusMsg,
            //     Response = TopPayWorldDefinition.CallbackResponse,
            //     WithdrawId = req.OrderNum
            // };
            return null;
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
            var signStr = string.Join("&", param.OrderBy(x => x.Key).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            try
            {
                 //将PKCS8格式的私钥转换为C#中的RSA对象
                 var rsa = RSA.Create();
                 var test = Convert.FromBase64String(rsaKey);
                 rsa.ImportPkcs8PrivateKey(test, out _);
                 
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
            var sign = GetSign(param, apiKey);
            return sign.Equals(target, StringComparison.OrdinalIgnoreCase);
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
        
        #endregion
    }
}