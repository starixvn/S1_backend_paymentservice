using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.JDPay
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
            var apiHost = input["Host"];
            var reqAmount = decimal.Parse(input["Amount"]);

            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merNo", input["MerchantNo"]},
                    {"ordernum", input["TransID"]},
                    {"amount", reqAmount.ToString("G29")}
                };
                
                req.Add("sign", GetSign(req, input["Key"]));
                req.Add("bankCode", input["PayCode"]);
                req.Add("playerName", input["AccountName"]);
                req.Add("notifyurl", input["CallBackUrl"]);

                var res = CreateOrder(apiHost, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = input["DeviceType"].Equals("1") ? res.Url1 : res.Url2 // 1: PC
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"JDPay create deposit failed, msg={ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var transId = input["TransID"];
            var str = formData["merNo"] + formData["ordernum"] + formData["amount"] + input["Key"];

            if (!VerifySign(formData["sign"], str))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{transId} signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = transId,
                DepositId = transId,
                Comment = $"status={formData["status"]}, msg={formData["msg"]}",
                TransStatus = formData["status"].Equals("1") ? TransStatus.Success : TransStatus.Pending,
                ActualAmount = decimal.Parse(formData["amount"]),
                Response = Definition.CallbackResponse
            };

            return result;
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
            var reqAmount = decimal.Parse(inputs["Amount"]);
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    {"merNo", inputs["MerchantNo"]},
                    {"ordernum", inputs["TransID"]},
                    {"bankname", inputs["BankName"]},
                    {"amount", reqAmount.ToString("G29")}
                };
                
                req.Add("sign", GetSign(req, inputs["Key"]));
                req.Add("bankCode", inputs["BankCode"]);
                req.Add("bankcard", inputs["AccountNo"]);
                req.Add("name", inputs["AccountName"]);
                req.Add("notifyurl", inputs["CallBackUrl"]);
                
                var res = Payout(apiHost, req);
                
                var response = new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(res)
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"JDPay payout failed, msg={ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var transId = input["TransId"];
            var str = formData["merNo"] + formData["ordernum"] + formData["amount"] + input["Key"];


            if (!VerifySign(formData["sign"], str))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"{transId} signature verification failed."
                };
            }

            var result = new NotifyWithdrawResult
            {
                ThirdId = transId,
                TransStatus = Definition.GetTransStatus(formData["status"]),
                Response = Definition.CallbackResponse,
                WithdrawId = transId,
                Message = $"status={formData["status"]}"
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

        private CreatOrderResponse CreateOrder(string apiHost, Dictionary<string, string> req)
        {
            var apiUrl = apiHost + Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"HttpStatus=({(int)statusCode}){statusCode}");

            if (!Definition.SuccessStatus.Equals(result.Status))
                throw new CustomException(result.Message);

            return result;
        }

        private CreatPayoutResponse Payout(string apiHost, Dictionary<string, string> req)
        {
            var apiUrl = apiHost + Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .PostFormData<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
                throw new CustomException($"HttpStatus=({(int)statusCode}){statusCode}");

            if (!Definition.SuccessStatus.Equals(result.Status))
                throw new CustomException(result.Message);

            return result;
        }

        private string GetSign(IDictionary<string, string> param, string apiKey)
        {
            var str = string.Join(string.Empty, param
                .Select(x =>x.Value)) + apiKey;

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToLower();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, string str)
            => target.Equals(base.ToMD5(str), StringComparison.OrdinalIgnoreCase);
        
        #endregion
    }
}