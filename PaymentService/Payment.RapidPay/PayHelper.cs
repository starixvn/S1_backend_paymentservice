using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.RapidPay
{
    public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
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
            var apiHost = inputs["host"];
            var payCurrency = inputs["PayCurrencyId"];
            var (payCode, method) = GetMethod(inputs["PayCode"]);
            var depositInfo = inputs["DepositInfo"];
            
            try
            {
                // 檢查 DepositInfo
                if (depositInfo == null)
                    throw new CustomException("DepositInfo is empty.");
                
                var frag = string.IsNullOrEmpty(payCode) ? $"d{payCurrency}" : payCode;
                var username = inputs[$"{frag}User"];
                var req = new Dictionary<string, string>
                {
                    {"username", username},
                    {"api_key", inputs[$"{frag}API"]}
                };
                
                var authResp = GetAuth(apiHost, req);
                var amount = decimal.Parse(inputs["Amount"]);

                CheckCurrencyLimit(apiHost, username, payCurrency, amount);
                
                req = new Dictionary<string, string>
                {
                    {"username", inputs["UserName"]},
                    {"auth", authResp.Auth},
                    {"amount", amount.ToString("G29")},
                    {"currency", payCurrency},
                    {"orderid", inputs["TransID"]},
                    {"email", "test@test.com"},
                    {"phone_number", "123456789"}
                };

                var account = inputs["PayAccount"];
                if (!string.IsNullOrEmpty(method))
                {
                    req.Add("customer_bank_holder_name", inputs["AccountName"]);
                    req.Add("customer_bank_account", account);
                    req.Add("bank_pid", inputs["BankCode"]);
                    req.Add("pay_method", method);
                }
                
                var (resp, statusCode) = _httpHelper
                    .PostFormData<Dictionary<string, string>, DepositResp>(apiHost + Path.CreateOrder, req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException(
                        $"Generate orders failed, HttpStatus: {(int)statusCode} {statusCode}");

                if (!resp.Status)
                    throw new CustomException("Generate orders failed, msg=" + resp.Message);

                depositInfo = depositInfo.AddOrSetJProperty("KeyFrag", frag);
                
                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = resp.Url,
                    DepositInfo = depositInfo
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);
            var transId = data.TransId;
            try
            {
                var secretKeyName = DebitCallBackSecretKey(payResource);
                
                if (string.IsNullOrEmpty(secretKeyName))
                    secretKeyName = GetDebitCallBackSecretKey(data.Currency, data.PaymentType);
                
                var str = secretKeyName + data.TransId;
                var sign = ToMD5(str).ToLower();

                if (!data.Token.Equals(sign))
                   throw new CustomException($"{transId}, signature verification failed.");

                return new NotifyDepositResult
                {
                    ActualAmount = decimal.Parse(data.Amount),
                    DepositId = transId,
                    ThirdId = transId,
                    TransStatus = Definition.GetTransStatus(data.OrderStatus),
                    Response = CallbackResponse.Success
                };
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.Message);
                
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Comment = e.Message,
                    Response = CallbackResponse.Failed
                };
            }
        }
        
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiHost = inputs["host"];
            var payCurrency = inputs["PayCurrencyId"];
            
            try
            {
                var username = inputs[$"w{payCurrency}User"];
                var req = new Dictionary<string, string>
                {
                    {"username", username},
                    {"api_key", inputs[$"w{payCurrency}API"]}
                };
                
                var authResp = GetAuth(apiHost, req);
                var amount = decimal.Parse(inputs["Amount"]);
                CheckCurrencyLimit(apiHost, username, payCurrency, amount);
                
                req = new Dictionary<string, string>
                {
                    { "auth", authResp.Auth },
                    { "amount", amount.ToString("G29") },
                    { "currency", inputs["PayCurrencyId"] },
                    { "orderid", inputs["TransID"] },
                    { "bank_id", inputs["BankCode"] },
                    { "bank_branch", "" },
                    { "holder_name", inputs["AccountName"] },
                    { "account_no", inputs["AccountNo"] }
                };
                
                var (resp, statusCode) = _httpHelper
                    .PostFormData<Dictionary<string, string>, WithdrawResp>(apiHost + Path.CreatePayout, req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException(
                        $"Create payout orders failed, HttpStatus: {(int)statusCode} {statusCode}");

                if (!resp.Status)
                    throw new CustomException("Create payout orders failed, msg=" + resp.Message);

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = inputs["TransID"],
                    RawContent = JsonConvert.SerializeObject(resp)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(bodyContent);
            var transId = data.TransId;
            var str = payResource[$"w{data.Currency}Secret"] + data.TransId;
            var sign = ToMD5(str).ToLower();

            if (!data.Token.Equals(sign))
            {
                _logger.LogInformation($"{transId}, signature verification failed.");

                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Message = "signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            return new NotifyWithdrawResult
            {
                WithdrawId = transId,
                ThirdId = transId,
                Message = data.Remarks,
                TransStatus = Definition.GetTransStatus(data.OrderStatus),
                Response = CallbackResponse.Success
            };
        }
        
        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);
            return data.TransId;
        }
        
        
        private AuthResp GetAuth(string apiUrl, Dictionary<string, string> req)
        {
            var (result, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, AuthResp>(apiUrl + Path.Auth, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException(
                    $"Get auth failed, HttpStatus: {(int)statusCode} {statusCode}");

            if (!result.Status)
                throw new CustomException("Get auth failed, msg=" + result.Message);

            return result;
        }
        
        private void CheckCurrencyLimit(string apiUrl, string mUsername, string payCurrency, decimal amount)
        {
            var req = new Dictionary<string, string>
            {
                { "username", mUsername }
            };
            
            var (result, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, CurrencyResp>(apiUrl + Path.Currency, req);

            // 沒資料就直接忽略
            if (!statusCode.IsSuccessStatus()
                || !result.Status
                || !result.Limit.Any(x => x.Currency.Equals(payCurrency)))
                return;

            var limit = result.Limit.First(x => x.Currency.Equals(payCurrency));
            if (amount >= limit.Min && amount <= limit.Max)
                return;

            throw new CustomException($"amount={amount} over limit min={limit.Min}, max={limit.Max}");
        }

        private static (string PayCode, string Method) GetMethod(string payCode)
        {
            var sPayCode = payCode.Split("_");

            return (sPayCode[0], sPayCode.Length > 1 ? sPayCode[1] : string.Empty);
        }

        private static string DebitCallBackSecretKey(IDictionary<string, string> inputs)
        {
            var depositInfo = inputs["DepositInfo"];
            var jObj = JObject.Parse(depositInfo);

            return jObj.TryGetValue("KeyFrag", StringComparison.OrdinalIgnoreCase, out var jPayCode)
                ? jPayCode.ToObject<string>()
                : string.Empty;
        }
        
        private static string GetDebitCallBackSecretKey(string currency, string paymentType)
        {
            return paymentType switch
            {
                "EWallet" => currency switch
                {
                    "MYR" => "DuitnowSecret",
                    "THB" => "PromptPaySecret",
                    "INR" => "UPISecret",
                    _ => throw new CustomException("Invalid currency for EWallet")
                },
                "QR" => currency switch
                {
                    "MYR" => "DuitnowSecret",
                    "THB" => "PromptPaySecret",
                    "IDR" => "QRISSecret",
                    _ => throw new CustomException("Invalid currency for QR")
                },
                "Online Banking" => currency switch
                {
                    "MYR" => "dMYRSecret",
                    "THB" => "dTHBSecret",
                    "IDR" => "dIDRSecret",
                    _ => "Error - Invalid currency for Online Banking"
                },
                _ => throw new CustomException("Invalid paymentType")
            };
        }
    }
}