using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using Newtonsoft.Json;
using static Payment.SojuPay.SojuPayDefinition;
using static Payment.SojuPay.SojuPayModel;


namespace Payment.SojuPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private const decimal AmountFixed = 100;
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region Debit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var host = inputs["host"];
            var apiURL = inputs["DepositURL"];
            var platfromId = inputs["MerchantID"];
            var serviceId = inputs["ServiceId"];
            var transId = inputs["TransID"];
            var apikey = inputs["APIKey"];
            var reqAmount = decimal.Parse(inputs["Amount"]) * AmountFixed;
            var strAmount = Math.Floor(reqAmount).ToString("#");
            try
            {
                var req = new Dictionary<string, string>()
                {
                    {"platform_id", platfromId},
                    {"service_id", serviceId},
                    {"payment_cl_id", transId},
                    {"name", inputs["AccountName"]},
                    {"amount", strAmount},
                    {"notify_url", inputs["CallBackUrl"]},
                    {"request_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                };

                req.Add("sign", GetSign(req, apikey));

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, DepositResponse>($"{host}{SojuPayDefinition.Path.Deposit}", req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"SojuPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.ErrorCode))
                {
                    throw new CustomException(response.ErrorMessage, response.ErrorCode);
                }

                return new DepositResult()
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = response.Data.Link
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
            var apiKey = payResource["APIKey"];
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);
            var transId = dicReq["payment_cl_id"];
            var status = dicReq["status"];
            var actualAmount = decimal.Parse(dicReq["real_amount"]) / AmountFixed;
            var sign = new Dictionary<string, string>()
            {
                {"payment_id",dicReq["payment_id"] },
                {"payment_cl_id",transId },
                {"platform_id",dicReq["platform_id"] },
                {"amount",dicReq["amount"] },
                {"real_amount",dicReq["real_amount"] },
                {"fee",dicReq["fee"] },
                {"status",status },
                {"create_time",dicReq["create_time"] },
                {"update_time",dicReq["update_time"] },
            };

            if (!VerifySign(sign,apiKey ,dicReq["sign"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = SojuPayDefinition.GetTransStatus(status);

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = actualAmount,
                ThirdId = transId,
                Comment = status,
                TransStatus = transStatus,
                Response = ToJson(new { error_code = "0000" })
            };
        }
        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var host = inputs["host"];
            var apikey = inputs["APIKey"];
            var platfromId = inputs["MerchantID"];
            var transId = inputs["TransID"];
            var serviceId = inputs["ServiceId"];
            var reqAmount = decimal.Parse(inputs["Amount"]) * AmountFixed;
            var strAmount = Math.Floor(reqAmount).ToString("#");
            try
            {
                var req = new Dictionary<string, string>()
                {
                    { "amount" , strAmount},
                    { "bank_name", inputs["BankName"] },
                    { "name", inputs["AccountName"] },
                    { "notify_url", inputs["CallBackUrl"] },
                    { "number", inputs["AccountNo"] },
                    { "payout_cl_id", transId},
                    { "platform_id", platfromId},
                    { "request_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()},
                    { "service_id", serviceId}
                };

                req.Add("sign", GetSign(req, apikey));

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, WithdrawalResponse>($"{host}{SojuPayDefinition.Path.Withdrawal}", req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"SojuPay Withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.ErrorCode))
                {
                    throw new CustomException(response.ErrorMessage, response.ErrorCode);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(response)
                };

            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var apikey = payResource["APIKey"];
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);
            var thirdId = dicReq["payout_id"];
            var transId = dicReq["payout_cl_id"];
            var status = dicReq["status"];
            var reqforsign = new Dictionary<string, string>()
            {
                { "payout_id", thirdId},
                { "payout_cl_id", transId },
                { "platform_id", dicReq["platform_id"]},
                { "amount", dicReq["amount"]},
                { "fee", dicReq["fee"]},
                { "status", status},
                { "create_time", dicReq["create_time"] },
                { "update_time", dicReq["update)time"]}
            };

            if (!VerifySign(dicReq, apikey, dicReq["sign"]))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = SojuPayDefinition.GetTransStatus(status);

            return new NotifyWithdrawResult
            {
                ThirdId = thirdId,
                TransStatus = transStatus,
                Response = ToJson(new { error_code = "0000" }),
                WithdrawId = transId
            };
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

        #region privite
        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            param = param.OrderBy(x => x.Key, StringComparer.Ordinal);
            var str = string.Join("&", param
                .Select(x => $"{x.Key}={x.Value}"));
            _logger.LogInformation($"string:{str}&{apiKey}");
            var sign = ToMD5($"{str}&{apiKey}").ToLower();
            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }
        #endregion
    }
}