using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.JYMPay;

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
            var secretKey = input["SecretKey"];
            var merchantCode = input["MerchantCode"];
            var payCode = input["PayCode"];
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var accountName = input["AccountName"];
            var notifyUrl = input["CallBackUrl"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merchantCode", merchantCode},
                    {"merchantOrderId", transId},
                    {"amount", strAmount},
                    {"notifyUrl",notifyUrl},
                    {"channelTypeId",payCode},
                    {"payerName",accountName},
                    {"requestTime",unixTimestamp}
                };
                
                req.Add("sign", GetSign(req, secretKey));

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = res.Code,
                    Url = res.Data.UpstreamLink,
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"JYMPay Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var response = new Dictionary<string, string>
            {
                {"error_code",JYMPayDefinition.CallbackSuccessResponse},
                {"message","Success"}
            };
            
            
            var transId = input["TransID"];
            var secretKey = input["SecretKey"];
            
            var req = FromJson<CallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                { "payment_id", req.PaymentId },
                { "payment_cl_id", req.PaymentClId },
                { "platform_id", req.PlatformId },
                { "amount", req.Amount.ToString() },
                { "real_amount", req.RealAmount.ToString() },
                { "fee", req.Fee.ToString() },
                { "status", req.Status.ToString() },
                { "create_time", req.CreateTime },
                { "update_time", req.UpdateTime },
            };
            
            if (!VerifySign(req.Sign, dic,secretKey))
            {
                response["error_code"] = JYMPayDefinition.CallbackFailResponse;
                response["message"] = "Signature verification failed.";
                
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = JsonConvert.SerializeObject(response)
                };
            }

            var getStatus = JYMPayDefinition.GetTransStatus(req.Status.ToString(),1);


            switch (getStatus)
            {
                case TransStatus.Success:
                    var result = new NotifyDepositResult
                    {
                        ThirdId = req.PaymentId,
                        DepositId = req.PaymentClId,
                        TransStatus = TransStatus.Success,
                        ActualAmount = req.Amount,
                        Response = JsonConvert.SerializeObject(response)
                    };

                    return result;
                case TransStatus.Fail:
                    response["error_code"] = JYMPayDefinition.CallbackFailResponse;
                    response["message"] = "Error";
                    var resultFail = new NotifyDepositResult
                    {
                        ThirdId = req.PaymentId,
                        DepositId = req.PaymentClId,
                        TransStatus = TransStatus.Fail,
                        ActualAmount = req.Amount,
                        Response = JsonConvert.SerializeObject(response)
                    };

                    return resultFail;
                case TransStatus.Pending:
                    response["error_code"] = JYMPayDefinition.CallbackPendingResponse;
                    response["message"] = "Pending";
                    var resultPending = new NotifyDepositResult
                    {
                        ThirdId = req.PaymentId,
                        DepositId = req.PaymentClId,
                        TransStatus = TransStatus.Pending,
                        ActualAmount = req.Amount,
                        Response = JsonConvert.SerializeObject(response)
                    };
                    return resultPending;
                default:
                    response["error_code"] = JYMPayDefinition.CallbackPendingResponse;
                    response["message"] = "Pending";
                    var resultDefault = new NotifyDepositResult
                    {
                        ThirdId = req.PaymentId,
                        DepositId = req.PaymentClId,
                        TransStatus = TransStatus.Pending,
                        ActualAmount = req.Amount,
                        Response = JsonConvert.SerializeObject(response)
                    };
                    return resultDefault;
            }
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> input)
        {
            var response = new WithdrawResult
            {
                Success = false,
                RawContent = "System Error"
            };
            
            var apiUrl = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var accountName = input["AccountName"];
            var accountNo = input["AccountNo"];
            var notifyUrl = input["CallBackUrl"];
            var bankCode = input["BankCode"];
            var secretKey = input["SecretKey"];
            var merchantCode = input["MerchantCode"];
            var payCode = input["PayCode"];
            var ifscCode = input["IFSCCode"];
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            try
            {
                
                var req = new Dictionary<string, string>
                {
                    {"merchantCode", merchantCode},
                    {"merchantOrderId", transId},
                    {"amount", strAmount},
                    {"notifyUrl",notifyUrl},
                    {"channelTypeId",payCode},
                    {"payeeName",accountName},
                    {"payeeAccount",accountNo},
                    {"payeeBankId",bankCode},
                    {"remark",ifscCode}, //收款银行IFSC
                    {"requestTime",unixTimestamp}
                };

                req.Add("sign", GetSign(req, secretKey));

                var res = Payout(apiUrl, req);
                
                if(res.Code == "0000")
                { 
                    response = new WithdrawResult
                    {
                        Success = true,
                        RawContent = JsonConvert.SerializeObject(res)
                    };
                }
                else
                {
                    response = new WithdrawResult
                    {
                        Success = false,
                        RawContent = JsonConvert.SerializeObject(res)
                    };
                }

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"JYMPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

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
            
            var response = new Dictionary<string, string>
            {
                {"error_code",JYMPayDefinition.CallbackSuccessResponse},
                {"message","Success"}
            };
            
            var result = new NotifyWithdrawResult();
            var secretKey = payResource["SecretKey"];
            var req = FromJson<CallBackRequest>(bodyContent);
            
            
            
            var dic = new Dictionary<string, string>
            {
                { "payout_id", req.PayoutId },
                { "payout_cl_id", req.PayoutClId },
                { "platform_id", req.PlatformId },
                { "amount", req.Amount.ToString() },
                { "fee", req.Fee.ToString() },
                { "status", req.Status.ToString() },
                { "create_time", req.CreateTime },
                { "update_time", req.UpdateTime },
            };

            if (!VerifySign(req.Sign, dic, secretKey))
            {
                response["error_code"] = JYMPayDefinition.CallbackPendingResponse;
                response["message"] = "Signature verification failed.";
                
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.PaymentId,
                    TransStatus = TransStatus.Pending,
                    Response = JsonConvert.SerializeObject(response)
                };
            }

            
            var getStatus = JYMPayDefinition.GetTransStatus(req.Status.ToString(),2);
            
            switch (getStatus)
            {
                case TransStatus.Success:
                    result = new NotifyWithdrawResult
                    {
                        ThirdId = req.PayoutId,
                        TransStatus = TransStatus.Success,
                        Response = JsonConvert.SerializeObject(response),
                        WithdrawId = req.PayoutClId,
                    };
                    return result;
                case TransStatus.Fail:
                    response["error_code"] = JYMPayDefinition.CallbackFailResponse;
                    response["message"] = "Error";
                    var resultFail = new NotifyWithdrawResult
                    {
                        ThirdId = req.PayoutId,
                        TransStatus = TransStatus.Fail,
                        Response = JsonConvert.SerializeObject(response),
                        WithdrawId = req.PayoutClId,
                    };
                    return resultFail;
                case TransStatus.Pending:
                    response["error_code"] = JYMPayDefinition.CallbackPendingResponse;
                    response["message"] = "Pending";
                    var resultPending = new NotifyWithdrawResult()
                    {
                        ThirdId = req.PayoutId,
                        TransStatus = TransStatus.Pending,
                        Response = JsonConvert.SerializeObject(response),
                        WithdrawId = req.PayoutClId,
                    };
                    return resultPending;
                default:
                    response["error_code"] = JYMPayDefinition.CallbackPendingResponse;
                    response["message"] = "Pending";
                    var resultDefault = new NotifyWithdrawResult()
                    {
                        ThirdId = req.PayoutId,
                        TransStatus = TransStatus.Pending,
                        Response = JsonConvert.SerializeObject(response),
                        WithdrawId = req.PayoutClId,
                    };    
                    return resultDefault;
            }
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

        private CreateOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + JYMPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreateOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"JYMPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Code != "0000")
                throw new CustomException("JYMPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + JYMPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"JYMPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.Code != "0000")
                throw new CustomException("JYMPay create payout failed.");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string secretKey)
        {
            var sortedParams = param.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}");
            var str = string.Join("&", sortedParams) + $"&{secretKey}";

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToLower();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string secretKey)
        {
            var sign = GetSign(param, secretKey);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
}