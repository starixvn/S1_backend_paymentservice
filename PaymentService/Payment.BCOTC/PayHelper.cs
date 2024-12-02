using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.BCOTC
{
    public class PayHelper : PayHelperBase
    {
        internal static string? SignConcatSourceString;
        private readonly ILogger<PayHelper> _logger;
        private readonly IFlurlHttpHelper _httpHelper;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["Host"]}{Definition.Path.Deposit}";

            try
            {
                var amount = decimal.Parse(inputs["Amount"]);
                var timeStamp = (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var payCode = int.Parse(inputs["PayCode"]);
                var req = new DepositPost
                {
                    MerchantId = inputs["MerchantId"],
                    MerchantOrderId = inputs["TransID"],
                    Amount = amount.ToString("F2"),
                    NotifyUrl = inputs["CallBackUrl"],
                    Payer = inputs["AccountName"],
                    PaymentMethod = payCode,
                    ApplyTimeStamp = timeStamp
                };

                req.Md5Sign = GetPostSign(req, inputs["SecretKey"]);
                var (result, statusCode) = _httpHelper
                    .PostJson<DepositPost, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"BCOTC deposit failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && result.Status != "1")
                {
                    throw new CustomException($"BCOTC deposit failed, Status: {result.Status}, Msg: {ConvertMsg(result.Msg)}");
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = result.PaymentUrl,
                    ThirdId = result.OrderId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"BCOTC deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var receive = FromJson<PaymentReceive>(bodyContent);
            var depositId = receive.MerchantOrderId;

            if (!VerifySign(receive, payResource["SecretKey"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = formData["merchantOrderId"],
                    TransStatus = TransStatus.Pending,
                    Response = $"TransId: {depositId}, Signature verification failed."
                };
            }

            var transStatus = Definition.GetDepositTransStatus(receive.Status);
            return new NotifyDepositResult
            {
                DepositId = depositId,
                ActualAmount = decimal.Parse(receive.Amount),
                ThirdId = formData["orderId"],
                Comment = receive.Status,
                TransStatus = transStatus,
                Response = CallbackResponse.Success
            };
        }
        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["Host"]}{Definition.Path.Withdrawal}";
            try
            {
                var amount = decimal.Parse(inputs["Amount"]);
                var timeStamp = (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var payCode = int.Parse(inputs["PayCode"]);
                var req = new WithdrawalPost
                {
                    MerchantId = inputs["MerchantId"],
                    MerchantOrderId = inputs["TransID"],
                    Amount = amount.ToString("F2"),
                    NotifyUrl = inputs["CallBackUrl"],
                    BankName = inputs["BankName"],
                    BankBranch = "Branch",
                    AccountName = inputs["AccountName"],
                    AccountNumber = inputs["AccountNo"],
                    PaymentMethod = payCode,
                    ApplyTimeStamp = timeStamp
                };

                req.Md5Sign = GetPostSign(req, inputs["SecretKey"]);
                var (result, statusCode) = _httpHelper
                    .PostJson<WithdrawalPost, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"BCOTC Withdrawal failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && result.Status != "1")
                {
                    throw new CustomException($"BCOTC Withdrawal failed, Status: {result.Status}, Msg: {ConvertMsg(result.Msg)}");
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"BCOTC Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var receive = FromJson<PaymentReceive>(bodyContent);
            var withdrawalId = receive.MerchantOrderId;

            if (!VerifySign(receive, payResource["SecretKey"]))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = withdrawalId,
                    TransStatus = TransStatus.Pending,
                    Response = $"TransId: {withdrawalId}, Signature verification failed."
                };
            }

            var transStatus = Definition.GetWithdrawalTransStatus(receive.Status);
            return new NotifyWithdrawResult
            {
                ThirdId = receive.OrderId,
                TransStatus = transStatus,
                Response = CallbackResponse.Success,
                WithdrawId = withdrawalId,
                Message = $"status={receive.Status}, amount={receive.Amount}, crypto_amount={receive.CryptoAmount}"
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

        internal string GetPostSign<T>(T model, string secretKey)
            where T: ISign
        {
            // only encrypt need api_key
            model.ApiKey = secretKey;
            SignConcatSourceString = ToJson(model);
            _logger.LogInformation($"SignSourceString: {SignConcatSourceString}");
            var sign = ToMD5(SignConcatSourceString).ToLower();

            // only encrypt need api_key
            model.ApiKey = null;
            return sign;
        }

        internal bool VerifySign(PaymentReceive receive, string secretKey)
        {
            var receiveSignModel = ConvertReceiveSignModel(receive);
            var md5Result = GetPostSign(receiveSignModel, secretKey);
            return receive.Md5Sign.Equals(md5Result, StringComparison.OrdinalIgnoreCase);
        }

        private static string? ConvertMsg(object message)
        {
            return message is string ? message.ToString() : JsonConvert.SerializeObject(message);
        }

        private static PaymentReceive ConvertReceiveSignModel(PaymentReceive model)
        {
            return new PaymentReceive
            {
                Amount = model.Amount,
                CompletedAt = model.CompletedAt,
                CryptoAmount = model.CryptoAmount,
                MerchantId = model.MerchantId,
                MerchantOrderId = model.MerchantOrderId,
                OrderId = model.OrderId,
                Status = model.Status
            };
        }
    }

}