using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.YuiPay
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
            var payCode = input["PayCode"].Split('-');
            var transId = input["TransID"];
            var merchantNo = input["merchantNo"];
            var reqAmount = decimal.Parse(input["Amount"]);
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "version", YuiPayDefinition.Version },
                    { "signType", YuiPayDefinition.SingType },
                    { "merchantNo", merchantNo },
                    { "date", DateTimeOffset.UtcNow.ToString("yyyyMMddhhmmss") },
                    { "channleType", payCode[0] },
                    { "noticeUrl", input["CallBackUrl"] },
                    { "orderNo", transId },
                    { "bizAmt", reqAmount.ToString("G29") }
                };

                if (payCode[0].Equals(PayType.OnlineBanking))
                {
                    req.Add("bankCode", input["BankCode"]);
                }
                
                if (payCode[0].Equals(PayType.QR))
                {
                    req.Add("bankCode", payCode[1]);
                }
                
                req.Add("sign", GetSign(req, key));
                
                var (response, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, DepositResponse>($"{baseApiUrl}{Path.Deposit}", req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"YuiPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.Code))
                {
                    throw new CustomException(response.Message, response.Code);
                }
                
                return new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = response.Detail.PayUrl
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var apiKey = input["key"];
            var req = FromJson<DepositCallbackRequest>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, apiKey, req.Sign))
            {
                return new NotifyDepositResult
                {
                    DepositId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = YuiPayDefinition.GetTransStatus(req.Status);

            return new NotifyDepositResult
            {
                DepositId = req.TransId,
                ActualAmount = req.Amount,
                ThirdId = req.TransId,
                Comment = req.Status,
                TransStatus = transStatus,
                Response = YuiPayDefinition.CallbackResponse
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
            var merchantNo = inputs["merchantNo"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var transId = inputs["TransID"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "version", YuiPayDefinition.Version },
                    { "signType", YuiPayDefinition.SingType },
                    { "merchantNo", merchantNo },
                    { "date", DateTimeOffset.UtcNow.ToString("yyyyMMddhhmmss") },
                    { "channleType", inputs["PayCode"] },
                    { "noticeUrl", inputs["CallBackUrl"] },
                    { "orderNo", transId },
                    { "bizAmt", reqAmount.ToString("G29") },
                    { "bankCode", inputs["BankCode"] },
                    { "bankBranchName", inputs["BankName"] },
                    { "cardNo", inputs["AccountNo"] },
                    { "accName", inputs["AccountName"] }
                };
                
                req.Add("sign", GetSign(req, key));

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, WithdrawalResponse>($"{baseApiUrl}{Path.Withdrawal}", req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"YuiPay withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.Code))
                {
                    throw new CustomException(response.Message, response.Code);
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var key = payResource["key"];
            var req = FromJson<WithdrawalCallbackRequest>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dicReq, key, req.Sign))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = YuiPayDefinition.GetTransStatus(req.Status);

            return new NotifyWithdrawResult
            {
                ThirdId = req.TransId,
                TransStatus = transStatus,
                Message = req.Remark,
                Response = YuiPayDefinition.CallbackResponse,
                WithdrawId = req.TransId
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
                .Where(x => !x.Key.Equals("sign") && !string.IsNullOrEmpty(x.Value))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + key;
            
            var sign = ToMD5(str).ToLower();

            return sign;
        }
        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }

        #endregion
    }
}