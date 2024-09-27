using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.EBPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private readonly List<string> _callbackSignKeys;
        private readonly List<string> _payoutCallbackSignKeys;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();

            #region SignKeys

            _callbackSignKeys= new List<string>
            {
                "orderNo",
                "merchantOrderId",
                "merchantNo",
                "payTypeId",
                "orderStatus",
                "orderAmount",
                "paidAmount"
            };

            #endregion
            
            
        }


        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var apiUrl = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchantNo", input["MerchantNo"]},
                    {"merchantOrderId", input["TransID"]},
                    {"userName", input["CustId"]},
                    {"deviceType", input["DeviceType"].Equals("1") ? "1" : "9"} // 1: PC, 9: 未知（他有分作業系統問群說H5傳這個）
                };

                req.Add("sign", GetSign(req, input["Key"]));
                req.Add("loginIp", input["ClientIP"]);
                req.Add("depositNotifyUrl", input["CallBackUrl"]);
                req.Add("payAmount", reqAmount.ToString("F2"));
                req.Add("depositName", input["AccountName"]);
                req.Add("payTypeId", input["PayCode"]);

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
                _logger.LogInformation("EBPay creat order failed: " + ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var str = string.Join('&', _callbackSignKeys.Select(x => $"{x}={formData[x]}"))
                + $"&key={input["Key"]}";
            
            if (!VerifySign(formData["sign"], str))
            {
                return new NotifyDepositResult
                {
                    DepositId = formData["merchantOrderId"],
                    TransStatus = TransStatus.Pending,
                    Response = ToJson(new CallbackResponse(400, "signature verification failed."))
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = formData["orderNo"],
                DepositId = formData["merchantOrderId"],
                Comment = $"orderStatus={formData["orderStatus"]}, completeTime={formData["completeTime"]}",
                TransStatus = Definition.GetTransStatus(formData["orderStatus"]),
                ActualAmount = decimal.Parse(formData["paidAmount"]),
                Response = ToJson(new CallbackResponse(200, "signature verification failed."))
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
                    {"merchantOrderId", input["TransID"]},
                    {"userName", input["CustId"]},
                    {"orderType", input["PayCode"]},
                    {"payAmount", reqAmount.ToString("F2")},
                    {"bankNum", input["AccountNo"]},
                    {"timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}
                };

                req.Add("sign", GetSign(req, input["Key"]));
                req.Add("deviceType", "0");
                req.Add("userIp", input["ClientIP"]);
                req.Add("notifyUrl", input["CallBackUrl"]);
                req.Add("bankCode", input["BankCode"]);
                req.Add("bankOwner", input["AccountName"]);
                req.Add("bankAddress", "bankAddress");

                var res = Payout(apiUrl, req);
                
                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = input["TransID"],
                    Message = res.ThirdId
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
            
            var str = string.Join('&', _callbackSignKeys.Select(x => $"{x}={formData[x]}"))
                      + $"&key={payResource["Key"]}";
            
            if (!VerifySign(formData["sign"], str))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = formData["merchantOrderId"],
                    TransStatus = TransStatus.Pending,
                    Response = ToJson(new CallbackResponse(400, "signature verification failed."))
                };
            }

            var result = new NotifyWithdrawResult
            {
                ThirdId = formData["orderNo"],
                TransStatus = Definition.GetTransStatus(formData["orderStatus"]),
                Response = ToJson(new CallbackResponse(200, "signature verification failed.")),
                WithdrawId = formData["merchantOrderId"],
                Message = $"orderStatus={formData["orderStatus"]}, completeTime={formData["completeTime"]}"
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

        private CreateOrderData CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, EBPayResp<CreateOrderData>>(apiUrl, req);

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"Code={result.Code}, Msg={result.Message}");

            return result.Data;
        }

        private CreatePayoutData Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, EBPayResp<CreatePayoutData>>(apiUrl, req);

            if (result.Code != Definition.SuccessCode)
                throw new CustomException($"EBPay create payout failed: Code={result.Code}, Msg={result.Message}");

            return result.Data;
        }

        public string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var str = string.Join('&', param
                    .Select(x => $"{x.Key}={x.Value}")) + $"&key={key}";

            _logger.LogInformation("str: " + str);
            var sign = ToMD5(str).ToLower();

            return sign;
        }

        private bool VerifySign(string target, string str)
        {
            var sign = ToMD5(str);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}