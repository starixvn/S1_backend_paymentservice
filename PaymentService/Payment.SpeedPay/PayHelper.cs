using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.SpeedPay
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

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var payCode = input["PayCode"];
            var transId = input["TransID"];
            var userName = input["UserName"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var setting = GetSetting(input);
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "version", SpeedPayDefinition.Version },
                    { "merchantNo", setting.MerchantId }
                };
                
                var content = new Dictionary<string, string>
                {
                    { "amount", reqAmount.ToString("F2") },
                    { "paymentCode", payCode },
                    { "orderNo", transId },
                    { "orderTime", DateTimeOffset.Now.ToString("yyyyMMddHHmmss") },
                    { "returnUrl", input["ReturnUrl"] },
                    { "notifyUrl", input["CallBackUrl"] },
                    { "memberCode", userName }
                };
                
                req.Add("request", EncodeContent(content));
                req.Add("signMsg", GetSign(req, setting.Key));

                var apiUrl = $"{setting.ApiUrl}{SpeedPayDefinition.Path.Deposit}";
                var respData = CreateTrans<DepositResp>(req, apiUrl, setting.Key);
                
                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = respData.ResultUrl,
                    ThirdId = respData.ThirdId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"SpeedPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var payCurrency = SpeedPayDefinition.GetPayCurrency(input["CurrencyId"]);
            input.Add("PayCurrencyId", payCurrency);
            
            var setting = GetSetting(input);
            var req = FromJson<CallbackReq>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, setting.Key, req.signMsg))
            {
                return new NotifyDepositResult
                {
                    DepositId = input["TransID"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var content = DecodeContent<CallbackContent>(req.content, setting.Key);

            if (content == null)
            {
                return new NotifyDepositResult
                {
                    ThirdId = input["Pending"],
                    TransStatus = TransStatus.Pending,
                    Response = "Missing callback content."
                };
            }

            var transStatus = SpeedPayDefinition.GetTransStatus(content.Status);

            return new NotifyDepositResult
            {
                DepositId = content.TransId,
                ActualAmount = content.Amount,
                ThirdId = content.TransId,
                Comment = content.Status,
                TransStatus = transStatus,
                Response = SpeedPayDefinition.CallbackResponse
            };
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> input)
        {
            var reqAmount = decimal.Parse(input["Amount"]);
            var transId = input["TransID"];
            var setting = GetSetting(input);
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "version", SpeedPayDefinition.Version },
                    { "merchantNo", setting.MerchantId }
                };
                
                var content = new Dictionary<string, string>
                {
                    { "amount", reqAmount.ToString("F2") },
                    { "bankCode", input["BankCode"] },
                    { "bankAccountNo", input["AccountNo"] },
                    { "bankAccountName", input["AccountName"] },
                    { "orderNo", transId },
                    { "orderTime", DateTimeOffset.Now.ToString("yyyyMMddHHmmss") },
                    { "notifyUrl", input["CallBackUrl"] },
                    { "memberCode", input["UserName"] }
                };
                
                req.Add("request", EncodeContent(content));
                req.Add("signMsg", GetSign(req, setting.Key));

                var apiUrl = $"{setting.ApiUrl}{SpeedPayDefinition.Path.Payout}";
                var respData = CreateTrans<PayoutResp>(req, apiUrl, setting.Key);
                
                return new WithdrawResult
                {
                    WithdrawId = transId,
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(respData)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"SpeedPay payout failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var payCurrency = SpeedPayDefinition.GetPayCurrency(payResource["CurrencyId"]);
            payResource.Add("PayCurrencyId", payCurrency);
            
            var setting = GetSetting(payResource);
            var req = FromJson<CallbackReq>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, setting.Key, req.signMsg))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = payResource["TransId"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }
            
            var content = DecodeContent<CallbackContent>(req.content, setting.Key);

            if (content == null)
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = payResource["TransId"],
                    TransStatus = TransStatus.Pending,
                    Response = "Missing callback content."
                };
            }
            
            var transStatus = SpeedPayDefinition.GetTransStatus(content.Status);

            return new NotifyWithdrawResult
            {
                ThirdId = content.ThirdId,
                TransStatus = transStatus,
                Message = content.Status,
                Response = SpeedPayDefinition.CallbackResponse,
                WithdrawId = content.TransId
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private

        public string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
        {
            var str = string.Join('&', input
                .Where(x => !x.Key.Equals("signMsg"))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}:{x.Value}")) + $"&key:{key}";
            
            _logger.LogInformation(str);
            var sign = ToMD5(str);

            return sign;
        }
        
        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }

        private static SpeedPaySetting GetSetting(IDictionary<string, string> input)
        {
            var payCurrency = input["PayCurrencyId"];

            if (!input.ContainsKey(payCurrency))
            {
                throw new CustomException($"SpeedPay missing {payCurrency} setting.");
            }
            
            var setting = JsonConvert.DeserializeObject<SpeedPaySetting>(input[payCurrency]);
            
            if (setting == null)
            {
                throw new CustomException($"SpeedPay {payCurrency} setting is null.");
            }

            return setting;
        }
        
        private string EncodeContent<T>(T input)
        {
            var json = JsonConvert.SerializeObject(input);
            var result = ToBase64Encode(json);

            _logger.LogInformation("orig content: " + json);
            
            return result;
        }
        
        private T DecodeContent<T>(string str, string key)
        {
            var json = ToBase64Decode(str);
            
            _logger.LogInformation("decoded content: " + json);

            var content = JsonConvert.DeserializeObject<T>(json);

            return content;
        }

        private T CreateTrans<T> (IDictionary<string, string> req, string apiUrl, string key)
            where T : ICreateTransResp
        {
            var (response, statusCode) = _httpHelper
                .PostUrlEncoded<IDictionary<string, string>, IDictionary<string, string>>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"HttpStatus: {(int)statusCode} {statusCode}");
            }
                
            if (!response["resCode"].Equals(SpeedPayDefinition.SuccessCode))
            {
                throw new CustomException(response["resCode"], response["resCode"]);
            }

            var respData = DecodeContent<T>(response["response"], key);
                
            if (!respData.Code.Equals(SpeedPayDefinition.TransSuccessCode))
            {
                throw new CustomException(respData.Message, respData.Code);
            }

            return respData;
        }

        #endregion
    }
}