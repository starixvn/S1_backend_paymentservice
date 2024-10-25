using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.FPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
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
            var apiHost = inputs["Host"];
            var payCurrency = GetPayCurrency(inputs["PayCurrencyId"]);
            var settingKey = GetSettingKey(inputs["PayCurrencyId"]);
            var depositInfo = inputs["DepositInfo"];
            
            try
            {
                // 檢查 DepositInfo
                if (depositInfo == null)
                    throw new CustomException("DepositInfo is empty.");
                
                var req = new Dictionary<string, string>
                {
                    {"username", inputs[$"{settingKey}DepositUsername"]},
                    {"api_key", inputs[$"{settingKey}DepositApiKey"]}
                };
                
                var authResp = GetAuth(apiHost, req);
                var amount = decimal.Parse(inputs["Amount"]);
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
                
                var (resp, statusCode) = _httpHelper
                    .PostFormData<Dictionary<string, string>, DepositResp>(apiHost + Path.CreateOrder, req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException(
                        $"Generate orders failed, HttpStatus: {(int)statusCode} {statusCode}");

                if (!resp.Status)
                    throw new CustomException("Generate orders failed, msg=" + resp.Message);

                depositInfo = depositInfo.AddOrSetJProperty("SettingKey", settingKey);

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
        
        /* staging 可從後台回調
         * https://sandbox2.fpay.support/login
         * acc: Nova88test
         * pw: 1234abcd
         */
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(bodyContent);
            var transId = data.TransId;
            
            var settingKey = GetDepositCallBackSettingKey(payResource);
            var str = payResource[$"{settingKey}DepositSecretKey"] + data.TransId;
            var sign = ToMD5(str).ToLower();

            if (!data.Token.Equals(sign))
            {

                _logger.LogInformation($"{transId}, signature verification failed.");

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            var result = new NotifyDepositResult
            {
                ActualAmount = decimal.Parse(data.Amount),
                DepositId = transId,
                ThirdId = transId,
                TransStatus = Definition.GetTransStatus(data.OrderStatus),
                Response = CallbackResponse.Success
            };

            return result;
        }
        
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiHost = inputs["Host"];
            var payCurrency = GetPayCurrency(inputs["PayCurrencyId"]);
            var settingKey = GetSettingKey(inputs["PayCurrencyId"]);
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    {"username", inputs[$"{settingKey}PayoutUsername"]},
                    {"api_key", inputs[$"{settingKey}PayoutApiKey"]}
                };
                
                var authResp = GetAuth(apiHost, req);
                var amount = decimal.Parse(inputs["Amount"]);
                req = new Dictionary<string, string>
                {
                    { "auth", authResp.Auth },
                    { "amount", amount.ToString("G29") },
                    { "currency", payCurrency },
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

        /* staging 可從後台回調
         * https://sandbox2.fpay.support/login
         * acc: Nova88_test
         * pw: 1234abcd
         */
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(bodyContent);
            var transId = data.TransId;
            var str = payResource[$"{data.Currency}PayoutSecretKey"] + data.TransId;
            var sign = ToMD5(str).ToLower();

            if (!data.Token.Equals(sign))
            {
                _logger.LogInformation($"{transId}, signature verification failed.");

                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Message = "Signature verification failed.",
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

        private static string GetSettingKey(string payCurrency)
            => payCurrency.Replace("_", "");
        
        private static string GetPayCurrency(string payCurrency)
            => payCurrency.Split("_")[0];
        
        private static string GetDepositCallBackSettingKey(IDictionary<string, string> inputs)
        {
            var depositInfo = inputs["DepositInfo"];
            var jObj = JObject.Parse(depositInfo);

            return jObj.TryGetValue("SettingKey", StringComparison.OrdinalIgnoreCase, out var jPayCode)
                ? jPayCode.ToObject<string>()
                : string.Empty;
        }
    }
}
