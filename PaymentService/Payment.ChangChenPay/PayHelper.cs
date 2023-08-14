using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.ChangChenPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private const decimal AmountFix = 100;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }


        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var apiUrl = input["ApiUrl"];
            var merchantId = input["MerchantId"];
            var key = input["Key"];
            var appId = input["AppId"];
            var payCode = input["PayCode"];
            var payCurrency = input["PayCurrencyId"];
            var reqAmount = decimal.Parse(input["Amount"]) * AmountFix;
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var notifyUrl = input["CallBackUrl"];
            var depositInfo = input["DepositInfo"];
            var accountName = input["AccountName"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    {"mchId", merchantId},
                    {"appId", appId},
                    {"productId", payCode},
                    {"mchOrderNo", transId},
                    {"currency", payCurrency},
                    {"amount", strAmount},
                    {"notifyUrl", notifyUrl},
                    {"subject", "subject"},
                    {"body", "body"},
                    {"param1", accountName}
                };
                
                req.Add("sign", GetSign(req, key));

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data.Url,
                    ThirdId = res.ThirdId,
                    DepositInfo = depositInfo
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
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var key = input["Key"];
            var transId = input["TransID"];
            
            if (!VerifySign(formData["sign"], formData, key))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = ChangChenPayDefinition.GetPayinTransStatus(formData["status"]);
            
            var result = new NotifyDepositResult
            {
                ThirdId = formData["payOrderId"],
                DepositId = formData["mchOrderNo"],
                Comment = formData["status"],
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(formData["amount"]),
                Response = ChangChenPayDefinition.CallbackResponse
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
            var apiUrl = inputs["ApiUrl"];
            var merchantId = inputs["MerchantId"];
            var key = inputs["Key"];
            var payKey = inputs["PayVerifyCode"];
            var appId = inputs["AppId"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var payCurrency = inputs["PayCurrencyId"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var bankName = inputs["BankName"];
            var payCode = inputs["PayCode"];

            try
            {
              
                var req = new Dictionary<string, string>
                {
                    {"mchId", merchantId},
                    {"appId", appId},
                    {"transVerifyCode", payKey},
                    {"mchTransOrderNo", transId},
                    {"currency", payCurrency},
                    {"amount", strAmount},
                    {"notifyUrl", notifyUrl},
                    {"bankCode", bankCode},
                    {"bankName", bankName},
                    {"accountType", payCode},
                    {"accountName", accountName},
                    {"accountNo", accountNo},
                    {"province", string.Empty},
                    {"city", string.Empty}
                };

                req.Add("sign", GetSign(req, key));

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
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var key = input["Key"];
            var transId = input["TransID"];
            

            if (!VerifySign(formData["sign"], formData, key))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = ChangChenPayDefinition.GetPayoutTransStatus(formData["status"]);
            var result = new NotifyWithdrawResult
            {
                ThirdId = formData["transOrderId"],
                TransStatus = transStatus,
                Response = ChangChenPayDefinition.CallbackResponse,
                WithdrawId = formData["mchTransOrderNo"],
                Message = formData["status"]
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
            var apiUrl = baseApiUrl + ChangChenPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<object, CreatOrderResponse>(apiUrl, new { @params = JsonConvert.SerializeObject(req) });

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"ChangChenPay creat order failed, HttpStatus: {(int) statusCode} {statusCode}");
            }

            if (result.Code != ChangChenPayDefinition.SuccessCode)
                throw new CustomException("ChangChenPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + ChangChenPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<object, CreatPayoutResponse>(apiUrl, new { @params = JsonConvert.SerializeObject(req) });

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"ChangChenPay create payout failed, HttpStatus: {(int) statusCode} {statusCode}");
            }

            if (result.Code != ChangChenPayDefinition.SuccessCode)
                throw new CustomException("ChangChenPay create payout failed.");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var str = string.Join("&", param
                .Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(x.Value))
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToUpper();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var sign = GetSign(param, key);

            return target.Equals(sign);
        }

        #endregion
    }
}