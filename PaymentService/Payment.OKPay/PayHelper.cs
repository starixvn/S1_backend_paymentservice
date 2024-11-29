using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.OKPay
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
            var apiHost = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var (product, pattern) = SplitPayCode(input["PayCode"]);
            try
            {
                
                var req = new Dictionary<string, string>
                {
                    {"appId", input["AppId"]},
                    {"merchantOrderNo", input["TransID"]},
                    {"amount", reqAmount.ToString("G29")},
                    {"paymentProduct", product},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()},
                    {"merchantCode", input["MerchantCode"]},
                    {"payPattern", pattern},
                    {"callbackAddress", input["CallBackUrl"]}
                };
                
                req.Add("sign", GetSign(req, input["Key"]));

                var res = CreateOrder(apiHost, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.PayUrl,
                    ThirdId = res.ThirdId
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"OKPay create deposit failed, msg={ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var transId = input["TransID"];
            var dic = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dic["sign"], dic, input["Key"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{transId} signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = dic["orderNo"],
                DepositId = transId,
                Comment = $"orderStatus={dic["orderStatus"]}",
                TransStatus = Definition.GetTransStatus(dic["orderStatus"]),
                ActualAmount = decimal.Parse(dic["amountActual"]),
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
            var apiHost = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "appId", input["AppId"] },
                    { "merchantOrderNo", input["TransID"] },
                    { "amount", reqAmount.ToString("G29") },
                    { "paymentBank", input["BankCode"] },
                    { "paymentProduct", input["PayCode"] },
                    { "payeeName", input["AccountName"] },
                    { "payeeAccount", input["AccountNo"] },
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                    { "merchantCode", input["MerchantCode"] },
                    { "callbackAddress", input["CallBackUrl"] },
                    { "timeout", "" }
                };
                    
                req.Add("sign", GetSign(req, input["Key"]));
                
                var res = Payout(apiHost, req);
                
                var response = new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(res)
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"OKPay payout failed, msg={ex.Message}");

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
            var transId = input["TransId"];
            var dic = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(dic["sign"], dic, input["Key"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{transId} signature verification failed."
                };
            }

            var result = new NotifyWithdrawResult
            {
                ThirdId = transId,
                TransStatus = Definition.GetTransStatus(dic["orderStatus"]),
                Response = Definition.CallbackResponse,
                WithdrawId = transId,
                Message = $"orderStatus={dic["orderStatus"]}"
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

        private CreatOrderData CreateOrder(string apiHost, Dictionary<string, string> req)
        {
            var apiUrl = apiHost + Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, OKPayResp<CreatOrderData>>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"HttpStatus=({(int)statusCode}){statusCode}");

            if (!Definition.SuccessCode.Equals(result.Code))
                throw new CustomException(result.Message);

            return result.Data;
        }

        private CreatPayoutData Payout(string apiHost, Dictionary<string, string> req)
        {
            var apiUrl = apiHost + Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .PostJson<Dictionary<string, string>, OKPayResp<CreatPayoutData>>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"HttpStatus=({(int)statusCode}){statusCode}");

            if (!Definition.SuccessCode.Equals(result.Code))
                throw new CustomException(result.Message);

            return result.Data;
        }

        private string GetSign(IDictionary<string, string> param, string apiKey)
        {
            var str = string.Join('&', param
                .Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)
                    && x.Value != string.Empty) // null要參與簽名
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}")) + $"&sKey={apiKey}";

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToLower();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IDictionary<string, string> param, string apiKey)
            => target.Equals(GetSign(param, apiKey), StringComparison.OrdinalIgnoreCase);
        
        private static (string product, string pattern) SplitPayCode(string payCode)
        {
            var result = payCode.Split('_');

            return (result[0], result.Length > 1 ? result[1] : string.Empty);
        }
        
        #endregion
    }
}