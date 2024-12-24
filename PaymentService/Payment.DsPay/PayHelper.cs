using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DsPay
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
            try
            {
                var url = $"{inputs["Host"]}{Definition.Path.Deposit}";
                var amount = decimal.Parse(inputs["Amount"]);
                var dateTimeUtc0530 = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(5, 30, 0));
                var req = new Dictionary<string,string>
                {
                    { "mch_id", inputs["MchId"] },
                    { "out_trade_no", inputs["TransID"] },
                    { "money", amount.ToString("F2") },
                    { "subject", "subject" },
                    { "notify_url", inputs["CallBackUrl"] },
                    { "type", inputs["PayCode"] },
                    { "client_ip", inputs["ClientIp"] },
                    { "return_url", inputs["ReturnUrl"] },
                    { "timestamp", dateTimeUtc0530.ToString("yyyy-MM-dd HH:mm:ss") }
                };
            
                req.Add("sign", GetSign(req, inputs["ApiKey"]));
                var result = CreateOrder(url, req);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.Data.PayUrl,
                    ThirdId = result.Data.TradeNo
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"DsPay deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var receive = FromJson<Dictionary<string,string>>(bodyContent);
            var transId = payResource["TransID"];
            var state = receive["state"];
            if (!VerifySign(receive, payResource["ApiKey"], receive["sign"]))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    DepositId = transId,
                    Response = "Signature verification failed."
                };
            }

            return new NotifyDepositResult
            {
                TransStatus = Definition.GetTransStatus(state),
                DepositId = transId,
                Response = Definition.CallBackResponse,
                ThirdId = receive["trade_no"],
                ActualAmount = decimal.Parse(receive["money"]),
                Comment = state
            };
        }
        #endregion
        
        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }
        #endregion
        
        #region unuse
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
        
        private PaymentResponse<DepositData> CreateOrder(string url, Dictionary<string, string> req)
        {
            var (result, statusCode) =
                _httpHelper.PostJson<Dictionary<string, string>, PaymentResponse<DepositData>>(url, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"DsPay deposit failed, HttpStatus: {(int)statusCode} {result}");
            }
            
            if (result.Code != Definition.SuccessCode)
            {
                throw new CustomException($"DsPay deposit failed, code: {result.Code}, msg: {result.Msg}");
            }

            return result;
        }

        private string GetSign(Dictionary<string,string> param, string apiKey)
        {
            var signSourceString = string.Join("&", param
                    .Where(pair => pair.Key != "sign")
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => $"{pair.Key}={pair.Value}")) + $"&key={apiKey}";
            var sign = ToMD5(signSourceString).ToUpper();
            return sign;
        }
        
        private bool VerifySign(Dictionary<string,string> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);
            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

    }
}