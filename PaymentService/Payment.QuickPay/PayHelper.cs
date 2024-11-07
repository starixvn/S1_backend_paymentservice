using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.QuickPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        private readonly IEnumerable<string> _depositSignExceptKeys;
        private readonly IEnumerable<string> _payoutSignExceptKeys;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();

            #region sign except keys

            _depositSignExceptKeys = new[]
            {
                
                "userName",
                "amountBeforeFixed",
                "payeeName",
                "appSecret",
                "bankName",
                "channelNo",
                "sign"
            };
            _payoutSignExceptKeys = new[]
            {
                
                "bankBranch",
                "memo",
                "VerifyChannelNo",
                "appSecret",
                "sign"
            };

            #endregion
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
            var payCode = input["PayCode"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var notifyUrl = input["CallBackUrl"];
            var depositInfo = input["DepositInfo"];
            var (channelNo, bankCode) = SplitPayCode(payCode);
            var depositType = input["DepositType"];

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
                    {"appSecret", appSecret},
                    {"payeeName", accountName},
                    {"channelNo", channelNo}
                };
                
                if (depositType.Equals(((int)DepositType.OnlineBanking).ToString()))
                {
                    req.Add("bankName", input["BankName"]);
                }

                if (channelNo.Equals("2"))
                {
                    req.Add("bankName",bankCode);
                    req.Add("userName", input["AccountName"]);
                }
                
                req.Add("sign", GetSign(req, apiKey, _depositSignExceptKeys));

                var res = CreateOrder(apiUrl, req);

                depositInfo = depositInfo.AddOrSetJProperty("PayCode", payCode);


                if (res.Code == 0)
                {
                    return new DepositResult
                    {
                        Success = true,
                        ErrorCode = "0",
                        Url = res.TargetUrl,
                        DepositInfo = depositInfo
                    };
                }
                
                return new DepositResult
                {
                    Success = false,
                    ErrorCode = "1",
                    Message = res.Text
                };
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
            var dic = FromJson<Dictionary<string, string>>(bodyContent);

            if (!VerifySign(req.Sign, dic, apiKey, _depositSignExceptKeys))
            {
                return new NotifyDepositResult
                {
                    DepositId = req.OrderNo,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = QuickPayDefinition.GetTransStatus(req.Status);
            var result = new NotifyDepositResult
            {
                ThirdId = req.TradeNo,
                DepositId = req.OrderNo,
                Comment = req.Extra,
                TransStatus = transStatus, // QuickPay只回調成功的單
                ActualAmount = req.Paid,
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
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var bankCode = input["BankCode"];
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
                    {"bankName",bankCode},
                    {"bankAccount",accountNo},
                    {"mobile",string.Empty},
                    {"datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                    {"notifyUrl",notifyUrl},
                    {"reverseUrl",string.Empty},
                    {"extra",string.Empty},
                    {"time", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString()},
                    {"appSecret",appSecret}
                };

                req.Add("sign", GetSign(req, apiKey, _payoutSignExceptKeys));

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
            var dic = FromJson<Dictionary<string, string>>(bodyContent);
            
            if (!VerifySign(req.Sign, dic, apiKey, _payoutSignExceptKeys))
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
                .PostJson<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"QuickPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if(result.Code == 1)
                throw new CustomException("QuickPay create payout failed.");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, IEnumerable<string> exceptKeys)
        {
            var str = string.Join("&", param
                .Where(x => !exceptKeys.Contains(x.Key))
                .OrderBy(kp => kp.Key)
                .Select(kp => $"{kp.Key}={kp.Value}")) + apiKey;

            _logger.LogInformation(str);
            var sha256 = ToSHA256(str).ToLower();
            var sign = ToMD5(sha256).ToUpper();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string apiKey, IEnumerable<string> exceptKeys)
        {
            var sign = GetSign(param, apiKey, exceptKeys);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }
        
        private static (string channelNo, string bankCode) SplitPayCode(string payCode)
        {
            var result = payCode.Split('_');

            return (result[0], result.Length > 1 ? result[1] : string.Empty);
        }

        #endregion
    }
}