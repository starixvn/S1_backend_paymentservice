using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.FoxPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private readonly ILogger<PayHelper> _logger;
        
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        }

        #region Models

        public class Response
        {
            [JsonProperty("code")]
            public string Code { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("data")]
            public ResponseData Data { get; set; }
        }

        public class ResponseData
        {
            [JsonProperty("orderNumber")]
            public string OrderNumber { get; set; }
            [JsonProperty("amount")]
            public string Amount { get; set; }
        }
        
        private class CallBackReq
        {
            public string encryptedData { get; set; }
            public string thirdOrderNumber { get; set; }
        }
        
        private class CallBackData
        {
            public string amount { get; set; }
            public string fiatOrderId { get; set; }
            public string ip { get; set; }
            public string name { get; set; }
            public string orderNumber { get; set; }
            public int orderType { get; set; }
            public string payerAccount { get; set; }
            public int paymentId { get; set; }
            public int status { get; set; }
            public string thirdOrderNumber { get; set; }
            public string utrNo { get; set; }
        }
        
        private class CallBackResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("success")]
            public bool Success { get; set; }
        }

        #endregion
        
        #region Deposit

        private class DepositReq
        {
            public string amount { get; set; }
            public string thirdOrderNumber { get; set; }
            public string thirdUserId { get; set; }
        }
        
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            sUrl += "getRechargeData";

            var thirdUserId = inputs["CustId"];
            if (thirdUserId.Length < 6)
            {
                thirdUserId = thirdUserId.PadLeft(6, '0');
            }
            var req = new DepositReq
            {
                amount = decimal.Parse(inputs["Amount"]).ToString("#"),
                thirdOrderNumber = inputs["TransID"],
                thirdUserId = thirdUserId
            };
            
            var sResponse = HttpPost(sUrl, ToJson(req), "application/json", inputs["apiId"], inputs["key"], req.thirdOrderNumber);

            var depositRes = JsonConvert.DeserializeObject<Response>(sResponse);
            var oDepositResult = new DepositResult();
            
            if (depositRes.Code == "0")
            {
                var resData = new Dictionary<string, string>
                {
                    {"OrderNumber", depositRes.Data.OrderNumber},
                    {"Amount", depositRes.Data.Amount}
                };
                var payUrl = $"{inputs["PayUrl"]}{depositRes.Data.OrderNumber}";
                
                oDepositResult.Url = payUrl;
                oDepositResult.FormData = resData;
                oDepositResult.ThirdId = depositRes.Data.OrderNumber;
                oDepositResult.Success = true;
                oDepositResult.Message = depositRes.Message;
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.ErrorCode = depositRes.Code;
                oDepositResult.Message = depositRes.Message;
            }

            return oDepositResult;
        }
        
        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        { 
            var preData = JsonConvert.DeserializeObject<CallBackReq>(bodyContent);
            return preData?.thirdOrderNumber;
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var pay = GetPaySetting(bodyContent);
            var data = DecryptResponse(pay["key"].Substring(0,16),pay["iv"],bodyContent);

            var response = new CallBackResponse
            {
                Code = (int)FoxPayStatus.Success,
                Message = "success",
                Success = true
            };
            
            var oNotify = new NotifyDepositResult
            {
                DepositId = payResource["TransID"],
                Comment = "success"
            };

            if (data != null)
            {
                _logger.LogInformation($"CallBackDebit Decrypt Data, Message:{JsonConvert.SerializeObject(data)}");
                
                oNotify.RefThirdId = data.utrNo;
                oNotify.ThirdId = data.fiatOrderId;
                
                switch (data.status)
                {
                    case (int)FoxPayStatus.Success:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.Message = "deposit success";
                        oNotify.RequestAmount = decimal.Parse(data.amount);
                        oNotify.ActualAmount = decimal.Parse(data.amount);
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                    
                    case (int)FoxPayStatus.Fail:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Message = "cancel deposit";
                        oNotify.Comment = "Fail";
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                    
                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        response.Code = (int)FoxPayStatus.ResponseError;
                        response.Message = "Unknown status";
                        response.Success = false;
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                }
            }
            else
            {
                response.Code = (int)FoxPayStatus.ResponseError;
                response.Message = "Decrypt Error";
                response.Success = false;
                oNotify.Message = "Decrypt Error";
                oNotify.Comment = "Decrypt Error";
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = JsonConvert.SerializeObject(response);
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
            var sUrl = inputs["host"];
            sUrl += "issue";
            
            var accountName = inputs["AccountName"];
            if (accountName.Length < 6)
            {
                accountName = accountName.PadLeft(6, '0');
            }
            
            var thirdUserId = inputs["CustId"];
            if (thirdUserId.Length < 6)
            {
                thirdUserId = thirdUserId.PadLeft(6, '0');
            }
            
            var preRequest = new PreWithdrawalReq
            {
                amount = Convert.ToDecimal(inputs["Amount"]).ToString(),
                thirdOrderNumber = inputs["TransID"],
                thirdUserId = thirdUserId,
                issuePayPo = new IssuePayPo
                {
                    accountName = inputs["AccountNo"],
                    name = accountName,
                    paymentId = inputs["EWalletSysId"] == ((int)FoxPayEWalletSysId.Nagad).ToString() ? ((int)FoxPayPaymentId.Nagad).ToString() : ((int)FoxPayPaymentId.Bkash).ToString()
                }
            };
            
            var reqJson = JsonConvert.SerializeObject(preRequest);
            
            _logger.LogInformation($"FoxPay Credit Data, Message:{reqJson}");
            
            var encryptedData = EncryptResponse(inputs["key"].Substring(0,16),inputs["iv"],reqJson);
            
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var nonce = DateTime.Now.ToString("HHmmssddMMyyyy");
            var reqArr = new[] { inputs["key"], timestamp, nonce , inputs["apiId"]};
            Array.Sort(reqArr);
            var content = new StringBuilder();
            foreach (var t in reqArr)
            {
                content.Append(t);
            }

            content.Append(reqJson);
            var signature = ToSHA1(content.ToString());

            var sendReq = new WithdrawalReq
            {
                encryptedData = encryptedData,
                signaturePo = new SignaturePo
                {
                    apiId = inputs["apiId"],
                    nonce = nonce,
                    signature = signature,
                    timestamp = timestamp
                }
            };

            var sResponse = HttpPost(sUrl, ToJson(sendReq), "application/json", preRequest.thirdOrderNumber);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = JsonConvert.DeserializeObject<Response>(sResponse);
            if (withdrawalRes.Code == "0")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.Message;
                withdrawResult.RawContent = JsonConvert.SerializeObject(withdrawalRes.Data);
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.Message;
                withdrawResult.RawContent = JsonConvert.SerializeObject(withdrawalRes.Data);
            }

            return withdrawResult;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = DecryptResponse(payResource["key"].Substring(0,16),payResource["iv"],bodyContent);
            
            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = data.thirdOrderNumber,
                Message = "success",
                ThirdId = data.fiatOrderId
            };
            
            var response = new CallBackResponse
            {
                Code = (int)FoxPayStatus.Success,
                Message = "success",
                Success = true
            };

            if (data != null)
            {
                _logger.LogInformation($"CallBackCredit Decrypt Data, Message:{JsonConvert.SerializeObject(data)}");
                
                switch (data.status)
                {
                    case (int)FoxPayStatus.Success:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.RequestAmount = decimal.Parse(data.amount);
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                    
                    case (int)FoxPayStatus.Fail:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                    
                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        response.Code = (int)FoxPayStatus.ResponseError;
                        response.Message = "Unknown status";
                        response.Success = false;
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                }
            }
            else
            {
                response.Code = (int)FoxPayStatus.ResponseError;
                response.Message = "Decrypt Error";
                response.Success = false;
                oNotify.Message = "Decrypt Error";
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = JsonConvert.SerializeObject(response);
            }
            
            
            return oNotify;
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

        #endregion

        #region private

        private CallBackData DecryptResponse(string key , string iv ,string bodyContent)
        {
            try
            {
                var preData = JsonConvert.DeserializeObject<CallBackReq>(bodyContent);
                var decryptData = AES128CBCDecrypt(key, iv, preData.encryptedData);
                var data = JsonConvert.DeserializeObject<CallBackData>(decryptData);
                return data;
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Decrypt failed, Message:{e.Message}");
                return null;
            }
        }
        
        private string EncryptResponse(string key , string iv ,string bodyContent)
        {
            try
            {
                var decryptData = AES128CBCEncrypt(key, iv, bodyContent);
                return decryptData;
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Decrypt failed, Message:{e.Message}");
                return null;
            }
        }

        private Dictionary<string, string> GetPaySetting(string bodyContent)
        {
            var preData = JsonConvert.DeserializeObject<CallBackReq>(bodyContent);
            var transIdData = PaymentHelper.GetDepositRequest(preData.thirdOrderNumber);
            var paySetting = PaymentHelper.GetPaymentAllList(transIdData.SiteId).FirstOrDefault(x => x.PayId == 36);
            var pay = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
            return pay;
        }

        #endregion
    }
}
