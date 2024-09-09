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
                var depositRequest = new DepositRequest
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }
    }
}
