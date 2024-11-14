using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.GoPay
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
            var apiUrl = input["Host"];
            var payCode = input["PayCode"];
            var (method, code) = GetMethod(payCode);
            var reqAmount = decimal.Parse(input["Amount"]);

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchantNo", input["MerchantNo"]},
                    {"outTradeNo", input["TransID"]},
                    {"type", method},
                    {"code", method.Equals("bank_transfer") ? input["BankCode"] : code},
                    {"amount", reqAmount.ToString("G29")},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
                };

                req.Add("sign", GetSign(req, input["Key"]));
                req.Add("notifyUrl", input["CallBackUrl"]);

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation(ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var req = FromJson<DepositCallBackRequest>(bodyContent);
            var str =
                $"{req.MerchantNo}|{req.ThirdId}|{req.TransId}|{req.Type}|{req.Code}|{req.Amount}|{req.Timestamp}|{input["Key"]}|";

            if (!VerifySign(req.Sign, str))
            {
                return new NotifyDepositResult
                {
                    DepositId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = req.ThirdId,
                DepositId = req.TransId,
                Comment = req.Status,
                TransStatus = req.Status.Equals("1") ? TransStatus.Success : TransStatus.Pending,
                ActualAmount = decimal.Parse(req.Amount),
                Response = Definition.CallbackResponse
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
                    {"merchantNo", input["MerchantNo"]},
                    {"outTradeNo", input["TransID"]},
                    {"bankCode", input["BankCode"]},
                    {"bankCardNumber", input["AccountNo"]},
                    {"amount", reqAmount.ToString("G29")},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
                };

                req.Add("sign", GetSign(req, input["Key"]));
                req.Add("notifyUrl", input["CallBackUrl"]);
                req.Add("username", input["AccountName"]);

                var res = Payout(apiUrl, req);
                
                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(res)
                };;
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
            var req = FromJson<PayoutCallBackRequest>(bodyContent);
            var str =
                $"{req.MerchantNo}|{req.ThirdId}|{req.TransId}|{req.BankCode}|{req.BankCardNumber}|{req.Amount}" +
                $"|{req.Timestamp}|{req.Status}|{payResource["Key"]}|";

            if (!VerifySign(req.Sign, str))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetTransStatus(req.Status);
            var result = new NotifyWithdrawResult
            {
                ThirdId = req.ThirdId,
                TransStatus = transStatus,
                Response = Definition.CallbackResponse,
                WithdrawId = req.TransId,
                Message = req.Status
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
                .PostJson<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"GoPay creat order failed: Code={result.Code}, Msg={result.Message}");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"GoPay creat payout failed: Code={result.Code}, Msg={result.Message}");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var str = string.Join("|", param
                .Select(x => $"{x.Value}")) + $"|{key}|";

            _logger.LogInformation("str: " + str);
            var sign = ToMD5(str).ToLower();

            return sign;
        }

        private bool VerifySign(string target, string str)
        {
            var sign = ToMD5(str);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private static (string method, string code) GetMethod(string payCode)
        {
            var result = payCode.Split('-');

            return (result[0], result.Length > 1 ? result[1] : string.Empty);
        }

        #endregion
    }
}