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

        private class RequestBase : EPayBase
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        public class ResponseBase
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

        public class ResponseDataBase : EPayBase
        {
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        #region Debit

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
            var p = string.Join("", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase) && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Value}").ToArray()) + key;
            content.Sign = ToMD5(p).ToLower();

            var req = new RequestBase
            {
                MerchantCode = inputs["merchantCode"],
                SignType = "md5",
                Content = JsonConvert.SerializeObject(content)
            };

            var sResponse = HttpPost(sUrl, ToJson(req), "application/json", inputs["TransID"], true);

            var depositRes = JsonConvert.DeserializeObject<ResponseBase>(sResponse);

            var depositDataRes = new ResponseDataBase();
            var depositContentRes = new DebitResponseContent();
            if (depositRes.Data != null)
            {
                depositDataRes = JsonConvert.DeserializeObject<ResponseDataBase>(depositRes.Data);
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

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
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

        private class WithdrawalReqContent
        {
            [JsonProperty("merchantCode")]
            public string MerchantCode { get; set; }
            [JsonProperty("merchantTradeNo")]
            public string MerchantTradeNo { get; set; }
            [JsonProperty("accountName")]
            public string AccountName { get; set; }
            [JsonProperty("amount")]
            public string Amount { get; set; }
            [JsonProperty("notifyUrl")]
            public string NotifyUrl { get; set; }
            [JsonProperty("bankCardNumber")]
            public string BankCardNumber { get; set; }
            [JsonProperty("bankCode")]
            public string BankCode { get; set; }
            [JsonProperty("bankName")]
            public string BankName { get; set; }
            [JsonProperty("channel")]
            public string Channel { get; set; }
            [JsonProperty("sign")]
            public string Sign { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var hostDic = JsonConvert.DeserializeObject<dynamic>(inputs["host"]);
            var keyDic = JsonConvert.DeserializeObject<dynamic>(inputs["key"]);
            var sUrl = hostDic[inputs["PayCurrencyId"]].ToString();
            var key = keyDic[inputs["PayCurrencyId"]].ToString();

            sUrl += "pay/center/withdrawal/apply";

            var content = new WithdrawalReqContent
            {
                MerchantCode = inputs["merchantCode"],
                MerchantTradeNo = inputs["TransID"],
                AccountName = inputs["AccountName"],
                Amount = decimal.Parse(inputs["Amount"]).ToString("0.00"),
                NotifyUrl = inputs["CallBackUrl"],
                BankCardNumber = inputs["AccountNo"],
                BankCode = string.IsNullOrEmpty(inputs["BankCode"]) ? inputs["IFSCCode"] ?? inputs["BankName"] : inputs["BankCode"],
                BankName = inputs["BankName"],
                Channel = inputs["PayCode"],
                Sign = null
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(content)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);
            var p = string.Join("", dicP.Where(x => !x.Value.Equals("", StringComparison.OrdinalIgnoreCase)
                                                    && !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase)
                                                    && !x.Key.Equals("bankName", StringComparison.OrdinalIgnoreCase)).Select(kp => $"{kp.Value}").ToArray()) + key;
            content.Sign = ToMD5(p).ToLower();

            var req = new RequestBase
            {
                MerchantCode = inputs["merchantCode"],
                SignType = "md5",
                Content = JsonConvert.SerializeObject(content)
            };

            var sResponse = HttpPost(sUrl, ToJson(req), "application/json", inputs["TransID"], true).ToString();

            var withdrawalResponse = JsonConvert.DeserializeObject<ResponseBase>(sResponse);

            var withdrawalDataRes = new ResponseDataBase();
            if (withdrawalResponse.Data != null)
            {
                withdrawalDataRes = JsonConvert.DeserializeObject<ResponseDataBase>(withdrawalResponse.Data);
            }

            var withdrawResult = new WithdrawResult();

            if (withdrawalResponse.Status == "SUCCESS")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalResponse.Message;
                withdrawResult.RawContent = withdrawalDataRes.Content;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalResponse.Message;
                withdrawResult.RawContent = withdrawalDataRes.Content;
            }

            return withdrawResult;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackData>(bodyContent);
            var dataContent = FromJson<CallBackDataContent>(data.Content);

            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = dataContent.MerchantTradeNo,
                Message = "success",
                ThirdId = dataContent.TradeNo
            };

            if (dataContent != null)
            {
                _logger.LogInformation($"CallBackCredit Data, Message:{JsonConvert.SerializeObject(data)}");

                switch (dataContent.TradeStatus)
                {
                    case "WITHDRAWAL_SUCCESS":
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.RequestAmount = decimal.Parse(dataContent.Amount);
                        oNotify.Message = "Withdrawal Success";
                        oNotify.Response = "SUCCESS";
                        break;

                    case "WITHDRAWAL_FAILURE":
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Message = "Withdrawal Reject";
                        oNotify.Response = "SUCCESS";
                        break;

                    case "WAITING_WITHDRAWAL":
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Message = "Withdrawal Pending";
                        oNotify.Response = "SUCCESS";
                        break;

                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Response = "Unknown Status";
                        break;
                }
            }
            else
            {
                oNotify.Message = "Data Error";
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = "Failed";
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

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
