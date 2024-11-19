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
            var requestAmount = decimal.Parse(input["Amount"]).ToString("F2");

            var (method, code) = GetMethod(payCode);

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantNo", input[$"{payCurrencyId}MerchantNo"] },
                    { "orderNo", transId },
                    { "userNo", input["CustId"] },
                    { "amount", requestAmount },
                    { "time", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                    { "extra", payCurrencyId },
                    { "notifyUrl", input["CallBackUrl"] }
                };

                if (payCurrencyId.Equals("INR", StringComparison.OrdinalIgnoreCase))
                {
                    req.Add("discount", "");
                    req.Add("datetime", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    req.Add("userName", input["CustId"]);
                    req.Add("channelNo", method);
                    req.Add("payeeName", input["CustId"]);
                    req.Add("returnUrl", input["ReturnUrl"]);
                    req.Add("appSecret", input[$"{payCurrencyId}AppSecret"]);

                    var sign = GetInrDepositPostSign(req, input[$"{payCurrencyId}SecretKey"]);
                    req.Add("sign", sign);
                }
                else
                {
                    req.Add("userName", "");
                    req.Add("channelNo", method);
                    req.Add("realName", "");
                    req.Add("bankCode", string.IsNullOrEmpty(code) ? input["BankCode"] : code);
                    req.Add("appSecret", input[$"{payCurrencyId}AppSecret"]);

                    var sign = GetPostSign(req, input[$"{payCurrencyId}SecretKey"]);
                    req.Add("sign", sign);
                }

                var url = $"{apiHost}{Definition.GetDepositPath(payCurrencyId)}";
                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, DepositResponse>(url, req);

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

            if (payCurrencyId.Equals("INR", StringComparison.OrdinalIgnoreCase))
            {
                receiveModel.Add("discount", formData["discount"]);
            }

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
                var requestAmount = decimal.Parse(inputs["Amount"]).ToString("F0");
                var req = new Dictionary<string, string>
                {
                    { "merchantNo", inputs[$"{payCurrencyId}MerchantNo"] },
                    { "orderNo", transId },
                    { "amount", requestAmount },
                    { "name", inputs["AccountName"] },
                    { "bankAccount", inputs["AccountNo"] },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "extra", payCurrencyId },
                    { "time", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                };

                if (payCurrencyId.Equals("INR",StringComparison.OrdinalIgnoreCase))
                {
                    req.Add("bankName", inputs["BankName"]);
                    req.Add("mobile", "");
                    req.Add("datetime", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    req.Add("reverseUrl", inputs["CallBackUrl"]);
                    req.Add("bankBranch", inputs["IFSCCode"]);
                    req.Add("appSecret", inputs[$"{payCurrencyId}AppSecret"]);

                    var sign = GetInrWithdrawalPostSign(req, inputs[$"{payCurrencyId}SecretKey"]);
                    req.Add("sign", sign);
                }
                else
                {
                    req.Add("bankName", inputs["BankCode"]);
                    req.Add("appSecret", inputs[$"{payCurrencyId}AppSecret"]);
                    req.Add("reverseUrl", inputs["CallBackUrl"]);

                    var sign = GetPostSign(req, inputs[$"{payCurrencyId}SecretKey"]);
                    req.Add("sign", sign);
                }

                var url = $"{apiHost}{Definition.GetWithdrawalPath(payCurrencyId)}";
                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, WithdrawalResponse>(url, req);

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

            if (payCurrencyId.Equals("INR",StringComparison.OrdinalIgnoreCase))
            {
                receiveModel.Add("mobile", formData["mobile"]);
            }

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

        internal string GetInrDepositPostSign(Dictionary<string, string> param, string secretKey)
        {
            var sortedParameters = param
                .Where(x => !x.Key.Equals("userName", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("channelNo", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("payeeName", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("amountBeforeFixed", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("returnUrl", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("appSecret", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("bankName", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key, StringComparer.Ordinal);
            SignConcatSourceString = string.Join("&", sortedParameters.Select(x => $"{x.Key}={x.Value}").ToArray());
            var encodeStr = $"{SignConcatSourceString}{secretKey}";
            _logger.LogInformation(encodeStr);

            var sign = Encrypt(encodeStr);
            return sign;
        }

        internal string GetInrWithdrawalPostSign(Dictionary<string, string> param, string secretKey)
        {
            var sortedParameters = param
                .Where(x => !x.Key.Equals("bankBranch", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("memo", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Key.Equals("appSecret", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key, StringComparer.Ordinal);
            SignConcatSourceString = string.Join("&", sortedParameters.Select(x => $"{x.Key}={x.Value}").ToArray());
            var encodeStr = $"{SignConcatSourceString}{secretKey}";
            _logger.LogInformation(encodeStr);

            var sign = Encrypt(encodeStr);
            return sign;
        }

        internal string GetPostSign(Dictionary<string, string> param, string secretKey)
        {
            var sortedParameters = param
                .Where(x => !x.Key.Equals("reverseUrl", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key, StringComparer.Ordinal);
            SignConcatSourceString = string.Join("&", sortedParameters.Select(x => $"{x.Key}={x.Value}").ToArray());
            var encodeStr = $"{SignConcatSourceString}{secretKey}";
            _logger.LogInformation(encodeStr);

            var sign = Encrypt(encodeStr);
            return sign;
        }

        private string Encrypt(string encodeStr)
        {
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