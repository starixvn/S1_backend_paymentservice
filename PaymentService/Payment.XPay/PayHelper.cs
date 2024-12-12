using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.XPay
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

        #region Deposit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var payCurrency = input["PayCurrencyId"];
            var url = $"{input["Host"]}{Path.CreateOrder}";
            
            try
            {
                var amount = decimal.Parse(input["Amount"]) * 100;
                var transId = input["TransID"];
                var req = new Dictionary<string, string>
                {
                    { "service_version", input["PayCode"] },
                    { "partner_code", input["MerchantId"] },
                    { "partner_orderid", transId },
                    { "member_id", input["CustId"] },
                    { "currency", payCurrency },
                    { "amount", amount.ToString("F0") },
                    { "account_name", input["AccountName"] },
                    { "bank_code", input["BankCode"] },
                    { "notify_url", input["CallBackUrl"] },
                };
                req.Add("sign", GetSign(req, input["Key"]));

                var (result, statusCode) =
                    _httpHelper.PostUrlEncoded<Dictionary<string, string>, CreatOrderResp>(url, req);
                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Code={statusCode}");
                }
                
                if (string.IsNullOrEmpty(result.displaypage))
                {
                    throw new CustomException($"XPay deposit failed! displaypage no value)");
                }
               
                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.displaypage,
                    ThirdId = result.billno
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation("XPay deposit failed: " + ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = formData["partner_orderid"];
            var status = formData["status"];
            var thirdId = formData["billno"];
            var amount = formData["deposit_amount"];

            try
            {
                var receiveModel = new Dictionary<string, string>
                {
                    { "service_version", formData["service_version"] },
                    { "billno", thirdId },
                    { "partner_orderid", formData["partner_orderid"] },
                    { "currency", formData["currency"] },
                    { "original_amount", formData["original_amount"] },
                    { "deposit_amount", amount },
                    { "status", status }
                };

                if (!VerifySign(receiveModel, payResource["Key"], formData["sign"]))
                {
                    throw new CustomException($"{transId} signature verification failed.");
                }
                
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    ThirdId = thirdId,
                    Comment = $"status={status}",
                    TransStatus = Definition.GetTransStatus(status),
                    ActualAmount = decimal.Parse(amount) / 100,
                    Response = Definition.GenerateCallbackResponse(thirdId)
                };
            }
            catch (CustomException ex)
            {
                return new NotifyDepositResult
                {
                    DepositId = payResource["TransID"],
                    TransStatus = TransStatus.Pending,
                    Response = ex.Message
                };
            }
        }

        #endregion

        #region Withdrawal

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var payCurrency = inputs["PayCurrencyId"];
            var url = $"{inputs["Host"]}{Path.CreatePayout}";
            var transId = inputs["TransID"];

            try
            {
                var amount = decimal.Parse(inputs["Amount"]) * 100;
                var req = new Dictionary<string, string>
                {
                    { "service_version", inputs["PayCode"] },
                    { "partner_code", inputs["MerchantId"] },
                    { "partner_orderid", transId },
                    { "member_id", inputs["CustId"] },
                    { "currency", payCurrency },
                    { "amount", amount.ToString("F0") },
                    { "account_name", inputs["AccountName"] },
                    { "bank_code", inputs["BankCode"] },
                    { "notify_url", inputs["CallBackUrl"] }
                };

                req.Add("sign", GetSign(req, inputs["Key"]));
                req.Add("account_number", inputs["AccountNo"]);
                req.Add("bank_province", "N/A");
                req.Add("bank_city", "N/A");
                req.Add("bank_branch", "N/A");
                
                var (result, statusCode) =
                    _httpHelper.PostUrlEncoded<Dictionary<string, string>, CreatPayoutResp>(url, req);
                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Code={statusCode}");
                }
                
                var successStatus = new List<string> { "000", "001" };
                if (!successStatus.Contains(result.status))
                {
                    throw new CustomException($"XPay withdrawal failed! status={result.status}, error_code={result.error_code}, error_description={result.error_description}");
                }
               
                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation(ex.Message);

                return new WithdrawResult
                {
                    Success = false,
                    WithdrawId = transId,
                    Message = ex.Message
                };
            }
        }
        
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = formData["partner_orderid"];
            var thirdId = formData["billno"];
            var receive = new Dictionary<string, string>
            {
                { "service_version", formData["service_version"] },
                { "billno", thirdId },
                { "partner_orderid", transId },
                { "currency", formData["currency"] },
                { "amount", formData["amount"] },
                { "account_name", formData["account_name"] },
                { "account_number", formData["account_number"] },
                { "bank_code", formData["bank_code"] },
                { "status", formData["status"] },
                { "error_code", formData["error_code"] },
                { "error_description", formData["error_description"] }
            };
            
            if (!VerifySign(receive, payResource["Key"], formData["sign"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{transId} signature verification failed."
                };
            }
            
            return new NotifyWithdrawResult
            {
                ThirdId = thirdId,
                TransStatus = Definition.GetTransStatus(formData["status"]),
                WithdrawId = transId,
                Message = $"status={formData["status"]}",
                Response = Definition.GenerateCallbackResponse(thirdId)
            };
        }

        #endregion

        #region NotImplemented
        
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
        
        #endregion

        #region private

        internal string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            SignConcatSourceString = string.Join("&", param
                    .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

            _logger.LogInformation("SignConcatSourceString: " + SignConcatSourceString);
            var sign = ToSHA1(SignConcatSourceString).ToUpper();

            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string key , string target)
        {
            var sign = GetSign(param, key);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}