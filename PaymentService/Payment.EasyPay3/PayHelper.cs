using Flurl.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;
using static Payment.EasyPay3.Definition;

namespace Payment.EasyPay3
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
            var apiUrl = input["Host"];
            var apiToken = input["Apikey"];
            var signToken = input["SignKey"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var notifyUrl = input["CallBackUrl"];
            var payCode = input["PayCode"];
            var depositInfo = input["DepositInfo"];
            var bankCode = input["BankCode"];
            try
            {
                var req = new Dictionary<string, string>
                {
                    {"amount", strAmount},
                    {"chargeType", payCode},
                    {"requestId", transId}
                };
                req.Add("sign", GetSign(req, signToken));
                req.Add("Apikey", apiToken);
                req.Add("callback", notifyUrl);

                if (payCode == "bank")
                    req.Add("subType", "");

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = res.Data.RedirectSsl,
                    ThirdId = res.Data.Id.ToString(),
                    DepositInfo = depositInfo
                };

                return response;
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
            var depositInfo = input["DepositInfo"];
            var apiSecret = input["ApiSecret"];
            var transId = input["TransID"];
            var thirdId = formData["chargeId"];
            var formStatus = formData["status"];
            var actualAmount = formData["chargeAmount"];

            var signature = formData["signature"];
            var dic = new Dictionary<string, string>
            {
                {"chargeId",thirdId},
                {"chargeType", formData["chargeType"]},
                {"chargeCode", formData["chargeCode"]},
                {"chargeAmount",actualAmount},
                {"status",  formStatus },
                {"transId", transId }
            };

            if (!VerifySign(signature, dic, apiSecret))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetTransStatus(formStatus);
            var result = new NotifyDepositResult
            {
                ThirdId = thirdId,
                DepositId = transId,
                Comment = formData["result"],
                TransStatus = transStatus,
                ActualAmount = decimal.Parse(actualAmount),
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
            var apiUrl = inputs["Host"];
            var apiToken = inputs["Apikey"];
            var apiSecret = inputs["ApiSecret"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var withdrawalType = Int32.Parse(inputs["WithdrawalType"]);

            try
            {

                var req = new Dictionary<string, string>() {
                    {"account", accountNo},
                    {"amount", strAmount},
                    {"requestId", transId}
                };

                req.Add("signature", GetSign(req, apiSecret));

                if (withdrawalType == (int)WithdrawalType.OnlineBanking)
                {
                    req.Add("bank_account", accountNo);
                    req.Add("bank_code", bankCode);
                    req.Add("bank_accountName", accountName);

                    req.Remove("account");
                }

                req.Add("apiKey", apiToken);
                req.Add("callback", notifyUrl);
                req.Add("account_name", accountName);

                var res = Payout(apiUrl, withdrawalType, req);
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
            var apiSecret = payResource["ApiSecret"];
            var transId = payResource["TransId"];
            var thirdId = formData["chargeId"];
            var formStatus = formData["status"];
            var ActualAmount = formData["chargeAmount"];

            var signature = formData["signature"];
            var dic = new Dictionary<string, string>
            {
                {"chargeId",thirdId},
                {"chargeType", formData["chargeType"]},
                {"chargeCode", formData["chargeCode"]},
                {"chargeAmount",ActualAmount},
                {"status",  formStatus },
                {"transId", transId }
            };

            if (!VerifySign(signature, dic, apiSecret))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var transStatus = Definition.GetTransStatus(formStatus);
            var result = new NotifyWithdrawResult
            {
                ThirdId = thirdId,
                TransStatus = transStatus,
                Response = Definition.CallbackResponse,
                WithdrawId = transId,
                Message = formData["result"]
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

        private CreatOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Definition.Path.CreateOrder;
            var parameters = string.Join("&", req.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}"));
            var (result, statusCode) = _httpHelper.Get<CreatOrderResponse>(apiUrl, parameters);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"EasyPay3 creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }
            if (!ErrorCode.Success.Equals(result.Status))
            {
                throw new CustomException(result.Msg, $"Status={result.Status}:{StatusMsgMap[result.Status]}/" +
                    $"ExtraStatus={result.ExtraStatus}:{ExtraStatusMsgMap[result.ExtraStatus]}");
            }

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, int withdrawalType, Dictionary<string, string> req)
        {
            var apiUrl = string.Empty;
            if (withdrawalType == (int)WithdrawalType.OnlineBanking)
                apiUrl = baseApiUrl + Definition.Path.CreateOnlineBankingPayout;
            else
                apiUrl = baseApiUrl + Definition.Path.CreateEWalletPayout;

            var parameters = string.Join("&", req.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}"));
            var (result, statusCode) = _httpHelper.Get<CreatPayoutResponse>(apiUrl, parameters);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"EasyPay3 create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!ErrorCode.Success.Equals(result.Status))
            {
                if (result.Data is not null)
                {
                    throw new CustomException(result.Msg, $"Status={result.Status}:{StatusMsgMap[result.Status]}/" +
                        $"ExtraStatus={result.ExtraStatus}:{ExtraStatusMsgMap[result.ExtraStatus]}/" +
                        $"DataStatus={result.Data.Status}:{WithdrawalDataStatusMap[result.Data.Status]}");
                }
                else
                {
                    throw new CustomException(result.Msg, $"Status={result.Status}:{StatusMsgMap[result.Status]}/" +
                        $"ExtraStatus={result.ExtraStatus}:{ExtraStatusMsgMap[result.ExtraStatus]}");
                }
            }

            return result;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string apiToken)
        {
            var sign = GetSign(param, apiToken);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiToken)
        {
            var str = string.Join("", param.Select(x => x.Value)) + apiToken;

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToLower();
            _logger.LogInformation(sign);

            return sign;
        }

        #endregion
    }
}