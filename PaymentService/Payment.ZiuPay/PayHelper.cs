using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.ZiuPay
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

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var apiHost = input["Host"];
            var transId = input["TransID"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "store_id", input["StoreId"] },
                    { "api_token", input["Token"] },
                    { "user_id", input["CustId"] },
                    { "user_name", input["AccountName"] },
                    { "bank_name", input["BankName"] },
                    { "bank_number", input["PayAccount"] },
                    { "user_phone", input["PhoneNo"] },
                    { "amount", input["Amount"] },
                    { "order_no", transId }
                };
                

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, ZiuPayResp<DepositRespData>>($"{apiHost}{Path.Deposit}", req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException($"HttpStatus: {statusCode}", ((int)statusCode).ToString());
         

                if (!Definition.SuccessCode.Equals(response.Code))
                    throw new CustomException(response.Message, response.Code.ToString());

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = response.Data.RedirectUrl,
                    ThirdId = response.Data.ThirdId
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code={ex.Code}, Message={ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var data = FromJson<CallbackReq>(bodyContent);

            return new NotifyDepositResult
            {
                DepositId = data.TransId,
                ActualAmount = data.ActualAmount,
                ThirdId = data.TransId,
                Comment = data.Status,
                TransStatus = Definition.GetTransStatus(data.Status),
                Response = ToJson(new CallbackResp(true))
            };
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var apiHost = inputs["Host"];
            var transId = inputs["TransID"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "store_id", inputs["StoreId"] },
                    { "api_token", inputs["Token"] },
                    { "user_id", inputs["CustId"] },
                    { "user_name", inputs["AccountName"] },
                    { "bank_name", inputs["BankName"] },
                    { "bank_number", inputs["AccountNo"] },
                    { "user_phone", inputs["PhoneNo"] },
                    { "amount", inputs["Amount"] },
                    { "order_no", transId }
                };

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, ZiuPayResp<object>>($"{apiHost}{Path.Withdrawal}", req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException($"HttpStatus: {statusCode}", ((int)statusCode).ToString());
             
                if (!Definition.SuccessCode.Equals(response.Code))
                    throw new CustomException(response.Message, response.Code.ToString());

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = transId,
                    RawContent = JsonConvert.SerializeObject(response)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Withdrawal failed, Code={ex.Code}, Message={ex.Message}");

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
            var data = FromJson<CallbackReq>(bodyContent);
            
            return new NotifyWithdrawResult
            {
                ThirdId = data.TransId,
                TransStatus = Definition.GetTransStatus(data.Status),
                Response = ToJson(new CallbackResp(true)),
                WithdrawId = data.TransId
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallbackReq>(bodyContent);

            return data.TransId;
        }

        #endregion
    }
}
