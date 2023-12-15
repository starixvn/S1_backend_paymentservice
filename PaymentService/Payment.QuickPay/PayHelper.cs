using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.QuickPay
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
            var merchantNo = input["MerchantNo"];
            var appSecret = input["appSecret"];
            var apiKey = input["apiKey"];
            var userNo = input["CustId"];
            var accountName = input["AccountName"];
            var bankName = input["BankName"];
            var payCode = input["PayCode"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var notifyUrl = input["CallBackUrl"];
            var depositInfo = input["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchantNo",merchantNo},
                    {"orderNo",transId},
                    {"userNo",userNo},
                    {"amount", strAmount + ".00"},
                    {"discount","0"},
                    {"extra",string.Empty},
                    {"datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                    {"notifyUrl",notifyUrl},
                    {"time", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString()},
                };
                
                req.Add("sign", GetSign(req, apiKey));
                req.Add("appSecret",appSecret);
                req.Add("payeeName",accountName);
                req.Add("channelNo",ConvertChannel(payCode));
                
                

                if (payCode.Equals("OnlineBanking"))
                {
                    req.Add("bankName", bankName);
                }

                var res = CreateOrder(apiUrl, req);

                depositInfo = depositInfo.AddOrSetJProperty("PayCode", payCode);

                var response = new DepositResult();

                if (res.Code == 0)
                {
                    response = new DepositResult
                    {
                        Success = true,
                        ErrorCode = "0",
                        Url = res.TargetUrl,
                        DepositInfo = depositInfo
                    };
                }
                else
                {
                    response = new DepositResult
                    {
                        Success = false,
                        ErrorCode = "1",
                        Message = res.Text
                    };
                }
                
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
            var apiKey = input["apiKey"];
            
            var req = FromJson<DepositCallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                {"amount",req.Amount.ToString()},
                {"discount",req.Discount.ToString()},
                {"extra",req.Extra},
                {"lucky",req.Lucky.ToString()},
                {"orderNo",req.OrderNo},
                {"paid",req.Paid.ToString()},
                {"status",req.Status},
                {"tradeNo",req.TradeNo},
                {"userNo",req.UserNo.ToString()}
            };

            if (!VerifySign(req.Sign, dic, apiKey))
            {
                return new NotifyDepositResult
                {
                    DepositId = req.OrderNo,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = req.TradeNo,
                DepositId = req.OrderNo,
                Comment = req.Extra,
                TransStatus = TransStatus.Success, // QuickPay只回調成功的單
                ActualAmount = req.Amount,
                Response = QuickPayDefinition.CallbackResponse
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
            var merchantNo = input["MerchantNo"];
            var appSecret = input["appSecret"];
            var apiKey = input["apiKey"];
            var accountName = input["AccountName"];
            var bankName = input["BankName"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var accountNo = input["AccountNo"];
            var notifyUrl = input["CallBackUrl"];
            
            
            try
            {

                var req = new Dictionary<string, string>
                {
                    {"merchantNo",merchantNo},
                    {"orderNo",transId},
                    {"amount", strAmount + ".00"},
                    {"name",accountName},
                    {"bankName",bankName},
                    {"bankAccount",accountNo},
                    {"mobile",string.Empty},
                    {"datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                    {"notifyUrl",notifyUrl},
                    {"extra",string.Empty},
                    {"time", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString()},
                };

                req.Add("sign", GetSign(req, apiKey));
                req.Add("appSecret",appSecret);

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
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var apiKey = payResource["apiKey"];
            
            var req = FromJson<WithdrawalCallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                {"amount",req.Amount.ToString()},
                {"bankAccount",req.BankAccount},
                {"bankName",req.BankName},
                {"extra",req.Extra},
                {"fee",req.Fee.ToString()},
                {"mobile",req.Mobile},
                {"name",req.Name},
                {"orderNo",req.OrderNo},
                {"payAccount",req.PayAccount},
                {"status",req.Status},
                {"tradeNo",req.TradeNo}
            };

            if (!VerifySign(req.Sign, dic, apiKey))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.OrderNo,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = QuickPayDefinition.GetTransStatus(req.Status);
            var result = new NotifyWithdrawResult
            {
                ThirdId = req.TradeNo,
                TransStatus = transStatus,
                Response = QuickPayDefinition.CallbackResponse,
                WithdrawId = req.OrderNo,
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
            var apiUrl = baseApiUrl + QuickPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJson<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"QuickPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Code == 1)
                throw new CustomException("QuickPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + QuickPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"QuickPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if(result.Code == 1)
                throw new CustomException("QuickPay create payout failed.");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var str = string.Join("&", param.OrderBy(kp => kp.Key).
                Select(kp => $"{kp.Key}={kp.Value}")) + apiKey;

            _logger.LogInformation(str);
            var sha256 = ToSHA256(str).ToLower();
            var sign = ToMD5(sha256).ToUpper();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var sign = GetSign(param, apiKey);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private static (string method, string code) GetMethod(string payCode)
        {
            var result = payCode.Split('-');

            return (result[0], result.Length > 1 ? result[1] : string.Empty);
        }

        private string ConvertChannel(string payCode)
        {
            return payCode switch
            {
                "OnlineBanking" => "4",
                "MobilePayment" => "2",
                _ => ""
            };
        }

        #endregion
    }
}