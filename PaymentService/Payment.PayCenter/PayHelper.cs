using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using System.Net.NetworkInformation;

namespace Payment.PayCenter
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
            var apiUrl = inputs["apiUrl"];
            var token = inputs["apiToken"];
            var notifyUrl = inputs["CallBackUrl"];
            var payCode = int.Parse(inputs["PayCode"]);
            var accountName = inputs["AccountName"];
            var amount = int.Parse(inputs["Amount"]);
            try
            {
                var req = new PayCenterRequest
                {
                    Direction = (int)Direction.Deposit,
                    Player = accountName,
                    PlayerWalletNum = "",
                    Amount = amount,
                    NotifyURL = notifyUrl,
                    VendorTranId = inputs["TransID"],
                    WalletNameID = payCode
                };

                var (result, statusCode) = _httpHelper
                    .WithHeader("Api-Token", token)
                    .PostJson<PayCenterRequest, PayCenterResponse>(apiUrl, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"PayCenter deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                
                if (result != null && result.ErrCode != 0)
                {
                    throw new CustomException(result.ErrText);
                }

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = result.ErrCode.ToString(),
                    Url = result.Result.PaymentURL,
                    Message = result.ErrText,
                    ThirdId = result.Result.PaymentID.ToString(),
                    DepositInfo = ""
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
            var amount = formData["amount"];
            var convertedAmount = decimal.Parse(amount);
            var status = formData["approved"];
            var apiKey = payResource["notifySecret"];

            var req = new Dictionary<string, string>
            {
                { "TransId",formData["tranId"]},
                { "Amount",amount},
                { "Player",formData["player"]},
                { "Approved",status},
            };

            if (!VerifySign(req, apiKey, formData["sign"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = GetTransStatus(status);

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = convertedAmount,
                ThirdId = transId,
                Comment = status,
                TransStatus = transStatus,
                Response = "SUCCESS"
            };
        }
        #endregion

        #region Credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiUrl = inputs["apiUrl"];
            var token = inputs["apiToken"];
            var notifyUrl = inputs["CallBackUrl"];
            var payCode = int.Parse(inputs["BankCode"]);
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var amount = int.Parse(inputs["Amount"]);

            try
            {
                var req = new PayCenterRequest
                {
                    Direction = (int)Direction.Withdraw,
                    Player = accountName,
                    PlayerWalletNum = accountNo,
                    Amount = amount,
                    NotifyURL = notifyUrl,
                    VendorTranId = inputs["TransID"],
                    WalletNameID = payCode
                };

                var (result, statusCode) = _httpHelper
                    .WithHeader("Api-Token", token)
                    .PostJson<PayCenterRequest, PayCenterResponse>(apiUrl, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"PayCenter withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (result != null && result.ErrCode != 0)
                {
                    throw new CustomException(result.ErrText);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Withdraw failed, Code:{ex.Code}, Message:{ex.Message}");

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
            var amount = formData["amount"];
            var convertedAmount = decimal.Parse(amount);
            var status = formData["approved"];
            var apiKey = payResource["notifySecret"];

            var req = new Dictionary<string, string>
            {
                { "TransId",formData["tranId"]},
                { "Amount",amount},
                { "Player",formData["player"]},
                { "Approved",status},
            };

            if (!VerifySign(req, apiKey, formData["sign"]))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = GetTransStatus(status);

            return new NotifyWithdrawResult
            {
                ThirdId = "",
                TransStatus = transStatus,
                Response = "SUCCESS",
                WithdrawId = transId
            };
        }
        #endregion


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

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var str = string.Join("", param
                .Select(x => x.Value)) + apiKey;

            var sign = ToMD5(str);

            return sign.ToLower();
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }

        private static Dictionary<string, TransStatus> PayCenterStatusMap =>
            new()
            {
                { PayCenterStatus.Approved, TransStatus.Success },
                { PayCenterStatus.Rejected, TransStatus.Fail },
            };

        public static TransStatus GetTransStatus(string status)
            => PayCenterStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;

        private class PayCenterStatus
        {
            public const string Rejected = "0";
            public const string Approved = "1";
        }


    }
}
