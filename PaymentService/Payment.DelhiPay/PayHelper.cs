using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DelhiPay
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


        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var apiUrl = input["host"];
            var apiKey = input["key"];
            var reqAmount = decimal.Parse(input["Amount"]) * 100;
            var strAmount = Math.Floor(reqAmount).ToString("#");
            var transId = input["TransID"];
            var merchantCode = input["merchantCode"];
            var notifyUrl = input["CallBackUrl"];
            var payCode = input["PayCode"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchantCode", merchantCode},
                    {"method", payCode},
                    {"signType", "MD5"},
                    {"timestamp", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")},
                    {"merchantOrderNo", transId},
                    {"orderAmount", strAmount},
                    {"accountName", string.Empty},
                    {"accountBank", string.Empty},
                    {"notifyUrl", notifyUrl}
                };

                req.Add("sign", GetSign(req, apiKey));

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.DepositUrl,
                    ThirdId = res.PlatformOrderNo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> input)
        {
            var apiKey = input["key"];
            var req = FromJson<CallBackRequest>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (GetSign(dicReq, apiKey) != req.Sign)
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }
            var transStatus = DelhiPayDefinition.GetTransStatus(req.OrderStatus);
            var result = new NotifyDepositResult
            {
                ThirdId = req.PlatformOrderNo,
                DepositId = req.TransId,
                Comment = req.OrderStatus,
                TransStatus = transStatus,
                ActualAmount = req.ActualAmount,
                Message = req.Message,
                Response = CallbackResponse.Success
            };

            return result;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["host"];
            var apiKey = inputs["key"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("#");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var ifscCode = inputs["IFSCCode"];
            var merchantCode = inputs["merchantCode"];
            var notifyUrl = inputs["CallBackUrl"];
            var payCode = inputs["PayCode"];

            try
            {
              
                var req = new Dictionary<string, string>
                {
                    {"merchantCode", merchantCode},
                    {"method", payCode},
                    {"signType", "MD5"},
                    {"timestamp", DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")},
                    {"merchantOrderNo", transId},
                    {"orderAmount", strAmount},
                    {"accountName", accountName},
                    {"accountNo", accountNo},
                    {"accountBank", string.Empty},
                    {"accountBranch", ifscCode},
                    {"notifyUrl", notifyUrl}
                };

                req.Add("sign", GetSign(req, apiKey));

                var res = Payout(apiUrl, req);
                var response = new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(res)
                };

                return response;
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> payResource)
        {
            var apiKey = payResource["key"];
            var req = FromJson<CallBackRequest>(bodyContent);
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

            if (GetSign(dicReq, apiKey) != req.Sign)
            {
                return new NotifyWithdrawResult
                {
                    TransStatus = TransStatus.Pending,
                    Response = CallbackResponse.Failed
                };
            }

            var transStatus = DelhiPayDefinition.GetTransStatus(req.OrderStatus);
            var result = new NotifyWithdrawResult
            {
                ThirdId = req.PlatformOrderNo,
                TransStatus = transStatus,
                Response = CallbackResponse.Success,
                WithdrawId = req.TransId,
                Message = req.Message
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

        #region Private Methods

        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DelhiPay CreatOrder failed, HttpStatus: {(int) statusCode} {statusCode}" +
                    $",errorMsg: {result.Message}");
            }

            if (result.Code != DelhiPayErrorCode.Success)
                throw new CustomException($"DelhiPay CreatOrder response error message: {result.Message}");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DelhiPay Payout failed, HttpStatus: {(int) statusCode} {statusCode}" +
                    $", error code: {result.Code} {result.Message}");
            }

            if (result.Code != DelhiPayErrorCode.Success)
                throw new CustomException(
                    $"DelhiPay Payout response error: {result.Code} {result.Message}");

            return result;
        }


        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var str = string.Join(string.Empty, param
                .Where(x => x.Key != "sign" )
                .OrderBy(x => x.Key)
                .Select(x => x.Value)) + key;

            _logger.LogInformation(str);
            var md5Str = base.ToMD5(str).ToUpper();
            _logger.LogInformation(md5Str);
            var sign = Convert.ToHexString(Encoding.UTF8.GetBytes(md5Str))
                .Replace("-", string.Empty);
            _logger.LogInformation(sign);
            return sign;
        }

        #endregion
    }
}