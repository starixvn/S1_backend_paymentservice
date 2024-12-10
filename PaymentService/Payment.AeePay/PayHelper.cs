using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.AeePay
{
    public class PayHelper : PayHelperBase
    {
        internal static string? SignConcatSourceString;
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
                var currency = int.Parse(inputs["CurrencyId"]);
                var url = $"{inputs[$"{currency}_Host"]}{Definition.Path.Deposit}";
                
                var payCode = GetPayCode(inputs["PayCode"]);
                var bankCode = GetBankCode(inputs["PayCode"]);
                var dateTimeUtc08 = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0));
                var sendTime = dateTimeUtc08.ToString("yyyyMMddHHmmss");
                var money = decimal.Parse(inputs["Amount"]).ToString("F2");

                var req = new Dictionary<string, string>
                {
                    { "merchantno", inputs[$"{currency}_MerchantNo"] },
                    { "morderno", inputs["TransID"] },
                    { "paycode", payCode },
                    { "notifyurl", inputs["CallBackUrl"] },
                    { "money", money },
                    { "sendtime", sendTime },
                    { "buyerip", inputs["ClientIP"] }
                };
                req.Add("sign", GetPostSign(req, inputs[$"{currency}_Key"]));
                req.Add("userinfo", inputs["UserName"]);
                req.Add("productname", "Service"); 
                
                // 当paycode = BDTTP, 用于选择支付所需要的用银行
                // Only for BDT CreateOrder
                if (currency == (int)CurrencyEnum.BDT)
                {
                    req.Add("bankcode", bankCode);
                }
                
                var result = CreateOrder(url, req);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.PayUrl
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation("AeePay create order failed: " + ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var depositId = formData["morderno"];
            var currency = payResource["CurrencyId"];
            var status = formData["status"];

            var callbackSignKeys= new List<string>
            {
                "merchantno",
                "orderno",
                "morderno",
                "paycode",
                "tjmoney",
                "money",
                "status"
            };

            if (!VerifySign(formData, callbackSignKeys, payResource[$"{currency}_Key"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = depositId,
                    TransStatus = TransStatus.Pending,
                    Response = $"TransId: {depositId}, Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = formData["orderno"],
                ActualAmount = decimal.Parse(formData["tjmoney"]),
                DepositId = depositId,
                Comment = $"status={status}", 
                TransStatus = Definition.GetDepositTransStatus(status),
                Response = CallbackResponse.Success
            };

            return result;
        }
        
        #endregion
        
        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var currency = int.Parse(inputs["CurrencyId"]);
                var url = $"{inputs[$"{currency}_Host"]}{Definition.Path.Withdrawal}";

                var dateTimeUtc08 = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0));
                var sendTime = dateTimeUtc08.ToString("yyyyMMddHHmmss");
                var money = decimal.Parse(inputs["Amount"]).ToString("F2");

                var req = new Dictionary<string, string>
                {
                    { "merchantno", inputs[$"{currency}_MerchantNo"] },
                    { "morderno  ", inputs["TransID"] },
                    { "bankcode", inputs["BankCode"] },
                    { "type", "0" },
                    { "realname", inputs["AccountName"] },
                    { "cardno", inputs["AccountNo"] },
                    { "money", money },
                    { "sendtime", sendTime },
                    { "buyerip ", inputs["ClientIP"] }
                };

                req.Add("sign", GetPostSign(req, inputs[$"{currency}_Key"]));
                req.Add("notifyUrl", inputs["CallBackUrl"]);

                var result = PayOut(url, req);

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = inputs["TransID"],
                    Message = result.OrderNo
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation(ex.Message);

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var withdrawalId = formData["morderno"];
            var currency = payResource["CurrencyId"];
            var callbackSignKeys= new List<string>
            {
                "merchantno",
                "orderno",
                "bankcode",
                "morderno",
                "cardno",
                "tjmoney",
                "money",
                "status"
            };
            
            var status = formData["status"];

            if (!VerifySign(formData, callbackSignKeys, payResource[$"{currency}_Key"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = withdrawalId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{withdrawalId}, Signature does not match."
                };
            }
            
            return new NotifyWithdrawResult
            {
                WithdrawId = withdrawalId,
                ThirdId = formData["orderno"],
                TransStatus = Definition.GetWithdrawalTransStatus(status),
                Response = CallbackResponse.Success
            };
        }
        #endregion
        
        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        private static string GetPayCode(string payCode)
        {
            var result = payCode.Split('-');

            return result[0];
        }

        private static string GetBankCode(string payCode)
        {
            var result = payCode.Split('-');

            return result.Length > 1 ? result[1] : string.Empty;
        }

        internal string GetPostSign(Dictionary<string, string> dic, string key)
        {
            SignConcatSourceString = string.Join('|', dic.Select(x => x.Value)) + $"|{key}";
            _logger.LogInformation($"SignSourceString: {SignConcatSourceString}");
            return ToMD5(SignConcatSourceString).ToLower();
        }

        internal bool VerifySign(Dictionary<string, string> receiveFormData, List<string> callbackSignKeys, string secretKey)
        {
            var filteredData = callbackSignKeys
                .ToDictionary(key => key, key => receiveFormData[key]);
            return receiveFormData["sign"].Equals(GetPostSign(filteredData, secretKey), StringComparison.OrdinalIgnoreCase);
        }

        private CreateOrderData CreateOrder(string url, Dictionary<string, string> req)
        {
            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, AeePayResponse<CreateOrderData>>(url, req);
                
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"AeePay deposit failed, HttpStatus: {(int)statusCode} {result}");
            }

            if (result != null && !result.Success)
            {
                throw new CustomException($"AeePay deposit failed, ResultCode:{result.ResultCode}, ResultMsg:{result.ResultMsg}");
            }

            return result.Data;
        }
        
        private CreatePayoutData PayOut(string url, Dictionary<string, string> req)
        {
            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, AeePayResponse<CreatePayoutData>>(url, req);
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"AeePay Withdrawal failed, HttpStatus: {(int)statusCode} {result}");
            }

            if (result != null && !result.Success)
            {
                throw new CustomException($"AeePay Withdrawal failed, ResultCode:{result.ResultCode}, ResultMsg:{result.ResultMsg}");
            }

            return result.Data;
        }
    }
}