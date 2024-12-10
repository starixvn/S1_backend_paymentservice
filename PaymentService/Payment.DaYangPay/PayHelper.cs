using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DaYangPay
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
        
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            try
            {
                
                var apiUrl = inputs["Host"];
                var merchantKey = inputs["MerchantKey"];
                var secretKey = inputs["SecretKey"];
                var payCode = inputs["PayCode"];
                var reqAmount = decimal.Parse(inputs["Amount"]);
                var strAmount = reqAmount.ToString("G29");
                var transId = inputs["TransID"];
                var notifyUrl = inputs["CallBackUrl"];
                var bankCode = inputs["BankCode"];
                
                
                var req = new Dictionary<string, string>
                {
                    { "client_key", merchantKey },
                    { "amount", strAmount },
                    { "channel_id", payCode },
                    { "out_trade_no", transId },
                    { "notify_url", notifyUrl },
                };


                if (payCode == "1001")
                {
                    var extraJson = new
                    {
                        bank_code = bankCode
                    };
                    var extraRequest = JsonConvert.SerializeObject(extraJson);
                    req.Add("extra", extraRequest);
                }

                if (payCode.Contains("-2"))
                {
                    var splitBankCode = "";
                    var bankCodeParts = payCode.Split('-');
                    if (bankCodeParts.Length > 1)
                    {
                        splitBankCode = bankCodeParts[1];
                    }
                    var extraJson = new
                    {
                        bank_code = splitBankCode,
                        type = "2"
                    };
                    
                    var extraRequest = JsonConvert.SerializeObject(extraJson);
                    req["channel_id"] = bankCodeParts[0];
                    req.Add("extra", extraRequest);
                }
                
                req.Add("signature", Hash(req, secretKey));
                
                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "Success",
                    Url = res.PaymentUrl,
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = payResource["TransID"]; 
            var secretKey = payResource["SecretKey"];
            
            var req = FromJson<CallBackDebitRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                { "client_key", req .ClientKey },
                { "amount", req.Amount.ToString() },
                { "channel_id", req.Channelid },
                { "trade_no", req.TradeNo },
                { "out_trade_no", req.OutTradeNo },
                { "created_at", req.CreatedAt },
                { "paid_at", req.PaidAt },
                { "status" , req.Status.ToString()}
            };

            

            if (!VerifySign(req.Signature, dic,secretKey))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var resultJson = new
            {
                code = DaYangDefinition.CallbackResponse
            };

            var resultJsonString = JsonConvert.SerializeObject(resultJson);
            
            //DaYangPay 只回傳 成功訂單 不是1(已支付) 直接Pending
            //Note : 代收订单状态为 已支付 ，才会通知商户，其他订单状态不会通知商户。
            //如果首次通知商户出现异常，会尝试再通知4次，共5次。频率为：0s/10s/10s/30s/30s。时间可能有1~2秒的差异，以实际为准。
            var result = new NotifyDepositResult
            {
                ThirdId = req.TradeNo,
                DepositId = req.OutTradeNo,
                TransStatus = req.Status == 1 ? TransStatus.Success : TransStatus.Pending, // DaYangPay只回調成功的單
                ActualAmount = req.Amount,
                Response = resultJsonString
            };

            return result;
        }
        
        #endregion
        
        #region Credit
        
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var apiUrl = inputs["Host"];
                var merchantKey = inputs["MerchantKey"];
                var secretKey = inputs["SecretKey"];
                var payCode = inputs["PayCode"];
                var reqAmount = decimal.Parse(inputs["Amount"]);
                var strAmount = reqAmount.ToString("G29");
                var transId = inputs["TransID"];
                var accountName = inputs["AccountName"];
                var accountNo = inputs["AccountNo"];
                var notifyUrl = inputs["CallBackUrl"];
                var bankCode = inputs["BankCode"];
            
                var req = new Dictionary<string, string>
                {
                    { "client_key", merchantKey },
                    { "amount", strAmount },
                    { "channel_id", payCode },
                    { "out_transfer_no", transId },
                    { "notify_url", notifyUrl },
                    { "payee_account", accountNo},
                    { "payee_name", accountName},
                };


                if (payCode == "5001")
                {
                    var extraJson = new
                    {
                        bank_code = bankCode
                    };
                    var extraRequest = JsonConvert.SerializeObject(extraJson);
                    req.Add("extra", extraRequest);
                }
                
                req.Add("signature", Hash(req, secretKey));
                
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
        
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var transId = payResource["TransId"]; 
            var secretKey = payResource["SecretKey"];
            
            var req = FromJson<CallBackCreditRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                { "client_key", req .ClientKey },
                { "amount", req.Amount.ToString() },
                { "channel_id", req.ChannelId },
                { "transfer_no", req.TransferNo },
                { "out_transfer_no", req.OutTransferNo },
                { "created_at", req.CreatedAt },
                { "status" , req.Status.ToString()}
            };

            switch (req.Status)
            {
                case 1:
                    dic.Add("paid_at",req.PaidAt);
                    break;
                case 3:
                    return new NotifyWithdrawResult
                    {
                        WithdrawId = transId,
                        TransStatus = TransStatus.Fail,
                        Response = $"DaYangPay Payout Fail,Please inquire with DaYangPay. DaYangPay Message:{req.Message}"
                    };
            }


            if (!VerifySign(req.Signature, dic,secretKey))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var resultJson = new
            {
                code = DaYangDefinition.CallbackResponse
            };

            var resultJsonString = JsonConvert.SerializeObject(resultJson);
            
            //DaYangPay 只回傳 成功訂單 或 失敗 不是1(已支付) 或者 3(代付失敗) 直接Pending
            //Note : 代付订单状态为 代付成功 或 代付失败 ，才会通知商户，其他订单状态不会通知商户。
            //如果首次通知商户出现异常，会尝试再通知4次，共5次。频率为：0s/10s/10s/30s/30s。时间可能有1~2秒的差异，以实际为准。
            var result = new NotifyWithdrawResult()
            {
                ThirdId = req.TransferNo,
                WithdrawId = req.OutTransferNo,
                TransStatus = req.Status == 1 ? TransStatus.Success : TransStatus.Pending, // DaYangPay只回調成功或失敗的單，失敗前面就直接回傳了
                Response = resultJsonString
            };

            return result;
        }
        
        #endregion
        
        #region UnUse
        
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
        
        #endregion

        #region Private
        
        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + DaYangDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            
            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DaYangPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            return result;
        }
        
        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + DaYangDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"DaYangPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            return result;
        }
        
        private bool VerifySign(string target, Dictionary<string, string> param, string clientSecret)
        {
            var sign = Hash(param, clientSecret);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private string Hash(Dictionary<string, string> param, string key)
        {
            var sortedParameters = param.OrderBy(p => p.Key);
            var queryString = string.Join("&", sortedParameters.Select(x => $"{x.Key}={x.Value}"));
            _logger.LogInformation(queryString);
            var sign = ToHMACSHA256(queryString,key).ToLower();
            _logger.LogInformation(sign);
            return sign;
        }

        #endregion
       
    }
}