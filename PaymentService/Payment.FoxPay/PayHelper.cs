using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;

namespace Payment.FoxPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;
        
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }
        
        #region Deposit

        private class DepositReq
        {
            public string amount { get; set; }
            public string thirdOrderNumber { get; set; }
            public string thirdUserId { get; set; }
        }
        
        public class DepositResponse
        {
            [JsonProperty("code")]
            public string Code { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("success")]
            public bool Success { get; set; }
            [JsonProperty("data")]
            public DepositResponseData Data { get; set; }
        }

        public class DepositResponseData
        {
            [JsonProperty("orderNumber")]
            public string OrderNumber { get; set; }
            [JsonProperty("amount")]
            public string Amount { get; set; }
        }
        
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            sUrl += "getRechargeData";
         
            var req = new DepositReq
            {
                amount = decimal.Parse(inputs["Amount"]).ToString("#"),
                thirdOrderNumber = inputs["TransID"],
                thirdUserId = inputs["CustId"]
            };
            
            var sResponse = HttpPost(sUrl, ToJson(req), "application/json", inputs["apiId"], inputs["key"], req.thirdOrderNumber , true);

            var depositRes = JsonConvert.DeserializeObject<DepositResponse>(sResponse);
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
        
        private class CallBackDebitReq
        {
            public string encryptedData { get; set; }
            public string thirdOrderNumber { get; set; }
        }
        
        private class CallBackDebitData
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
        
        private class CallBackDebitResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("success")]
            public bool Success { get; set; }
        }
        
        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        { 
            var preData = JsonConvert.DeserializeObject<CallBackDebitReq>(bodyContent);
            return preData?.thirdOrderNumber;
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var pay = GetPaySetting(bodyContent);
            var data = DecryptResponse(pay["key"].Substring(0,16),pay["iv"],bodyContent);

            var response = new CallBackDebitResponse
            {
                Code = 1,
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
                    case 1:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.Message = "deposit success";
                        oNotify.RequestAmount = decimal.Parse(data.amount);
                        oNotify.ActualAmount = decimal.Parse(data.amount);
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                    
                    case 0:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Message = "cancel deposit";
                        oNotify.Comment = "Fail";
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                    
                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        response.Code = 2;
                        response.Message = "Unknown status";
                        response.Success = false;
                        oNotify.Response = JsonConvert.SerializeObject(response);
                        break;
                }
            }
            else
            {
                response.Code = 2;
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

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            // var sUrl = inputs["host"];
            // var getReceptionToken = sUrl + "/api/login";
            // var getWalletAddress = sUrl + "/api/callBack";
            // var withdrawalInfo = inputs["WithdrawalInfo"];
            //
            // var depositGetH5Req = new Dictionary<string, string>();
            // depositGetH5Req.Add("email", inputs["Email"]);
            // depositGetH5Req.Add("password", inputs["Password"]);
            // var sParam = string.Join("&", depositGetH5Req.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            // string sResponse = null;
            //
            //
            // var oDepositResult = new DepositResult();
            // oDepositResult.Success = false;
            //
            // var withdrawResult = new WithdrawResult();
            // // 檢查 WithdrawalInfo
            // if (withdrawalInfo == null)
            // {
            //     withdrawResult.Success = false;
            //     withdrawResult.Message = "WithdrawalInfo is empty.";
            //     return withdrawResult;
            // }
            // try
            // {
            //     sResponse = HttpPost(getReceptionToken, sParam, "application/x-www-form-urlencoded", inputs["TransID"].ToLower(), true);
            //
            //     var depositResp = new DepositResp();
            //     depositResp = JsonConvert.DeserializeObject<DepositResp>(sResponse);
            //
            //     if (depositResp != null && depositResp.access_token != null)
            //     {
            //         var withdrawGetWalletReq = new WithdrawGetWalletReq();
            //         withdrawGetWalletReq.command = "partner_withdraw";
            //         var amount = decimal.Parse(inputs["Amount"]);
            //         withdrawGetWalletReq.amount = amount.ToString("#");
            //         withdrawGetWalletReq.currency = currMapping(inputs["CurrencyId"]);
            //         withdrawGetWalletReq.withdrawal_token = inputs["PayCode"];
            //         withdrawGetWalletReq.callback_url = inputs["CallBackUrl"];
            //         withdrawGetWalletReq.address = inputs["AccountNo"];
            //         withdrawGetWalletReq.customer_uid = inputs["CustId"];
            //         withdrawGetWalletReq.client_reference_id = inputs["TransID"];
            //         withdrawGetWalletReq.hashCode = ToMD5("partner_withdraw" + inputs["key"]).ToLower();
            //         var message = "";
            //         var toAmount = GetExchangeAmountAsync(withdrawGetWalletReq.amount, withdrawGetWalletReq.currency, withdrawGetWalletReq.withdrawal_token, inputs).Result;
            //
            //         if (toAmount > 0)
            //         {
            //             withdrawGetWalletReq.withdrawal_amount = toAmount.ToString();
            //             var sResponse1 = HttpPost(getWalletAddress, ToJson(withdrawGetWalletReq), "application/json", depositResp.access_token, inputs["TransID"].ToLower(), true);
            //
            //             var withdrawGetWalletResp = new WithdrawGetWalletResp();
            //             withdrawGetWalletResp = JsonConvert.DeserializeObject<WithdrawGetWalletResp>(sResponse1);
            //
            //             withdrawResult.Message = withdrawGetWalletResp.message;
            //             withdrawResult.RawContent = withdrawGetWalletResp.message;
            //             if (withdrawGetWalletResp.status == "pending")
            //             {
            //                 withdrawResult.Success = true;
            //                 withdrawResult.WithdrawalInfo =
            //                     withdrawalInfo.AddOrSetExchangeInfo((toAmount / amount).ToString(), toAmount.ToString()); ;
            //             }
            //             else
            //             {
            //                 withdrawResult.Success = false;
            //             }
            //         }
            //         else
            //         {
            //             withdrawResult.Success = false;
            //             oDepositResult.Message = message;
            //             oDepositResult.RawContent = message;
            //         }
            //     }
            //     else
            //     {
            //         oDepositResult.Message = depositResp.message;
            //         oDepositResult.RawContent = sResponse;
            //     }
            // }
            // catch (WebException wEx)
            // {
            //     var err = FromJson<Dictionary<string, string>>(new StreamReader(wEx.Response?.GetResponseStream()).ReadToEnd());
            //     if (err.ContainsKey("error"))
            //     {
            //         oDepositResult.Message = $"{err["error"]}-{err["error_message"]}";
            //         oDepositResult.RawContent = $"{err["error"]}-{err["error_message"]}";
            //     }
            //     if (err.ContainsKey("err"))
            //     {
            //         oDepositResult.Message = $"{err["err"]}";
            //         oDepositResult.RawContent = $"{err["err"]}";
            //     }
            //
            //     _logger.LogError(wEx.InnerException, wEx.Message);
            // }
            //
            // return withdrawResult;
            return null;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            // var data = FromJson<CallBackWithdrawReq>(bodyContent);
            //
            // var oNotify = new NotifyWithdrawResult();
            // oNotify.WithdrawId = data.client_reference_id;
            // oNotify.Message = data.status;
            // oNotify.ThirdId = data.txid;
            //
            // if (data.status == "completed")
            // {
            //     oNotify.TransStatus = TransStatus.Success;
            //     oNotify.RequestAmount = decimal.Parse(data.amount);
            //     oNotify.Response = "true";
            // }
            // else if (data.status == "rejected")
            // {
            //     oNotify.TransStatus = TransStatus.Fail;
            //     oNotify.Response = "true";
            // }
            // else
            // {
            //     oNotify.TransStatus = TransStatus.Pending;
            //     oNotify.Response = "true";
            // }
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

        #endregion

        #region private

        private CallBackDebitData? DecryptResponse(string key , string iv ,string bodyContent)
        {
            try
            {
                var preData = JsonConvert.DeserializeObject<CallBackDebitReq>(bodyContent);
                var decryptData = AES128CBCDecrypt(key, iv, preData.encryptedData);
                var data = JsonConvert.DeserializeObject<CallBackDebitData>(decryptData);
                return data;
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Decrypt failed, Message:{e.Message}");
                return null;
            }
        }

        private Dictionary<string, string> GetPaySetting(string bodyContent)
        {
            var preData = JsonConvert.DeserializeObject<CallBackDebitReq>(bodyContent);
            var transIdData = PaymentHelper.GetDepositRequest(preData.thirdOrderNumber);
            var paySetting = PaymentHelper.GetPaymentAllList(transIdData.SiteId).FirstOrDefault(x => x.PayId == 36);
            var pay = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
            return pay;
        }

        #endregion
    }
}
