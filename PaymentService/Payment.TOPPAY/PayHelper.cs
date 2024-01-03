using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.TopPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        private readonly List<string> NoSignField = new List<string>
            { "bankCardNumber", "bankName", "payAmount", "sign" };

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["Host"];
            var merchantPrivateKey = inputs["MerchantPrivateKey"];
            var merchantNo = inputs["MerchantNo"];
            var payCode = inputs["PayCode"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var depositInfo = inputs["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchantCode", merchantNo },
                    { "orderType", "0" },
                    { "method", bankCode },
                    { "orderNum", transId },
                    { "payMoney", strAmount },
                    { "productDetail", transId },
                    { "notifyUrl", notifyUrl },
                    { "dateTime", DateTime.Now.ToString("yyyyMMddHHmmss") },
                    { "expiryPeriod", "120" },
                    { "name", accountName },
                    { "email", "" },
                    { "phone", "" },
                };
                
                req.Add("sign" , GetSign(req,merchantPrivateKey));

                var res = CreateOrder(apiUrl, req);

                depositInfo = depositInfo.AddOrSetJProperty("PayCode", payCode);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = "res.Data.Url",
                    ThirdId = "res.Data.ThirdId",
                    DepositInfo = "depositInfo"
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
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
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

        #region private

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string privateKey)
        {
            var signStr = string.Join("", param.OrderBy(kp => kp.Key)
                  .Select(x => $"{x.Value}"));
        
            return RsaSign(signStr, privateKey, HashAlgorithmName.SHA1);
        }

        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + TopPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJson<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"TopPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.PlatRespCode == "FAIL")
                throw new CustomException("TopPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, string token, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + TopPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"TopPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Success)
                throw new CustomException("TopPay create payout failed.");

            return result;
        }
        
        #endregion
    }
}