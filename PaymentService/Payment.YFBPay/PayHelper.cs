using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.YFBPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        internal string? SignConcatSourceString { get; private set; }

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
                var setting = GetSetting(inputs);
                var time = new DateTimeOffset(DateTime.Now);
                
                var req = new Dictionary<string, string>
                {
                    { "merchantNo", setting.MerchantNo },
                    { "orderNo", inputs["TransID"] },
                    { "userNo", inputs["CustId"] },
                    { "amount", decimal.Parse(inputs["Amount"]).ToString("F2") },
                    { "datetime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                    { "discount", string.Empty },
                    { "extra", string.Empty },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "time", time.ToUnixTimeSeconds().ToString() }
                };
                // above properties are specific for sign use
                req.Add("sign", GetSign(req, setting.Key));
                req.Add("channelNo", inputs["PayCode"]);
                req.Add("payeeName", inputs["CustId"]);
                req.Add("appSecret", setting.AppSecret);

                var apiUrl = $"{setting.Host.TrimEnd('/')}{Path.Deposit}";
                var res = CreateOrder(apiUrl, req);
                var result = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    ThirdId = res.TradeNo,
                    Message = res.Text,
                    Url = res.TargetUrl
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation($"YFBPay deposit failed, Code:{exception.Code}, Message:{exception.Message}");

                return new DepositResult(false, exception.Code, exception.Message);
            }
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

            var checkSignData = new Dictionary<string, string>
            {
                { "amount", formData["amount"] },
                { "discount", formData["discount"] },
                { "extra", formData["extra"] },
                { "lucky", formData["lucky"] },
                { "orderNo", formData["orderNo"] },
                { "paid", formData["paid"] },
                { "status", formData["status"] },
                { "tradeNo", formData["tradeNo"] },
                { "userNo", formData["userNo"] }
            };

            if (!ValidateSign(checkSignData, formData["sign"], setting.Key))
            {
                return new NotifyDepositResult
                {
                    DepositId = formData["orderNo"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetDepositTransStatus(formData["status"]);
            var result = new NotifyDepositResult
            {
                ThirdId = formData["tradeNo"],
                DepositId = formData["orderNo"],
                Comment = formData["status"],
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(formData["amount"]),
                Response = Definition.CallbackResponse
            };

            return result;
        }
        #endregion

        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var setting = GetSetting(inputs);
                var time = new DateTimeOffset(DateTime.Now);
                var isInr = inputs["PayCurrencyId"].Equals("inr", StringComparison.OrdinalIgnoreCase);

                var req = new Dictionary<string, string>
                {
                    { "merchantNo", setting.MerchantNo },
                    { "orderNo", inputs["TransID"] },
                    { "amount", double.Parse(inputs["Amount"]).ToString("F0") },
                    { "name", inputs["AccountName"] },
                    { "bankName", inputs["BankName"] }, 
                    { "bankAccount", inputs["AccountNo"] },
                    { "mobile", string.Empty },
                    { "datetime", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                    { "extra", string.Empty },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "reverseUrl", string.Empty },
                    { "time", time.ToUnixTimeSeconds().ToString() }
                };
                // above properties are specific for sign use
                req.Add("sign", GetSign(req, setting.Key));
                req.Add("bankBranch", isInr ? inputs["IFSCCode"] : "N/A");
                req.Add("memo", string.Empty);
                req.Add("appSecret", setting.AppSecret);

                var apiUrl = $"{setting.Host.TrimEnd('/')}{Path.Withdrawal}";
                var res = CreatePayout(apiUrl, req);

                var result = new WithdrawResult
                {
                    Success = true,
                    Message = res.Code
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new WithdrawResult(exception.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var payCurrency = Definition.GetPayCurrency(payResource["CurrencyId"]);
            payResource.Add("PayCurrencyId", payCurrency);
            var setting = GetSetting(payResource);

            var checkSignData = new Dictionary<string, string>
            {
                { "amount", formData["amount"] },
                { "bankAccount", formData["bankAccount"] },
                { "bankName", formData["bankName"] },
                { "extra", formData["extra"] },
                { "fee", formData["fee"] },
                { "mobile", formData["mobile"] },
                { "name", formData["name"] },
                { "orderNo", formData["orderNo"] },
                { "status", formData["status"] },
                { "tradeNo", formData["tradeNo"] }
            };

            if (!ValidateSign(checkSignData, formData["sign"], setting.Key))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = formData["orderNo"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetWithdrawalTransStatus(formData["status"]);
            var result = new NotifyWithdrawResult
            {
                WithdrawId = formData["orderNo"],
                ThirdId = formData["tradeNo"],
                TransStatus = transStatus,
                Response = Definition.CallbackResponse,
                Message = formData["status"]
            };

            return result;
        }
        #endregion

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

        internal string GetSign(Dictionary<string, string> parameters, string key)
        {
            var sortedParameters = parameters.OrderBy(p => p.Key);
            var queryString = string.Join("&", sortedParameters.Select(x => $"{x.Key}={x.Value}").ToArray());
            _logger.LogInformation(queryString);

            var signConcatString = $"{queryString}{key}";
            SignConcatSourceString = signConcatString;

            var signSha256 = ToSHA256(signConcatString).ToLower();
            var result = ToMD5(signSha256);
            _logger.LogInformation(result);

            return result;
        }

        internal bool ValidateSign(Dictionary<string, string> parameters, string sign, string key)
        {
            var calculatedSign = GetSign(parameters, key);
            return calculatedSign.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private DepositResponse CreateOrder(string apiUrl, Dictionary<string, string> req)
        {
            var (response, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, DepositResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"YFBPay CreateOrder failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.Code != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"YFBPay CreateOrder failed: expected code={Definition.SuccessCode}, actual code={response.Code} text={response.Text}");
            }

            return response;
        }

        private WithdrawalResponse CreatePayout(string apiUrl, Dictionary<string, string> req)
        {
            var (response, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, WithdrawalResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"YFBPay CreatePayout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.Code != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"YFBPay CreatePayout failed: expected code={Definition.SuccessCode}, actual code={response.Code} text={response.Text}");
            }

            return response;
        }

        private Setting GetSetting(Dictionary<string, string> inputs)
        {
            var payCurrency = inputs["PayCurrencyId"];

            if (!inputs.ContainsKey(payCurrency))
            {
                throw new CustomException($"YFBPay missing {payCurrency} setting.");
            }

            var setting = JsonConvert.DeserializeObject<Setting>(inputs[payCurrency]);

            if (setting == null)
            {
                throw new CustomException($"YFBPay {payCurrency} setting is null.");
            }

            return setting;
        }
    }
}
