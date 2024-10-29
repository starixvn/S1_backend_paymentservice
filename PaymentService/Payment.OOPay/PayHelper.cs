using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.OOPay
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

        internal static string? SignConcatSourceString { get; private set; }

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var payCurrencyId = input["PayCurrencyId"];
            var apiHost = input[$"{payCurrencyId}Host"];
            var transId = input["TransID"];
            var payCode = input["PayCode"];
            var (method, code) = GetMethod(payCode);

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantNo", input[$"{payCurrencyId}MerchantNo"] },
                    { "orderNo", transId },
                    { "userNo", "" },
                    { "userName", "" },
                    { "channelNo", method },
                    { "amount", input["Amount"] },
                    { "realName", "" },
                    { "bankCode", string.IsNullOrEmpty(code) ? input["BankCode"] : code },
                    { "extra", payCurrencyId },
                    { "notifyUrl", input["CallBackUrl"] },
                    { "time", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                    { "appSecret", input[$"{payCurrencyId}AppSecret"] }
                };

                var sign = GetPostSign(req, input[$"{payCurrencyId}SecretKey"]);
                req.Add("sign", sign);

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, DepositResponse>($"{apiHost}{Path.Deposit}", req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException($"HttpStatus: {statusCode}", ((int)statusCode).ToString());

                if (!Definition.SuccessCode.Equals(response.Code))
                    throw new CustomException(response.Text, response.Code);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = response.TargetUrl,
                    ThirdId = response.TradeNo
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code={ex.Code}, Message={ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var thirdId = formData["tradeNo"];
            var status = formData["status"];
            var transId = formData["orderNo"];
            var amount = formData["amount"];
            var payCurrencyId = formData["extra"];
            var receiveModel = new Dictionary<string, string>
            {
                { "status", status },
                { "tradeNo", thirdId },
                { "orderNo", transId },
                { "userNo", formData["userNo"] },
                { "userName", formData["userName"] },
                { "channelNo", formData["channelNo"] },
                { "amount", amount },
                { "lucky", formData["lucky"] },
                { "paid", formData["paid"] },
                { "extra", payCurrencyId }
            };

            var sign = formData["sign"];
            if (!VerifySign(sign, receiveModel, input[$"{payCurrencyId}SecretKey"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = decimal.Parse(amount),
                ThirdId = thirdId,
                Comment = status,
                TransStatus = Definition.GetDepositTransStatus(status),
                Response = CallbackResponse.Success
            };
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var payCurrencyId = inputs["PayCurrencyId"];
            var apiHost = inputs[$"{payCurrencyId}Host"];
            var transId = inputs["TransID"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantNo", inputs[$"{payCurrencyId}MerchantNo"] },
                    { "orderNo", transId },
                    { "amount", inputs["Amount"] },
                    { "name", inputs["AccountName"] },
                    { "bankName", inputs["BankCode"] },
                    { "bankAccount", inputs["AccountNo"] },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "extra", payCurrencyId },
                    { "time", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                    { "appSecret", inputs[$"{payCurrencyId}AppSecret"] }
                };

                var sign = GetPostSign(req, inputs[$"{payCurrencyId}SecretKey"]);
                req.Add("reverseUrl", inputs["CallBackUrl"]);
                req.Add("sign", sign);

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, WithdrawalResponse>($"{apiHost}{Path.Withdrawal}", req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException($"HttpStatus: {statusCode}", ((int)statusCode).ToString());

                if (!Definition.SuccessCode.Equals(response.Code))
                    throw new CustomException(response.Text, response.Code);

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId,
                    RawContent = JsonConvert.SerializeObject(response)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Withdrawal failed, Code={ex.Code}, Message={ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    WithdrawId = transId,
                    Message = ex.Message
                };
            }

        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var thirdId = formData["tradeNo"];
            var status = formData["status"];
            var transId = formData["orderNo"];
            var amount = formData["amount"];
            var payCurrencyId = formData["extra"];

            var receiveModel = new Dictionary<string, string>
            {
                { "status", status },
                { "tradeNo", thirdId },
                { "orderNo", transId },
                { "amount", amount },
                { "name", formData["name"] },
                { "bankName", formData["bankName"] },
                { "bankAccount", formData["bankAccount"] },
                { "fee", formData["fee"] },
                { "extra", payCurrencyId }
            };

            var sign = formData["sign"];
            if (!VerifySign(sign, receiveModel, payResource[$"{payCurrencyId}SecretKey"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            return new NotifyWithdrawResult
            {
                ThirdId = thirdId,
                TransStatus = Definition.GetPayoutTransStatus(status),
                Response = CallbackResponse.Success,
                WithdrawId = transId
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion
        private static (string method, string code) GetMethod(string payCode)
        {
            var result = payCode.Split('-');

            return (result[0], result.Length > 1 ? result[1] : string.Empty);
        }

        internal string GetPostSign(Dictionary<string, string> param, string secretKey)
        {
            var sortedParameters = param.OrderBy(p => p.Key, StringComparer.Ordinal);
            SignConcatSourceString = string.Join("&", sortedParameters.Select(x => $"{x.Key}={x.Value}").ToArray());
            var encodeStr = $"{SignConcatSourceString}{secretKey}";
            _logger.LogInformation(encodeStr);

            var signSha256 = ToHMACSHA256(encodeStr);
            _logger.LogInformation(signSha256);

            var sign = ToMD5(signSha256).ToUpper();
            _logger.LogInformation(sign);

            return sign;
        }

        internal bool VerifySign(string receiveSign, Dictionary<string, string> reqForSign, string secretKey)
        {
            var sign = GetPostSign(reqForSign, secretKey);
            return receiveSign.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

    }
}