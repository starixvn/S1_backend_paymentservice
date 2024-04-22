using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using static Payment.VTPay.VTPayModel;
using static Payment.VTPay.VTPayDefinition;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using Newtonsoft.Json;
using System.Text.RegularExpressions;


namespace Payment.VTPay
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
            var baseUrl = inputs["host"];
            var payCode = inputs["PayCode"];
            var currency = inputs["PayCurrencyId"];
            var transId = inputs["TransID"];
            var returnUrl = inputs["ReturnUrl"];
            var callbackUrl = inputs["CallBackUrl"];
            var amount = decimal.Parse(inputs["Amount"]);
            var dateTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));//+7時區
            var cleanedDate = dateTime.ToString("yyyyMMddHHmmss");
            var IPAddress = inputs["ClientIP"];
            var bankCode = "";
            var merchantId = inputs[$"MerchantIDFor{payCode}"];
            var passKey = inputs[$"PassKeyFor{payCode}"];
            var paramOptional = "";

            try
            {
                DepositResponse response;
                HttpStatusCode statusCode;

                switch (payCode)
                {
                    case "QRIS":
                        bankCode = payCode;
                        break;
                    case "P2P":
                        bankCode = inputs["BankCode"];
                        break;
                    case "VA":
                        bankCode = inputs["BankCode"];
                        paramOptional = $"FirstName={inputs["AccountName"]};";
                        break;
                }

                var sign = new Dictionary<string, string>
                    {
                        { "MerchantID",merchantId },
                        { "Amount",amount.ToString("F4")},
                        { "BankCode",bankCode},
                        { "DateRequest",cleanedDate}
                    };
                var signature = GetSign(sign, passKey);

                var reqData = new DepositRequestData
                {
                    MerchantID = merchantId,
                    Currency = currency,
                    BankCode = bankCode,
                    MerchantTransactionID = transId,
                    ReturnUrl = returnUrl,
                    RequestUrl = callbackUrl,
                    Language = "en-US",
                    Amount = decimal.Parse(amount.ToString("F4")),
                    DateRequest = dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    IPAddress = IPAddress,
                    ParamOptional = paramOptional,
                    Signature = signature,
                };

                (response, statusCode) = _httpHelper
                    .PostJson<DepositRequestData, DepositResponse>($"{baseUrl}{VTPayDefinition.Path.Deposit}", reqData);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"VTPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (response.ResultCode != ErrorCode.Success)
                {
                    throw new CustomException(response.ErrorMessage);
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success.ToString(),
                    Url = response.Content.URL,
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);
            var depositInfo = FromJson<Dictionary<string, string>>(payResource["DepositInfo"]);
            var payCode = depositInfo["PayCode"];
            var merchantID = dicReq["MerchantID"];
            var authenticateCode = dicReq["AuthenticateCode"];
            var passKey = payResource[$"PassKeyFor{payCode}"];
            var transId = dicReq["MerchantTransactionID"];
            var amount = decimal.Parse(dicReq["Amount"]);
            var sign = dicReq["Signature"];
            var status = dicReq["Status"];
            var currency = dicReq["Currency"];

            Dictionary<string, string> req;

            req = new Dictionary<string, string>
            {
                { "MerchantID",merchantID },
                { "MerchantTransactionID",transId},
                { "Amount",amount.ToString("F4") },
                { "Status",status},
                { "Currency",currency},
            };


            if (!VerifySign(req, passKey, sign))
            {
                var depositResponse = new DepositResponse
                {
                    ResultCode = 0,
                    ErrorMessage = "Signature does not match.",
                    Content = new Content
                    {
                        Status = "failed",
                        MerchantTransactionID = transId,
                        AuthenticateCode = authenticateCode
                    }
                };
                string json = ToJson(depositResponse);

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = json
                };
            }

            var transStatus = GetTransStatus(status);

            #region VTPay規範response
            var response = new DepositResponse
            {
                ResultCode = 1,
                ErrorMessage = "",
                Content = new Content
                {
                    Status = "success",
                    MerchantTransactionID = transId,
                    AuthenticateCode = authenticateCode
                }
            };
            string responseString = ToJson(response);
            #endregion

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = amount,
                ThirdId = dicReq["PGTransactionID"],
                Comment = status,
                TransStatus = transStatus,
                Response = responseString
            };
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
            var baseUrl = inputs["host"];
            var currency = inputs["PayCurrencyId"];
            var transId = inputs["TransID"];
            var callbackUrl = inputs["CallBackUrl"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var amount = decimal.Parse(inputs["Amount"]);
            var dateTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));//+7時區
            var cleanedDate = dateTime.ToString("yyyyMMddHHmmss");
            var IPAddress = inputs["ClientIP"];
            var bankCode = inputs["BankCode"];
            var merchantId = inputs["MerchantIDForP2P"];
            var passKey = inputs["PassKeyForP2P"];

            try
            {
                var sign = new Dictionary<string, string>
                {
                    { "MerchantID",merchantId},
                    { "Amount",amount.ToString("F4")},
                    { "BankCode",bankCode},
                    { "DateRequest",cleanedDate},
                };
                var signature = GetSign(sign, passKey);

                var reqData = new WithdrawalRequestData
                {
                    MerchantID = merchantId,
                    Amount = decimal.Parse(amount.ToString("F4")),
                    MerchantTransactionID = transId,
                    RequestURL = callbackUrl,
                    DateRequest = dateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Language = "en-US",
                    IPAddress = IPAddress,
                    Currency = currency,
                    BankCode = bankCode,
                    BankAccountName = accountName,
                    BankAccountNo = accountNo,
                    BankBranch = "BankBranch",
                    BankCity = "BankCity",
                    BankProvince = "BankProvince",
                    ParamOptional = "",
                    Signature = signature,
                };
                

                var (response, statusCode) = _httpHelper
                    .PostJson<WithdrawalRequestData, WithdrawalResponse>($"{baseUrl}{VTPayDefinition.Path.Withdrawal}", reqData);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"VTpay withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.ResultCode))
                {
                    throw new CustomException(response.ErrorMessage, response.ResultCode.ToString());
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(response)
                };
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
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);
            var transID = dicReq["MerchantTransactionID"];
            var apiKey = payResource["PassKeyForP2P"];
            var req = new Dictionary<string, string>
            {
                { "MerchantID",dicReq["MerchantID"]},
                { "MerchantTransactionID",transID},
                { "Amount",dicReq["Amount"]},
                { "Status",dicReq["Status"]},
                { "Currency",dicReq["Currency"]},
            };

            if (!VerifySign(req, apiKey, dicReq["Signature"]))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = dicReq["PGTransactionID"],
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = VTPayDefinition.GetTransStatus(dicReq["Status"]);

            var response = new DepositResponse
            {
                ResultCode = 1,
                ErrorMessage = "",
                Content = new Content
                {
                    Status = "success",
                    MerchantTransactionID = transID,
                    AuthenticateCode = dicReq["PGTransactionID"]
                }
            };
            string responseString = ToJson(response);

            return new NotifyWithdrawResult
            {
                ThirdId = dicReq["PGTransactionID"],
                TransStatus = transStatus,
                Message = dicReq["Status"],
                Response = responseString,
                WithdrawId = transID
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region private

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var str = string.Join("", param
                .Select(x => x.Value));

            var sign = ToSHA512(str + apiKey);

            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }
        #endregion
    }
}
