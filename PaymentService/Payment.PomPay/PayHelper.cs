using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PomPay
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
            try
            {
                var sUrl = input["host"];
                var amount = decimal.Parse(input["Amount"]);
                var formData = new Dictionary<string, string>()
                {
                    { "clientId", input["ClientId"] },
                    { "transactionId", input["TransID"] },
                    { "custName", input["AccountName"] },
                    { "amount", amount.ToString("#") },
                    { "returnUrl", input["ReturnUrl"] },
                    { "callbackUrl", input["CallBackUrl"] },
                    { "paymentMethod", input["PayCode"] }
                };
                formData.Add("hashVal", Hash(formData, input["ClientSecret"]));
            
                var oDepositResult = new DepositResult
                {
                    Url = sUrl,
                    FormData = formData,
                    Success = true,
                    Message = ""
                };
                return oDepositResult;
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
            var oNotify = new NotifyDepositResult();
            var status = formData["status"];
            var dict = new Dictionary<string, string>()
            {
                { "clientId", input["ClientId"] }
            };
            
            if (status == "success")
            {
                dict.Add("referenceId", formData["referenceId"]);
                dict.Add("transactionId", formData["transactionId"]);
                dict.Add("custName", formData["custName"]);
                dict.Add("amount", formData["amount"]);
                dict.Add("status", formData["status"]);
            }
            else
            {
                dict.Add("transactionId", formData["transactionId"]);
                dict.Add("status", formData["status"]);
            }
            
            var hashVal = formData["hashVal"];
            var actual = Hash(dict, input["clientSecret"]);
            if (string.Equals(hashVal, actual))
            {
                
                oNotify.DepositId = formData["transactionId"];
                oNotify.Message = status;
                oNotify.Comment = status;
                if (status == "success")
                {
                    var amount = decimal.Parse(formData["amount"]);
                    oNotify.ThirdId = formData["referenceId"];
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = amount;
                    oNotify.ActualAmount = amount;
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
            }

            return oNotify;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["Host"];
            var clientSecret = inputs["ClientSecret"];
            var clientId = inputs["ClientId"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];

            try
            {
              
                var req = new Dictionary<string, string>
                {
                    {"clientId", clientId},
                    {"transactionId", transId},
                    {"custName", accountName},
                    {"custBank", bankCode},
                    {"custBankAcc", accountNo},
                    {"amount", strAmount},
                    {"callbackUrl", notifyUrl}
                };

                req.Add("hashVal", Hash(req, clientSecret));

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
            var clientId = payResource["ClientId"];
            var clientSecret = payResource["ClientSecret"];
            var req = FromJson<PayoutCallBackResponse>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                {"clientId", clientId},
                {"transactionId", req.TransId},
                {"referenceId", req.ReferenceId},
                {"custName", req.AccountName},
                {"custBank", req.BankCode},
                {"custBankAcc", req.AccountNo},
                {"amount", req.Amount},
                {"status", req.Status}
            };

            if (!VerifySign(req.Sign, dic, clientSecret))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = PomPayDefinition.GetTransStatus(req.Status);
            var result = new NotifyWithdrawResult
            {
                ThirdId = req.ReferenceId,
                TransStatus = transStatus,
                Response = PomPayDefinition.CallbackResponse,
                WithdrawId = req.TransId,
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

        private PayoutCallBackResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + PomPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, PayoutCallBackResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"PomPay create payout failed, HttpStatus: {(int) statusCode} {statusCode}");
            }

            if (result.Status != "success")
                throw new CustomException("PomPay create payout failed.");

            return result;
        }
        
        private bool VerifySign(string target, Dictionary<string,string> param, string clientSecret)
        {
            var sign = Hash(param, clientSecret);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }
        
        private string Hash(Dictionary<string, string> para, string key)
        {
            var queryString = string.Join("", para.Select(p => p.Value).ToArray());
            _logger.LogInformation(queryString);
            var sign = ToSHA256($"{queryString}{key}").ToLower();
            _logger.LogInformation(sign);
            return sign;
        }

        #endregion
    }
}