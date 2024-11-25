using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.DfPay
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
            throw new NotImplementedException();
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var url = $"{inputs["Host"]}{Definition.Path.Withdrawal}";
            try
            {
                var amount = decimal.Parse(inputs["Amount"]).ToString("F0");
                var req = new Dictionary<string, string>
                {
                    {"dfusername", inputs["DfUserName"]},
                    {"money", amount},
                    {"orderno", inputs["TransID"]},
                    {"skcardno", inputs["AccountNo"]},
                    {"skcardname", inputs["AccountName"]},
                    {"skbankid", inputs["PayCode"]},
                    {"hdurli", inputs["CallBackUrl"]},
                };

                req.Add("sign", GetSign(req, inputs["Token"]));
                var (result, statusCode) = _httpHelper
                    .PostUrlEncoded<Dictionary<string, string>, PaymentResponse>(url, req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"DfPay Withdrawal failed, HttpStatus: {(int)statusCode} {result}");
                }

                if (result != null && result.Systate != "0")
                {
                    throw new CustomException($"DfPay Withdrawal failed, systate: {result.Systate}, msg: {result.msg}");
                }

                return new WithdrawResult
                {
                    Success = true,
                    Message = $"Withdrawal request success! ([Response Info] systate={result.Systate})",
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"DfPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var key = payResource["Token"];
            var receiveModel = FromJson<ReceiveModel>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                { "orderno", receiveModel.OrderNo },
                { "orderstate", receiveModel.OrderState },
                { "truedfmoney", receiveModel.TrueDfMoney },
                { "endtime", receiveModel.EndTime }
            };

            var transId = payResource["TransId"];
            if (!VerifySign(dic, key, receiveModel.Sign))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = $"TransId: {transId}, Signature verification failed."
                };
            }

            return new NotifyWithdrawResult
            {
                ThirdId = receiveModel.OrderNo,
                TransStatus = Definition.GetTransStatus(receiveModel.OrderState),
                Response = CallbackResponse.Success,
                WithdrawId = transId,
                Message = $"Receive content - orderstate: {receiveModel.OrderState}"
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

        internal string GetSign(Dictionary<string, string> dic, string token)
        {
            var str = string.Join("", dic.Select(x => x.Value).ToArray());
            SignConcatSourceString = $"{str}{{{token}}}";
            _logger.LogInformation($"SignSourceString: {SignConcatSourceString}");
            var sign = ToMD5(SignConcatSourceString).ToLower();
            return sign;
        }

        private bool VerifySign(Dictionary<string, string> dic, string token, string receiveSign)
        {
            return receiveSign.Equals(GetSign(dic, token), StringComparison.OrdinalIgnoreCase);
        }
    }

}