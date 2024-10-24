using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;


namespace Payment.XcPay
{
    public class PayHelper : PayHelperBase
    {
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
            var marchantNo = inputs["MerchantNo"];
            var apiKey = inputs["APIKey"];
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            var url = $"{inputs["APIUrl"]}{Definition.Path.deposit}";

            try
            {
                var p = new
                {
                    merchant_ref = inputs["TransID"],
                    product = Definition.QR,
                    amount = inputs["Amount"]
                };
                var jparam = ToJson(p);

                var toMD5Data = new Dictionary<string, string>();
                toMD5Data.Add("merchant_no", marchantNo);
                toMD5Data.Add("params", jparam);
                toMD5Data.Add("sign_type", Definition.CryptoMethod.MD5);
                toMD5Data.Add("timestamp", timestamp);
                toMD5Data.Add("Key", apiKey);

                var formData = new Dictionary<string, string>();
                formData.Add("merchant_no", marchantNo);
                formData.Add("timestamp", timestamp);
                formData.Add("sign_type", Definition.CryptoMethod.MD5);
                formData.Add("sign", GetSign(toMD5Data));
                formData.Add("params", jparam);

                var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                var (result, statusCode) = _httpHelper
                    .PostFormData<string, PaymentResponse>(url, sParam);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"XcPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }


                if (result != null && result.Code != 200)
                {
                    throw new CustomException(result.Message);
                }

                var deserializeParams = JsonConvert.DeserializeObject<PaymentParams>(result.Params);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = result.Code.ToString(),
                    Url = deserializeParams.Payurl,
                    Message = result.Message,
                    ThirdId = deserializeParams.SystemRef,
                    DepositInfo = ""
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Bajicash deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = payResource["TransID"];
            var apiKey = payResource["APIKey"];
            var param = formData["params"].Replace("\\", "");

            var MD5Data = new Dictionary<string, string>();
            MD5Data.Add("merchant_no", formData["merchant_no"]);
            MD5Data.Add("params", param);
            MD5Data.Add("sign_type", formData["sign_type"]);
            MD5Data.Add("timestamp", formData["timestamp"]);
            MD5Data.Add("Key", apiKey);

            var queryString = string.Join("", MD5Data.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString);

            if (formData["sign"] != sign.ToLower())
            {
                _logger.LogInformation($"{transId}, signature verification failed.");

                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = "Fail"
                };
            }
            var deserializeParams = JsonConvert.DeserializeObject<PaymentParams>(param);
            var result = new NotifyDepositResult
            {
                ActualAmount = decimal.Parse(deserializeParams.Amount),
                DepositId = transId,
                ThirdId = deserializeParams.SystemRef,
                TransStatus = Definition.GetTransStatus(deserializeParams.Status.ToString()),
                Response = "success"
            };

            return result;
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var marchantNo = inputs["MerchantNo"];
            var apiKey = inputs["APIKey"];
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            var url = $"{inputs["APIUrl"]}{Definition.Path.Withdrawal}";

            try
            {
                var p = new
                {
                    merchant_ref = inputs["TransID"],
                    product = Definition.Pauout,
                    amount = inputs["Amount"],
                    extra = new
                    {
                        account_name = inputs["AccountName"],
                        account_no = inputs["AccountNo"],
                        bank_code = inputs["IFSCCode"]
                    }
                };
                var jparam = ToJson(p);

                var toMD5Data = new Dictionary<string, string>();
                toMD5Data.Add("merchant_no", marchantNo);
                toMD5Data.Add("params", jparam);
                toMD5Data.Add("sign_type", Definition.CryptoMethod.MD5);
                toMD5Data.Add("timestamp", timestamp);
                toMD5Data.Add("Key", apiKey);

                var formData = new Dictionary<string, string>();
                formData.Add("merchant_no", marchantNo);
                formData.Add("timestamp", timestamp);
                formData.Add("sign_type", Definition.CryptoMethod.MD5);
                formData.Add("sign", GetSign(toMD5Data));
                formData.Add("params", jparam);

                var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                var (result, statusCode) = _httpHelper
                    .PostFormData<string, PaymentResponse>(url, sParam);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"XcPay Withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (result != null && result.Code != 200)
                {
                    throw new CustomException(result.Message);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"XcPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = payResource["TransId"];
            var apiKey = payResource["APIKey"];
            var param = formData["params"].Replace("\\", "");

            var MD5Data = new Dictionary<string, string>();
            MD5Data.Add("merchant_no", formData["merchant_no"]);
            MD5Data.Add("params", param);
            MD5Data.Add("sign_type", formData["sign_type"]);
            MD5Data.Add("timestamp", formData["timestamp"]);
            MD5Data.Add("Key", apiKey);

            var queryString = string.Join("", MD5Data.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString);

            if (formData["sign"] != sign.ToLower())
            {
                _logger.LogInformation($"{transId}, signature verification failed.");

                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }
            var deserializeParams = JsonConvert.DeserializeObject<PaymentParams>(param);
            var result = new NotifyWithdrawResult
            {
                ThirdId = deserializeParams.SystemRef,
                TransStatus = Definition.GetTransStatus(deserializeParams.Status.ToString()),
                Response = "success",
                WithdrawId = transId,
                Message = deserializeParams.Status.ToString()
            };

            return result;
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

        #region private
        private string GetSign(Dictionary<string, string> dic)
        {
            var queryString = string.Join("", dic.Select(x => x.Value).ToArray());
            var sign = ToMD5(queryString);
            return sign;
        }
        #endregion
    }
}