using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.TruePay
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
        
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"] + Path.Deposit;
            var merchant = inputs["Merchant"];
            var ts = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "mCode", merchant },
                    { "ts", ts },
                    { "bCode", !string.IsNullOrEmpty(inputs["PayCode"])
                        ? inputs["PayCode"] : inputs["BankCode"] },
                    { "amt", inputs["Amount"] },
                    { "user", inputs["CustId"] },
                    { "trxid", inputs["TransID"] },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "webUrl", inputs["ReturnUrl"] }
                };

                var token = $"{merchant}-{inputs["key"]}-getDPLink-{ts}";
                req.Add("token", ToMD5(token).ToLower());

                var (result, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, DepositResp>(apiUrl, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException(
                        $"TruePay deposit failed, HttpStatus: {(int)statusCode} {statusCode} Message: {result.Message}");
                }

                if (result.Status == ErrorCode.Success)
                {
                    return new DepositResult
                    {
                        Success = true,
                        ThirdId = result.Data.Id,
                        Url = result.Data.Url
                    };
                }

                return new DepositResult(result.Message);
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<DepositCallbackReq>(ToJson(formData));

            var str = $"{payResource["Merchant"]}-{payResource["key"]}-{data.TransId}-{data.Amount}-{data.Timestamp}";
            var token = ToMD5(str).ToLower();
            
            if (!token.Equals(data.Token))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }
            
            return new NotifyDepositResult
            {
                DepositId = data.TransId,
                TransStatus = data.Status == 1 ? TransStatus.Success : TransStatus.Pending,
                ThirdId = data.TransId,
                ActualAmount = data.Amount,
                Message = data.Message,
                Comment = $"{data.Status}-{data.Message}",
                Response = "OK"
            };
        }
        
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"] + Path.Payout;
            var merchant = inputs["Merchant"];
            var transId = inputs["TransID"];
            var ts = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "mCode", merchant },
                    { "ts", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                    { "bCode", inputs["BankCode"] },
                    { "trxid", transId },
                    { "user", inputs["CustId"] },
                    { "amt", inputs["Amount"] },
                    { "bAccName", inputs["AccountName"] },
                    { "bAccNo", inputs["AccountNo"] },
                    { "notifyUrl", inputs["CallBackUrl"] }
                };

                var str = $"{merchant}-{inputs["key"]}-sPayout-{ts}";
                req.Add("token", ToMD5(str).ToLower());
            
                var (result, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, WithdrawalResp>(apiUrl, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException(
                        $"TruePay deposit failed, HttpStatus: {(int)statusCode} {statusCode} Message: {result.Message}");
                }

                return new WithdrawResult
                {
                    Success = result.Status == ErrorCode.Success,
                    WithdrawId = transId,
                    Message = result.Message,
                    RawContent = result.Status.ToString()
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }
        
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<DepositCallbackReq>(ToJson(formData));

            var str = $"{payResource["Merchant"]}-{payResource["key"]}-{data.TransId}-{data.Amount}-{data.Timestamp}";
            var token = ToMD5(str).ToLower();
            
            if (!token.Equals(data.Token))
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var result = new NotifyWithdrawResult
            {
                WithdrawId = data.TransId,
                ThirdId = data.TransId,
                Message = $"{data.Status}-{data.Message}",
                Response = "OK"
            };

            switch (data.Status)
            {
                case 1:
                    result.TransStatus = TransStatus.Success;
                    result.RequestAmount = data.Amount;
                    return result;
                
                case -1:
                    result.TransStatus = TransStatus.Fail;
                    return result;
                
                default:
                    result.TransStatus = TransStatus.Pending;
                    return result;
            }
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
