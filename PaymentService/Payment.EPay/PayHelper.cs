using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.EPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private readonly ILogger<PayHelper> _logger;
        
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        }

        #region Models
        
        public class EPayBase
        {
            [JsonProperty("merchantCode")]
            public string MerchantCode { get; set; }
            [JsonProperty("signType")]
            public string SignType { get; set; }
        }

        #region Debit

        public class DebitResponse
        {
            [JsonProperty("status")]
            public string Status { get; set; }
            [JsonProperty("errorCode")]
            public string ErrorCode { get; set; }
            [JsonProperty("msg")]
            public string Message { get; set; }
            [JsonProperty("data")]
            public string Data { get; set; }
        }

        public class DebitResponseData : EPayBase
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }
        
        public class DebitResponseContent
        {
            [JsonProperty("merchantCode")]
            public string MerchantCode { get; set; }
            [JsonProperty("merchantTradeNo")]
            public string MerchantTradeNo { get; set; }
            [JsonProperty("tradeNo")]
            public string TradeNo { get; set; }
            [JsonProperty("payUrl")]
            public string PayUrl { get; set; }
            [JsonProperty("sign")]
            public string Sign { get; set; }
        }
        
        private class CallBackData : EPayBase
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        private class CallBackDataContent
        {
            [JsonProperty("amount")]
            public string Amount { get; set; }
            [JsonProperty("tradeStatus")]
            public string TradeStatus { get; set; }
            [JsonProperty("notifyTime")]
            public string NotifyTime { get; set; }
            [JsonProperty("sign")]
            public string Sign { get; set; }
            [JsonProperty("tradeNo")]
            public string TradeNo { get; set; }
            [JsonProperty("merchantTradeNo")]
            public string MerchantTradeNo { get; set; }
            [JsonProperty("merchantCode")]
            public string MerchantCode { get; set; }
        }

        #endregion
        
        #endregion
        
        #region Deposit

        private class DepositReq : EPayBase
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        private class DepositReqContent
        {
            [JsonProperty("merchantCode")]
            public string MerchantCode { get; set; }
            [JsonProperty("merchantTradeNo")]
            public string MerchantTradeNo { get; set; }
            [JsonProperty("userId")]
            public string UserId { get; set; }
            [JsonProperty("amount")]
            public string Amount { get; set; }
            [JsonProperty("notifyUrl")]
            public string NotifyUrl { get; set; }
            [JsonProperty("terminalType")]
            public int TerminalType { get; set; }
            [JsonProperty("channel")]
            public string Channel { get; set; }
            [JsonProperty("sign")]
            public string Sign { get; set; }
        }
        
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var hostDic = JsonConvert.DeserializeObject<dynamic>(inputs["host"]);
            var keyDic = JsonConvert.DeserializeObject<dynamic>(inputs["key"]);
            var sUrl = hostDic[inputs["PayCurrencyId"]].ToString();
            var key = keyDic[inputs["PayCurrencyId"]].ToString();

            sUrl += "pay/center/deposit/apply";
            
            var content = new DepositReqContent
            {
                MerchantCode = inputs["merchantCode"],
                MerchantTradeNo = inputs["TransID"],
                UserId = inputs["CustId"],
                Amount = decimal.Parse(inputs["Amount"]).ToString("0.00"),
                NotifyUrl = inputs["CallBackUrl"],
                TerminalType = 1,
                Channel = inputs["PayCode"],
                Sign = null
            };
            
            var dicP = FromJson<Dictionary<string, string>>(ToJson(content)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Value}").ToArray()) + key ;
            content.Sign = ToMD5(p).ToLower();
            
            var req = new DepositReq
            {
                MerchantCode = inputs["merchantCode"],
                SignType = "md5",
                Content = JsonConvert.SerializeObject(content)
            };
      
            var sResponse = HttpPost(sUrl, ToJson(req), "application/json", inputs["TransID"] , true);
            
            var depositRes = JsonConvert.DeserializeObject<DebitResponse>(sResponse);

            var depositDataRes = new DebitResponseData();
            var depositContentRes = new DebitResponseContent();
            if (depositRes.Data != null)
            {
                depositDataRes = JsonConvert.DeserializeObject<DebitResponseData>(depositRes.Data);
            }
            if (depositDataRes.Content != null)
            {
                depositContentRes = JsonConvert.DeserializeObject<DebitResponseContent>(depositDataRes.Content);
            }
            
            var oDepositResult = new DepositResult();
            if (depositRes.Status == "SUCCESS")
            {
                var resData = new Dictionary<string, string>
                {
                    {"MerchantCode", depositContentRes.MerchantCode},
                    {"SignType", depositDataRes.SignType},
                    {"MerchantTradeNo", depositContentRes.MerchantTradeNo},
                    {"TradeNo", depositContentRes.TradeNo},
                    {"PayUrl", depositContentRes.PayUrl},
                    {"Sign", depositContentRes.Sign}
                };
              
                oDepositResult.Url = depositContentRes.PayUrl;
                oDepositResult.FormData = resData;
                oDepositResult.ThirdId = depositContentRes.TradeNo;
                oDepositResult.Success = true;
                oDepositResult.Message = depositRes.Message;
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.ErrorCode = depositRes.ErrorCode;
                oDepositResult.Message = depositRes.Message;
            }

            return oDepositResult;
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackData>(bodyContent);
            var dataContent = FromJson<CallBackDataContent>(data.Content);
            
            var oNotify = new NotifyDepositResult
            {
                DepositId = payResource["TransID"],
                Comment = "success"
            };

            if (dataContent != null)
            {
                _logger.LogInformation($"CallBackDebit Data, Message:{JsonConvert.SerializeObject(data)}");
                
                oNotify.RefThirdId = dataContent.TradeNo;
                oNotify.ThirdId = dataContent.TradeNo;
                
                switch (dataContent.TradeStatus)
                {
                    case "PAYMENT_SUCCESS":
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.Message = "deposit success";
                        oNotify.RequestAmount = decimal.Parse(dataContent.Amount);
                        oNotify.ActualAmount = decimal.Parse(dataContent.Amount);
                        oNotify.Response = "SUCCESS";
                        break;
                    
                    case "PAYMENT_FAILURE":
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Message = "cancel deposit";
                        oNotify.Response = "SUCCESS";
                        break;
                    
                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Comment = "Unknown status";
                        oNotify.Response = "Unknown status";
                        break;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Data Error";
                oNotify.Response = "Data Error";
            }
            
            return oNotify;
        }
        
        #endregion

        #region Withdrawal
        
        public class PreWithdrawalReq
        {
            public string amount { get; set; }
            public string thirdOrderNumber { get; set; }
            public string thirdUserId { get; set; }
            public IssuePayPo issuePayPo { get; set; }
        }

        public class IssuePayPo
        {
            public string accountName { get; set; }
            public string name { get; set; }
            public string paymentId { get; set; }
        }
        
        public class WithdrawalReq
        {
            public string encryptedData { get; set; }
            public SignaturePo signaturePo { get; set; }
        }
        
        public class SignaturePo
        {
            public string apiId { get; set; }
            public string nonce { get; set; }
            public string signature { get; set; }
            public string timestamp { get; set; }
        }
        
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            // var sUrl = inputs["host"];
            // sUrl += "issue";
            //
            // var accountName = inputs["AccountName"];
            // if (accountName.Length < 6)
            // {
            //     accountName = accountName.PadLeft(6, '0');
            // }
            //
            // var thirdUserId = inputs["CustId"];
            // if (thirdUserId.Length < 6)
            // {
            //     thirdUserId = thirdUserId.PadLeft(6, '0');
            // }
            //
            // var preRequest = new PreWithdrawalReq
            // {
            //     amount = Convert.ToDecimal(inputs["Amount"]).ToString(),
            //     thirdOrderNumber = inputs["TransID"],
            //     thirdUserId = thirdUserId,
            //     issuePayPo = new IssuePayPo
            //     {
            //         accountName = inputs["AccountNo"],
            //         name = accountName,
            //         paymentId = inputs["EWalletSysId"] == ((int)FoxPayEWalletSysId.Nagad).ToString() ? ((int)FoxPayPaymentId.Nagad).ToString() : ((int)FoxPayPaymentId.Bkash).ToString()
            //     }
            // };
            //
            // var reqJson = JsonConvert.SerializeObject(preRequest);
            //
            // _logger.LogInformation($"FoxPay Credit Data, Message:{reqJson}");
            //
            // var encryptedData = EncryptResponse(inputs["key"].Substring(0,16),inputs["iv"],reqJson);
            //
            // var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            // var nonce = DateTime.Now.ToString("HHmmssddMMyyyy");
            // var reqArr = new[] { inputs["key"], timestamp, nonce , inputs["apiId"]};
            // Array.Sort(reqArr);
            // var content = new StringBuilder();
            // foreach (var t in reqArr)
            // {
            //     content.Append(t);
            // }
            //
            // content.Append(reqJson);
            // var signature = ToSHA1(content.ToString());
            //
            // var sendReq = new WithdrawalReq
            // {
            //     encryptedData = encryptedData,
            //     signaturePo = new SignaturePo
            //     {
            //         apiId = inputs["apiId"],
            //         nonce = nonce,
            //         signature = signature,
            //         timestamp = timestamp
            //     }
            // };
            //
            // var sResponse = HttpPost(sUrl, ToJson(sendReq), "application/json", preRequest.thirdOrderNumber);
            //
            // var withdrawResult = new WithdrawResult();
            // var withdrawalRes = JsonConvert.DeserializeObject<Response>(sResponse);
            // if (withdrawalRes.Code == "0")
            // {
            //     withdrawResult.Success = true;
            //     withdrawResult.Message = withdrawalRes.Message;
            //     withdrawResult.RawContent = JsonConvert.SerializeObject(withdrawalRes.Data);
            // }
            // else
            // {
            //     withdrawResult.Success = false;
            //     withdrawResult.Message = withdrawalRes.Message;
            //     withdrawResult.RawContent = JsonConvert.SerializeObject(withdrawalRes.Data);
            // }
            //
            // return withdrawResult;
            return null;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            // var data = DecryptResponse(payResource["key"].Substring(0,16),payResource["iv"],bodyContent);
            //
            // var oNotify = new NotifyWithdrawResult
            // {
            //     WithdrawId = data.thirdOrderNumber,
            //     Message = "success",
            //     ThirdId = data.fiatOrderId
            // };
            //
            // var response = new CallBackResponse
            // {
            //     Code = (int)FoxPayStatus.Success,
            //     Message = "success",
            //     Success = true
            // };
            //
            // if (data != null)
            // {
            //     _logger.LogInformation($"CallBackCredit Decrypt Data, Message:{JsonConvert.SerializeObject(data)}");
            //     
            //     switch (data.status)
            //     {
            //         case (int)FoxPayStatus.Success:
            //             oNotify.TransStatus = TransStatus.Success;
            //             oNotify.RequestAmount = decimal.Parse(data.amount);
            //             oNotify.Response = JsonConvert.SerializeObject(response);
            //             break;
            //         
            //         case (int)FoxPayStatus.Fail:
            //             oNotify.TransStatus = TransStatus.Fail;
            //             oNotify.Response = JsonConvert.SerializeObject(response);
            //             break;
            //         
            //         default:
            //             oNotify.TransStatus = TransStatus.Pending;
            //             response.Code = (int)FoxPayStatus.ResponseError;
            //             response.Message = "Unknown status";
            //             response.Success = false;
            //             oNotify.Response = JsonConvert.SerializeObject(response);
            //             break;
            //     }
            // }
            // else
            // {
            //     response.Code = (int)FoxPayStatus.ResponseError;
            //     response.Message = "Decrypt Error";
            //     response.Success = false;
            //     oNotify.Message = "Decrypt Error";
            //     oNotify.TransStatus = TransStatus.Pending;
            //     oNotify.Response = JsonConvert.SerializeObject(response);
            // }
            //
            //
            // return oNotify;
            return null;
        }

        #endregion

        #region Not Implemented
        
        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            throw new NotImplementedException();
        }

        #endregion

        
    }
}
