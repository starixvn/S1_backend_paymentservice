using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var (method, bankCode, channelType) = GetMethodBankCode(inputs);
            var post = new DepositPost
            {
                Merchant = inputs["MerchantCode"],
                Currency = inputs["PayCurrencyId"],
                Method = method,
                Bank = bankCode,
                PaymentChannelType = channelType,
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
            post.Key = GetPostSign(post, inputs["Key"]);
            post.Datetime = CurrentTimeStamp.ToString("yyyy-MM-dd HH:mm:sstt", new CultureInfo("en-us"));

            var postData = ToJson(post);
            var based64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(postData));
            var param = ParameterEncoder.Encode(based64);

            var parameter = HttpUtility.UrlEncode(param);
            var url = $"{inputs["DepositUrl"]}Home/2.0/Deposit/?parameter={parameter}";
            _logger.LogInformation($"Origin PostData: {postData}, Url: {url}");

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                Url = url
            };
        }

        public override NotifyDepositResult CallBackDebit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var callbackRequest = FromJson<DepositCallbackRequest>(bodyContent);

            if (!ValidateSign(callbackRequest, payResource["Key"]))
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

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {

            try
            {
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
                    merchant = inputs["MerchantCode"],
                    note = string.Empty,
                    reference = inputs["TransID"]
                };
                post.key = GetCreditPostSign(post, inputs["Key"]);
                post.datetime = CurrentTimeStamp.ToString("yyyy-MM-dd HH:mm:sstt", new CultureInfo("en-us")); // for post to 3rdParty format

                var withdrawalResponse = CreatePayout(inputs["WithdrawalUrl"], post);
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

        public override NotifyWithdrawResult CallBackCredit(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            
            var req = FromJson<WithdrawalCallBackRequest>(bodyContent);

            if (!VerifySign(req, payResource["Key"] ))
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

        private (string method, string bankCode, string channelType) GetMethodBankCode(Dictionary<string, string> inputs)
        {
            var methodKey = inputs["PayCode"];

            return methodKey switch
            {
                "Onl" => (
                    method: "1",
                    bankCode: inputs.TryGetValue("BankCode", out var bankCode) ? bankCode : string.Empty,
                    channelType: "1"
                ),
                "QR" => (
                    method: "3",
                    bankCode: "AUTO",
                    channelType: "2"
                ),
                "True" => (
                    method: "3",
                    bankCode: "TRUEWALLET",
                    channelType: "3"
                ),
                _ => throw new NotImplementedException()
            };
        }


    }
    public class ParameterEncoder
    {
        public static string Encode(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var length = plainText.Length;
            if (length < 100)
                throw new InvalidDataException("Invalid plainText");

            var key = new Random().Next(1, 10);
            return plainText.Insert(length - 10, $"{key}").Insert(key, Guid.NewGuid().ToString("n").Substring(0, 1));
        }
    }
}
