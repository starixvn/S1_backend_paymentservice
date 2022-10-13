using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.HonorPay
{
    class PayHelper : PayHelperBase
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
            var baseApiUrl = input["host"];
            var key = input["key"];
            var apiToken = input["access_token"];
            var payCode = input["PayCode"];
            var transId = input["TransID"];
            
            try
            {
                var (payType, bankCode) = ParseDepositPayCode(payCode);
                var formData = new Dictionary<string, string>
                {
                    { "account", input["merchant_no"] },
                    { "payType", payType },
                    { "payMoney", input["Amount"] },
                    { "ip", input["ClientIP"] },
                    { "notifyURL", input["CallBackUrl"] },
                    { "orderNo", transId }
                };

                if (NeedBankCode(payType))
                {
                    formData.Add("BankCode", bankCode);
                }
                
                formData.Add("sign", GetSign(formData, key));

                var (response, statusCode) = _httpHelper
                    .WithOAuthBearerToken(apiToken)
                    .PostUrlEncoded<Dictionary<string, string>, DepositResponse>($"{baseApiUrl}{Path.Deposit}", formData);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"HonorPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.ErrorCode))
                {
                    var msg = string.IsNullOrEmpty(response.ErrorMessage)
                        ? GetDepositErrorMsg(response.ErrorCode) : response.ErrorMessage;
                    throw new CustomException(msg, response.ErrorCode);
                }
                
                return new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = response.RedirectUrl,
                    ThirdId = response.TpTransId,
                    FormData = formData
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> input)
        {
            var apiKey = input["key"];
            var req = FromJson<DepositCallbackRequest>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, apiKey, req.sign))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature does not match.",
                    Response = HonorPayDefinition.CallbackResponse
                };
            }

            var transId = input["TransID"];
            var transStatus = DepositStatus.Success.Equals(req.payStatus)
                ? TransStatus.Success
                : TransStatus.Pending;

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = req.realCharge,
                ThirdId = req.uuid,
                Comment = req.payStatus,
                TransStatus = transStatus,
                Response = HonorPayDefinition.CallbackResponse
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

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var baseApiUrl = inputs["host"];
            var key = inputs["key"];
            var apiToken = inputs["access_token"];
            var merchant = inputs["merchant_no"];
            var reqAmount = inputs["Amount"];
            var transId = inputs["TransID"];
            
            try
            {
                var formData = new Dictionary<string, string>
                {
                    { "account", merchant },
                    { "orderNo", transId },
                    { "withdrawalType", "bankcard" },
                    { "amount", reqAmount },
                    { "bankName", inputs["BankCode"] },
                    { "bankNumber", inputs["AccountNo"] },
                    { "bankHolder", inputs["AccountName"] },
                    { "nonceStr", GenerateNonceStr() },
                    { "notifyURL", inputs["withdrawal_notify_url"] }
                };

                //formData.Add("sign", GetSign(formData, key));
               
                var (response, statusCode) = _httpHelper
                    .WithOAuthBearerToken(apiToken)
                    .PostUrlEncoded<Dictionary<string, string>, WithdrawalResponse>($"{baseApiUrl}{Path.Withdrawal}", formData);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"HonorPay withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!IsSuccess(response.ErrorCode))
                {
                    var msg = GetWithdrawalErrorMsg(response.ErrorCode);
                    throw new CustomException(msg, response.ErrorCode);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(response)
                };
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var key = payResource["key"];
            var req = FromJson<WithdrawalCallbackRequest>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, key, req.sign))
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Pending,
                    Message = "Signature does not match.",
                    Response = HonorPayDefinition.CallbackResponse
                };
            }

            var transId = payResource["TransID"];
            var transStatus = HonorPayDefinition.GetWithdrawalTransStatus(req.status);

            return new NotifyWithdrawResult
            {
                ThirdId = req.uuid,
                TransStatus = transStatus,
                Response = HonorPayDefinition.CallbackResponse
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private

        private string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
        {
            var str = string.Join('&', input
                .Where(x => !string.IsNullOrEmpty(x.Value) && !x.Key.Equals("sign"))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";
            
            var sign = ToHMACSHA256(str, key).ToUpper();

            return sign;
        }



        private static bool NeedBankCode(string payType)
            => HonorPayDefinition.NeedBankCodeChannels.Contains(payType);

        private static bool IsSuccess(string errorCode)
            => DepositStatus.Success.Equals(errorCode);

        private static string GetDepositErrorMsg(string errorCode)
        {
            if (HonorPayDefinition.DepositErrorMsgMap.ContainsKey(errorCode))
            {
                return HonorPayDefinition.DepositErrorMsgMap[errorCode];
            }

            return "Unknown error";
        }
        
        private static string GetWithdrawalErrorMsg(string errorCode)
        {
            if (HonorPayDefinition.WithdrawalErrorMsgMap.ContainsKey(errorCode))
            {
                return HonorPayDefinition.WithdrawalErrorMsgMap[errorCode];
            }

            return "Unknown error";
        }
        
        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }
        
        private static string GenerateNonceStr()
            => Guid.NewGuid().ToString()[..32];

        private static (string PayType, string BankCode) ParseDepositPayCode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return (string.Empty, string.Empty);
            }
            
            var arr = str.Split('-');

            return arr.Length > 1 ? (arr[0], arr[1]) : (arr[0], string.Empty);
        }

        #endregion
    }
}
