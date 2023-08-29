using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.RolexPay
{
    class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private const decimal AmountFix = 100;
        
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var transId = inputs["TransID"];
            var reqAmount = decimal.Parse(inputs["Amount"]) * AmountFix;
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "version", RolexPayDefinition.Version.Deposit },
                    { "cid", inputs["MerchantId"] },
                    { "tradeNo", transId },
                    { "amount", reqAmount.ToString("G29") },
                    { "payType", inputs["PayCode"] },
                    { "acctName", inputs["AccountName"] },
                    { "requestTime", GetRequestTime() },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "returnType", "0" }
                };

                req.Add("Sign", GetSign(req, inputs["Key"]));

                var resp = CreateOrder(inputs["ApiUrl"], req);
                
                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = resp.Link,
                    ThirdId = resp.ThirdId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"SpeedPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var transId = inputs["TransID"];
            var reqAmount = decimal.Parse(inputs["Amount"]) * AmountFix;
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "version", RolexPayDefinition.Version.Payout },
                    { "cid", inputs["MerchantId"] },
                    { "tradeNo", transId },
                    { "amount", reqAmount.ToString("G29") },
                    { "payType", inputs["PayCode"] },
                    { "acctName", inputs["AccountName"] },
                    { "acctNo", inputs["AccountNo"] },
                    { "bankCode", inputs["BankCode"] },
                    { "requestTime", GetRequestTime() },
                    { "notifyUrl", inputs["CallBackUrl"] }
                };

                req.Add("sign", GetSign(req, inputs["Key"]));

                var resp = CreatePayout(inputs["ApiUrl"], req);

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId,
                    Message = resp.Message,
                    RawContent = resp.Status
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"TruePay payout failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var dic = FromJson<Dictionary<string, string>>(bodyContent);
            
            if (!dic["sign"].Equals(GetSign(dic, payResource["Key"])))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = RolexPayDefinition.GetTransStatus(dic["status"]);
            var amount = decimal.Parse(dic["amount"]) / AmountFix;
            
            return new NotifyDepositResult
            {
                DepositId = dic["tradeNo"],
                TransStatus = transStatus,
                ThirdId = dic["rockTradeNo"],
                ActualAmount = amount,
                Comment = $"({dic["status"]}){transStatus}",
                Response = RolexPayDefinition.CallbackResponse
            };
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var dic = FromJson<Dictionary<string, string>>(bodyContent);
            
            if (!dic["sign"].Equals(GetSign(dic, payResource["Key"])))
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = RolexPayDefinition.GetTransStatus(dic["status"]);
            
            return new NotifyWithdrawResult
            {
                WithdrawId = dic["tradeNo"],
                TransStatus = transStatus,
                ThirdId = dic["rockTradeNo"],
                Message = $"({dic["status"]}){transStatus}",
                Response = RolexPayDefinition.CallbackResponse
            };
        }
        
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

        #region Private

        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = $"{baseApiUrl}{RolexPayDefinition.Path.Deposit}";

            var (result, statusCode) = _httpHelper
                .PostJson<object, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"RolexPay creat order failed, HttpStatus: {(int) statusCode} {statusCode}");
            }

            if (result.Code != RolexPayDefinition.SuccessCode)
                throw new CustomException(result.Message);

            return result;
        }
        
        private CreatPayoutResponse CreatePayout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = $"{baseApiUrl}{RolexPayDefinition.Path.Payout}";

            var (result, statusCode) = _httpHelper
                .PostJson<object, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"RolexPay creat payout failed, HttpStatus: {(int) statusCode} {statusCode}");
            }

            if (result.Code != RolexPayDefinition.SuccessCode)
                throw new CustomException(result.Message);

            return result;
        }
        
        private string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
        {
            var str = string.Join('&', input
                .Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";
            
            _logger.LogInformation(str);
            var sign = ToMD5(str);

            return sign;
        }
        
        private static string GetRequestTime()
            => DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
        
        #endregion
    }
}