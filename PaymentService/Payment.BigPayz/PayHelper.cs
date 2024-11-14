using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.BigPayz
{
    public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
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
            var payCurrency = inputs["PayCurrencyId"];
            var key = inputs[$"{payCurrency}_key"];
            var type = GetPayType(inputs["PayCode"]);
            var merchantId = inputs[$"{payCurrency}_merchantId"];
            
            try
            {
                return type switch
                {
                    "bank" => DepositBank(inputs, key, merchantId),
                    "qr" => DepositQr(inputs, key, merchantId),
                    _ => new DepositResult(false, "-1", "BigPayz service not support type=" + type)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation("BigPayz creat order failed: " + ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var depositInfo = FromJson<DepositInfo>(payResource["DepositInfo"]);
            var key = payResource[$"{depositInfo.PayCurrencyId}_key"];
            var type = GetPayType(depositInfo.PayCode);

            try
            {
                if (formData == null || formData.Count == 0)
                    formData = FromJson<Dictionary<string, string>>(bodyContent);
                
                var info = type switch
                {
                    "bank" => new DepositCallbackInfo
                    {
                        VerifyStr = $"{formData["transaction"]}{formData["status"]}{formData["Currency"]}{formData["Amount"]}{formData["created_at"]}",
                        VerifyTarget = formData["signature2"],
                        Status = formData["status"],
                        ActualAmount = formData["Amount"]
                    },
                        
                    "qr" => new DepositCallbackInfo
                    {
                        VerifyStr = $"{formData["InvoiceNo"]}{formData["ReceiverBank"]}{formData["ReceiverAccount"]}" +
                                    $"{formData["Currency"]}{formData["Amount"]}{formData["PaymentDate"]}",
                        VerifyTarget = formData["Signature"],
                        Status = formData["Status"],
                        ActualAmount = formData["Amount"]
                    },
                    _ => throw new CustomException("not support type=" + type)
                };

                if (!VerifySign(info.VerifyStr, key, info.VerifyTarget))
                    throw new CustomException($"{payResource["TransID"]}, signature verification failed.");
                
                return new NotifyDepositResult
                {
                    DepositId = payResource["TransID"],
                    Comment = $"orderStatus={info.Status}",
                    TransStatus = Definition.GetTransStatus(info.Status),
                    ActualAmount = decimal.Parse(info.ActualAmount),
                    Response = ToJson(new CallbackResp(0, "success"))
                };
            }
            catch (CustomException ex)
            {
                return new NotifyDepositResult
                {
                    DepositId = payResource["TransID"],
                    TransStatus = TransStatus.Pending,
                    Response = ToJson(new CallbackResp(-1, ex.Message))
                };
            }
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var payCurrency = inputs["PayCurrencyId"];
            var key = inputs[$"{payCurrency}_key"];
            var merchantId = inputs[$"{payCurrency}_merchantId"];
            var transId = inputs["TransID"];
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchant_code", merchantId },
                    { "ref_id", transId },
                    { "player_username", inputs["CustId"] },
                    { "player_ip", inputs["ClientIP"] },
                    { "currency_code", inputs["PayCurrencyId"] },
                    { "amount", inputs["Amount"] },
                    { "bank_code", inputs["BankCode"] },
                    { "beneficiary_account", inputs["AccountNo"] },
                    { "beneficiary_name", inputs["AccountName"] }
                };
            
                if (payCurrency == "INR")
                    req.Add("ifsc", inputs["IFSCCode"]);
            
                var str = $"{req["merchant_code"]}{req["ref_id"]}{req["player_username"]}{req["player_ip"]}" +
                          $"{req["currency_code"]}{req["amount"]}{req["bank_code"]}{req["beneficiary_account"]}" +
                          $"{req["beneficiary_name"]}";

                req.Add("hash", GetSign(str, key));

                var resp = _httpHelper
                    .PostJson<Dictionary<string, string>, PayoutResp>(inputs["payout_url"], req);
                
                if (!resp.StatusCode.IsSuccessStatus())
                    throw new CustomException($"Code={resp.StatusCode}");
            
                if (!Definition.SuccessCode.Equals(resp.Result.ErrorCode))
                    throw new CustomException($"Code={resp.Result.ErrorCode}, Msg={resp.Result.ErrorMessage}");
                
                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId,
                    Message = resp.Result.ThirdId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation("BigPayz creat payout failed: " + ex.Message);
                
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
            var currency = formData["Currency"];
            var key = payResource[$"{currency}_key"];
            var transId = payResource["TransId"];
            
            var str = $"{formData["InvoiceNo"]}{formData["Status"]}{formData["Currency"]}{formData["Amount"]}{formData["PaymentDate"]}";

            if (!VerifySign(str, key, formData["Signature"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = ToJson(new CallbackResp(-1, $"{payResource["TransID"]}, signature verification failed."))
                };
            }
            
            return new NotifyWithdrawResult
            {
                ThirdId = formData["InvoiceNo"],
                TransStatus = Definition.GetTransStatus(formData["Status"]),
                Response = ToJson(new CallbackResp(0, "success")),
                WithdrawId = transId,
                Message = $"Status={formData["Status"]}, PaymentDate={formData["PaymentDate"]}"
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        
        public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            string refId;
            
            if (formData is { Count: > 0 })
            {
                formData.TryGetValue("RefId", out refId);
            }
            else
            {
                var data = FromJson<Dictionary<string, string>>(bodyContent);
                data.TryGetValue("RefId", out refId);
            }

            return refId;
        }

        #region Private
        
        private DepositResult DepositBank(IDictionary<string, string> inputs, string key, string merchantId)
        {
            var req = new Dictionary<string, string>
            {
                { "MerchantCode", merchantId },
                { "ReturnURL", inputs["ReturnUrl"] },
                { "FailedReturnURL", inputs["ReturnUrl"] },
                { "HTTPPostURL", inputs["CallBackUrl"] },
                { "Amount", inputs["Amount"] },
                { "Currency", inputs["PayCurrencyId"] },
                { "ItemID", inputs["TransID"] },
                { "ItemDescription", inputs["TransID"] },
                { "PlayerId", inputs["CustId"] }
            };
            var str = $"{req["MerchantCode"]}{req["ItemID"]}{req["Currency"]}{req["Amount"]}";
            req.Add("Hash", GetSign(str, key));
            
            var resp = _httpHelper
                .WithOAuthBearerToken(key)
                .PostJson<Dictionary<string, string>, DepositBankResp>(inputs["bank_url"], req);
            
            if (!resp.StatusCode.IsSuccessStatus())
                throw new CustomException($"Code={resp.StatusCode}, Msg={resp.Result.Message}");

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = resp.Result.ThirdId,
                Url = resp.Result.RedirectTo.Replace("redirectlink=", "")
            };
        }
        
        private DepositResult DepositQr(IDictionary<string, string> inputs, string key, string merchantId)
        {
            var method = GetPayMethod(inputs["PayCode"]);
            var req = new Dictionary<string, string>
            {
                { "merchant_code", merchantId },
                { "ref_id", inputs["TransID"] },
                { "player_username", inputs["CustId"] },
                { "player_ip", inputs["ClientIP"] },
                { "currency_code", inputs["PayCurrencyId"] },
                { "amount", inputs["Amount"] },
                { "client_url", inputs["ReturnUrl"] }
            };
            var str = $"{req["merchant_code"]}{req["ref_id"]}{req["player_username"]}{req["player_ip"]}{req["currency_code"]}{req["amount"]}{req["client_url"]}";
            req.Add("hash", GetSign(str, key));
            
            var resp = _httpHelper
                .WithOAuthBearerToken(key)
                .PostJson<Dictionary<string, string>, DepositQrResp>(inputs[$"{method}_url"], req);
            
            if (!resp.StatusCode.IsSuccessStatus())
                throw new CustomException($"Code={resp.StatusCode}");
            
            if (!Definition.SuccessCode.Equals(resp.Result.ErrorCode))
                throw new CustomException($"Code={resp.Result.ErrorCode}, Msg={resp.Result.Message}");

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = resp.Result.ThirdId,
                Url = resp.Result.RedirectTo
            };
        }
        
        private string GetSign(string str, string secretKey)
        {
            _logger.LogInformation("str: " + str);
            var sign = ToHMACSHA256(str, secretKey);

            return sign;
        }

        private bool VerifySign(string str, string secretKey, string target)
            => target.Equals(GetSign(str, secretKey), StringComparison.OrdinalIgnoreCase);
        
        private static string GetPayType(string payCode)
        {
            var result = payCode.Split('-');

            return result[0];
        }
        
        private static string GetPayMethod(string payCode)
        {
            var result = payCode.Split('-');

            return result.Length > 1 ? result[1] : string.Empty;
        }

        #endregion
    }
}