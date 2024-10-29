using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.GameWallet
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private readonly string[] _specificBankValuesForAmountFormat = { "MOMO", "ZALOPAY", "VTTMO" };
        private DateTimeOffset? _currentTimeStamp;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        internal DateTimeOffset CurrentTimeStamp
        {
            private get => _currentTimeStamp.HasValue
                ? _currentTimeStamp.Value
                : DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8, 0, 0));
            set => _currentTimeStamp = value;
        }

        internal string? SignConcatSourceString { get; private set; }

        public override NotifyWithdrawResult CallBackCredit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var payCurrency = Definition.GetPayCurrency(payResource["CurrencyId"]);
            payResource.Add("PayCurrencyId", payCurrency);
            var setting = GetSetting(payResource);
            var req = FromJson<WithdrawalCallBackRequest>(bodyContent);

            if (!VerifySign(req, setting.Key))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.Reference,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetCreditTransStatus(req.StatusCode);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = req.Reference,
                ThirdId = req.TransactionId,
                TransStatus = transStatus,
                Response = Definition.CallBackResponse,
                Message = req.StatusMessage
            };

            return result;
        }

        public override NotifyDepositResult CallBackDebit(
        	Dictionary<string, string> formData,
        	string bodyContent,
        	Dictionary<string, string> headers,
        	Dictionary<string, string> payResource)
        {
            var payCurrency = Definition.GetPayCurrency(payResource["CurrencyId"]);
            payResource.Add("PayCurrencyId", payCurrency);
            var setting = GetSetting(payResource);
            var callbackRequest = FromJson<DepositCallbackRequest>(bodyContent);

            if (!ValidateSign(callbackRequest, setting.Key))
            {
                return new NotifyDepositResult
                {
                    DepositId = payResource["TransID"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = Definition.GetTransStatus(callbackRequest.statusCode);
            return new NotifyDepositResult
            {
                ThirdId = callbackRequest.transactionId,
                DepositId = callbackRequest.reference,
                Comment = $"statusCode={callbackRequest.statusCode} statusMessage={callbackRequest.statusMessage}",
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(callbackRequest.amount),
                Response = Definition.CallBackResponse
            };
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var setting = GetSetting(inputs);
                var post = new WithdrawalPost
                {
                    accountName = inputs["AccountName"],
                    accountNumber = inputs["AccountNo"],
                    amount = double.Parse(inputs["Amount"]).ToString("F2"),
                    bank = inputs["BankCode"],
                    callbackUri = inputs["CallBackUrl"],
                    currency = inputs["PayCurrencyId"],
                    customer = inputs["CustId"],
                    datetime = CurrentTimeStamp.ToString("yyyyMMddHHmmss"),
                    merchant = setting.MerchantCode,
                    note = string.Empty,
                    reference = inputs["TransID"]
                };
                post.key = GetCreditPostSign(post, setting.Key);
                post.datetime = CurrentTimeStamp.ToString("yyyy-MM-dd HH:mm:sstt", new CultureInfo("en-us")); // for post to 3rdParty format

                var withdrawalResponse = CreatePayout(setting.PayoutHost, post);
                var result = new WithdrawResult
                {
                    Success = true,
                    Message = withdrawalResponse.statusCode
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new WithdrawResult(exception.Message);
            }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            try
            {
                var setting = GetSetting(inputs);
                var (method, bank) = GetMethodBankCode(inputs);
                var post = new DepositPost
                {
                    Merchant = setting.MerchantCode,
                    Currency = inputs["PayCurrencyId"],
                    Method = method,
                    Bank = bank,
                    Customer = inputs["CustId"],
                    Reference = inputs["TransID"],
                    Amount = decimal.Parse(inputs["Amount"]).ToString("F2"),
                    Note = string.Empty,
                    SuccessUrl = inputs["ReturnUrl"],
                    FailUrl = inputs["ReturnUrl"],
                    Datetime = CurrentTimeStamp.ToString("yyyyMMddHHmmss"),
                    CallbackUri = inputs["CallBackUrl"],
                    Language = inputs["PayLangId"],
                    ClientIP = inputs["ClientIP"]
                };
                post.Key = GetPostSign(post, setting.Key);
                post.Datetime = CurrentTimeStamp.ToString("yyyy-MM-dd HH:mm:sstt", new CultureInfo("en-us"));
                if (_specificBankValuesForAmountFormat.Contains(bank))
                {
                    post.Amount = decimal.Parse(inputs["Amount"]).ToString("F0");
                }

                var postData = ToJson(post);
                var parameter = HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(postData)));
                var url = $"{setting.Host.TrimEnd('/')}?parameter={parameter}";
                _logger.LogInformation($"Origin PostData: {postData}, Url: {url}");

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = url
                };
            }
            catch (CustomException exception)
            {
                _logger.LogInformation($"GameWallet deposit failed, Code:{exception.Code}, Message:{exception.Message}");

                return new DepositResult(false, exception.Code, exception.Message);
            }
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        internal string GetCreditPostSign(WithdrawalPost data, string key)
        {
            var signConcatSourceString =
                $"{data.merchant}{data.reference}{data.customer}{data.amount}{data.currency}{data.datetime}{key}";
            SignConcatSourceString = signConcatSourceString;

            return ToMD5(signConcatSourceString).ToLower();
        }

        internal string GetPostSign(DepositPost data, string key)
        {
            var signConcatSourceString =
                $"{data.Merchant}{data.Reference}{data.Customer}{data.Amount}{data.Currency}{data.Datetime}{key}{data.ClientIP}";
            SignConcatSourceString = signConcatSourceString;

            return ToMD5(signConcatSourceString).ToLower();
        }

        internal bool ValidateSign(DepositCallbackRequest data, string key)
        {
            var signConcatSourceString =
                $"{data.merchant}{data.reference}{data.customer}{data.amount}{data.currency}{data.statusCode}{key}";
            SignConcatSourceString = signConcatSourceString;

            return ToMD5(signConcatSourceString)
                .Equals(data.key, StringComparison.OrdinalIgnoreCase);
        }

        internal bool VerifySign(WithdrawalCallBackRequest data, string key)
        {
            var signConcatSourceString = 
                $"{data.Merchant}{data.Reference}{data.Customer}{data.Amount}{data.Currency}{data.StatusCode}{key}";
            SignConcatSourceString = signConcatSourceString;

            return ToMD5(signConcatSourceString)
                .Equals(data.key, StringComparison.OrdinalIgnoreCase);
        }

        private WithdrawalResponse CreatePayout(string url, WithdrawalPost post)
        {
            var (response, _) = _httpHelper
                .PostJson<WithdrawalPost, WithdrawalResponse>(url, post);

            if (response.statusCode != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"GameWallet create payout failed: statusCode={response.statusCode}, statusMessage={response.statusMessage}");
            }

            return response;
        }

        private (string method, string bank) GetMethodBankCode(Dictionary<string, string> inputs)
        {
            string bank;

            var methods = inputs["PayCode"].Split('-');
            var method = methods[0];
            if (methods.Length > 1)
            {
                bank = methods[1];
            }
            else
            {
                bank = inputs.TryGetValue("BankCode", out var bankCode) ? bankCode : string.Empty;
            }

            return (method, bank);
        }

        private GameWalletSetting GetSetting(Dictionary<string, string> inputs)
        {
            var payCurrency = inputs["PayCurrencyId"];

            if (!inputs.ContainsKey(payCurrency))
            {
                throw new CustomException($"GameWallet missing {payCurrency} setting.");
            }

            var setting = JsonConvert.DeserializeObject<GameWalletSetting>(inputs[payCurrency]);

            if (setting == null)
            {
                throw new CustomException($"GameWallet {payCurrency} setting is null.");
            }

            return setting;
        }
    }
}
