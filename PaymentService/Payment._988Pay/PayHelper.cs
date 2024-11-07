using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

// 988Pay and 808Pay document is same
namespace Payment._988Pay
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
        internal static string? SignConcatSourceString { get; set; }

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var apiUrl = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"appId", input["AppId"]},
                    {"mchOrderNo", input["TransID"]},
                    {"amount",reqAmount.ToString("F2")},
                    {"member", input["CustId"]},
                    {"notifyUrl", input["CallBackUrl"]},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()},
                    {"nonce", GetRandomString4(10)}
                };

                req.Add("sign", GetPostSign(req, input["Key"]));

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Url
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation("988Pay create order failed: " + ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var receiveModel = FromJson<Dictionary<string,string>>(bodyContent);
            if (!VerifySign(receiveModel, input["Key"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = receiveModel["mchOrderNo"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = receiveModel["orderNo"],
                DepositId = receiveModel["mchOrderNo"],
                Comment = $"status={receiveModel["status"]}",
                TransStatus = Definition.GetDepositTransStatus(receiveModel["status"]),
                ActualAmount = decimal.Parse(receiveModel["amount"]),
                Response = "success"
            };

            return result;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> input)
        {
            var apiUrl = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"appId", input["AppId"]},
                    {"member", input["CustId"]},
                    {"mchOrderNo", input["TransID"]},
                    {"amount",reqAmount.ToString("F2")},
                    {"notifyUrl", input["CallBackUrl"]},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()},
                    {"nonce", GetRandomString4(10)},
                    {"receiveAccount", input["AccountNo"]}
                };

                req.Add("sign", GetPostSign(req, input["Key"]));

                var res = Payout(apiUrl, req);

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = input["TransID"],
                    Message = res.Message
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {

            var receiveModel = FromJson<Dictionary<string, string>>(bodyContent);
            if (!VerifySign(receiveModel, payResource["Key"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = receiveModel["mchOrderNo"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyWithdrawResult
            {
                ThirdId = receiveModel["orderNo"],
                WithdrawId = receiveModel["mchOrderNo"],
                TransStatus = Definition.GetDepositTransStatus(receiveModel["status"]),
                Response = "success"
            };

            return result;
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        internal string GetPostSign(Dictionary<string, string> param, string key)
        {
            var sortedParameters = param.OrderBy(p => p.Key);
            var signSourceParams = string.Join("&", 
                sortedParameters
                    .Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) 
                                && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"{x.Key}={x.Value}"));
            SignConcatSourceString = $"{signSourceParams}&key={key}";
            _logger.LogInformation(SignConcatSourceString);

            var sign = ToMD5(SignConcatSourceString).ToUpper();
            _logger.LogInformation(sign);
            return sign;
        }

        private bool VerifySign(Dictionary<string, string> receiveModel, string key)
        {
            var receiveSign = receiveModel["sign"];
            var sign = GetPostSign(receiveModel, key);
            return receiveSign.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private CreateOrderData CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.Deposit;

            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, Response<CreateOrderData>>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"HttpStatus: {statusCode}", ((int)statusCode).ToString());

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"Code={result.Code}, Msg={result.Message}");

            return result.Data;
        }

        private Response<CreatePayoutData> Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.Withdrawal;
            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, Response<CreatePayoutData>>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"HttpStatus: {statusCode}", ((int)statusCode).ToString());

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"988Pay create payout failed: Code={result.Code}, Msg={result.Message}");

            return result;
        }
    }
}