using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.RarepidPay
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

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var url = GetUrl(inputs["Host"], Path.CreateDeposit);
            var authorization = GetAuthorization(inputs["Contract"], inputs["ApiKey"]);

            try
            {
                var depositRequest = new DepositPost
                {
                    HttpPostUrl = inputs["CallBackUrl"],
                    ReturnUrl = inputs["ReturnUrl"],
                    FailedReturnUrl = inputs["ReturnUrl"],
                    Depositer = inputs["CustId"],
                    Amount = int.Parse(inputs["Amount"]),
                    TranId = inputs["TransID"],
                    Remark = "service"
                };

                var orderResponse = CreateOrder(url, authorization, ToJson(depositRequest));
                var result = new DepositResult
                {
                    Success = true,
                    ErrorCode = orderResponse.Status,
                    ThirdId = orderResponse.PgTranId,
                    Message = orderResponse.StatusMessage,
                    Url = orderResponse.RedirectTo
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new DepositResult(false, exception.Code, exception.Message);
            }
        }

        private DepositResponse CreateOrder(string url, string authorization, string data)
        {
            var (response, statusCode) = _httpHelper
                .WithHeader("Authorization", authorization)
                .WithHeader("accept", MediaType.Json)
                .PostJson<string, DepositResponse>(url, data);

            if (response.Status != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"RarepidPay create order failed: Status={response.Status}, StatusMessage={response.StatusMessage}");
            }

            return response;
        }

        private string GetAuthorization(string contract, string apiKey)
        {
            return $"{contract} {apiKey}";
        }

        private string GetUrl(string baseUrl, string path)
        {
            return $"{baseUrl.TrimEnd('/')}{path}";
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var url = GetUrl(inputs["Host"], Path.CreateWithdrawal);
            var authorization = GetAuthorization(inputs["Contract"], inputs["ApiKey"]);

            try
            {
                var withdrawalRequest = new WithdrawalPost
                {
                    HttpPostUrl = inputs["CallBackUrl"],
                    Amount = int.Parse(inputs["Amount"]).ToString("F2"),
                    ToBankCode = inputs["BankCode"],
                    ToBankAccount = inputs["AccountNo"],
                    ToAccountName = inputs["AccountName"],
                    IfscCode = inputs["IFSCCode"],
                    TranId = inputs["TransID"],
                    Remark = "payout"
                };

                var withdrawalResponse = CreatePayout(url, authorization, ToJson(withdrawalRequest));
                var result = new WithdrawResult
                {
                    Success = true,
                    Message = withdrawalResponse.StatusMessage
                };

                return result;
            }
            catch (CustomException exception)
            {
                _logger.LogInformation(exception.Message);

                return new WithdrawResult(exception.Message);
            }
        }

        private WithdrawalResponse CreatePayout(string url, string authorization, string data)
        {
            var (response, statusCode) = _httpHelper
                .WithHeader("Authorization", authorization)
                .WithHeader("accept", MediaType.Json)
                .PostJson<string, WithdrawalResponse>(url, data);

            if (response.Status != Definition.SuccessCode)
            {
                throw new CustomException(
                    $"RarepidPay create payout failed: Status={response.Status}, StatusMessage={response.StatusMessage}");
            }

            return response;
        }

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

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var req = FromJson<DepositCallBackRequest>(bodyContent);
            var signConcatSourceString = $"{req.Token}{req.TranId}{req.FromBankCode}{req.Amount}";

            if (!VerifySign(req.Signature, signConcatSourceString, payResource["ApiKey"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = req.TranId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = req.TranId,
                DepositId = req.TranId,
                Comment = req.Status,
                TransStatus = req.Status.Equals("8") ? TransStatus.Success : TransStatus.Pending,
                ActualAmount = decimal.Parse(req.Amount),
                Response = Definition.CallbackResponse
            };

            return result;
        }

        private bool VerifySign(string sourceSignature, string signConcatSourceString, string apiKey)
        {
            var resultSignature = ToHMACSHA256(signConcatSourceString, apiKey);
            return sourceSignature.Equals(resultSignature, StringComparison.OrdinalIgnoreCase);
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }
    }
}