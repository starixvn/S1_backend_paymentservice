using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.LuckyPay
{
    public class PayHelper : PayHelperBase
    {
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region Deposit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var payCurrency = input["PayCurrencyId"];
            var host = input[$"{payCurrency}host"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "clientCode", input[$"{payCurrency}merchantNo"] },
                    { "chainName", "BANK" },
                    { "coinUnit", payCurrency },
                    { "clientNo", input["TransID"] },
                    { "requestTimestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() }
                };

                req.Add("sign", GetSign(req, input[$"{payCurrency}key"]));
                req.Add("memberFlag", input["CustId"]);
                req.Add("requestAmount", input["Amount"]);
                req.Add("callbackurl", input["CallBackUrl"]);
                req.Add("hrefbackurl", input["ReturnUrl"]);
                req.Add("toPayQr", "0");
                
                if (payCurrency.Equals("MYR"))
                {
                    req.Add("channel", input["PayCode"]);
                }
                
                var res = CreateOrder(host, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data.PayUrl
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation("LuckyPay create payout failed: " + ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = formData["clientNo"];
            var payCurrency = formData["coinUnit"];
            var status = formData["status"];
            
            try
            {
                var dic4Sign = new Dictionary<string, string>
                {
                    { "clientCode", formData["clientCode"] },
                    { "clientNo", formData["clientNo"] },
                    { "orderNo", formData["orderNo"] },
                    { "payAmount", formData["payAmount"] },
                    { "status", status },
                    { "txid", formData["txid"] }
                };

                if (!VerifySign(dic4Sign, payResource[$"{payCurrency}key"], formData["sign"]))
                    throw new CustomException($"{transId} signature verification failed.");
                
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    ThirdId = formData["orderNo"],
                    Comment = $"orderStatus={status}",
                    TransStatus = Definition.GetTransStatus(status),
                    ActualAmount = decimal.Parse(formData["payAmount"]),
                    Response = Definition.CallbackResponse
                };
            }
            catch (CustomException ex)
            {
                return new NotifyDepositResult
                {
                    DepositId = payResource["TransID"],
                    TransStatus = TransStatus.Pending,
                    Response = ex.Message
                };
            }
        }

        #endregion

        #region Withdrawal

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var payCurrency = inputs["PayCurrencyId"];
            var host = inputs[$"{payCurrency}host"];
            var transId = inputs["TransID"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "clientCode", inputs[$"{payCurrency}merchantNo"] },
                    { "chainName", "BANK" },
                    { "coinUnit", payCurrency },
                    { "clientNo", transId },
                    { "requestTimestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() }
                };

                req.Add("sign", GetSign(req, inputs[$"{payCurrency}key"]));
                req.Add("bankCardNum", inputs["AccountNo"]);
                req.Add("bankUserName", inputs["AccountName"]);
                //THB沒有IFSC所以固定代字串IFSC
                req.Add("ifsc", payCurrency == "INR" ? inputs["IFSCCode"] : "IFSC");
                req.Add("bankName", inputs["BankCode"]);
                req.Add("amount", inputs["Amount"]);
                req.Add("callbackurl", inputs["CallBackUrl"]);

                var res = Payout(host, req);
                
                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId,
                    RawContent = JsonConvert.SerializeObject(res)
                };;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation(ex.Message);

                return new WithdrawResult
                {
                    Success = false,
                    WithdrawId = transId,
                    Message = ex.Message
                };
            }
        }
        
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var currency = formData["coinUnit"];
            var transId = formData["clientNo"];
            
            var dic4Sign = new Dictionary<string, string>
            {
                { "clientCode", formData["clientCode"] },
                { "clientNo", formData["clientNo"] },
                { "orderNo", formData["orderNo"] },
                { "payAmount", formData["payAmount"] },
                { "status", formData["status"] },
                { "txid", formData["txid"] }
            };
            
            if (!VerifySign(dic4Sign, payResource[$"{currency}_key"], formData["sign"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{transId} signature verification failed."
                };
            }
            
            return new NotifyWithdrawResult
            {
                ThirdId = formData["clientNo"],
                TransStatus = Definition.GetTransStatus(formData["status"]),
                Response = Definition.CallbackResponse,
                WithdrawId = transId,
                Message = $"status={formData["status"]}, txid={formData["txid"]}"
            };
        }

        #endregion

        #region NotImplemented
        
        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        #endregion

        #region private
        
        private CreatOrderResp CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, CreatOrderResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"Code={statusCode}");

            if (!result.Success)
                throw new CustomException($"Code={result.Code}, Msg={result.Message}");

            return result;
        }

        private CreatPayoutResp Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"Code={statusCode}");

            if (!result.Success)
                throw new CustomException($"Code={result.Code}, Msg={result.Message}");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string key)
        {
            var str = string.Join("&", param
                    .Select(x => $"{x.Value}")) + key;

            _logger.LogInformation("str: " + str);
            var sign = ToMD5(str).ToLower();

            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string key , string target)
        {
            var sign = GetSign(param, key);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}