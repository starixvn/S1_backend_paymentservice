using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.MTPay;

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
            var uid = input["UID"];
            var merchantNo = input["MerchantNo"];
            var secretKey = input["SecretKey"];
            var payCode = input["PayCode"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = input["TransID"];
            var custId = input["CustId"];
            var notifyUrl = input["CallBackUrl"];
            var depositInfo = input["DepositInfo"];
            var bankCode = input["BankCode"];

            if (payCode == "1")
            {
                var di = JsonConvert.DeserializeObject<DepositInfo>(depositInfo);
                var getBankCode = GetBankCode(di.ChannelName);
                bankCode = getBankCode;
            }
            
            try
            {
                var req = new Dictionary<string, string>
                {
                    {"uid", uid},
                    {"merchant_num", merchantNo},
                    {"merchant_order", transId},
                    //金额，单位为分，金额为1元时，此处应填100。每个通道的金额限制请在 “通道管理->充值通道” 中查看
                    {"coin",strAmount + "00"},
                    {"pay_notifyurl",notifyUrl},
                    {"pay_date",DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss")},
                    {"pay_type",payCode},
                    {"userinfo",custId}
                };
                
                if (payCode == "1")
                {
                    var di = JsonConvert.DeserializeObject<DepositInfo>(depositInfo);
                    var getBankCode = GetBankCode(di.ChannelName);
                    bankCode = getBankCode;
                }
                
                // 銀行轉帳或掃碼bankCode不為空再帶銀行選項
                if ((payCode == "4" || payCode == "1") && !string.IsNullOrEmpty(bankCode))
                {
                    req.Add("bank_code", bankCode);
                }
                
                req.Add("sign", GetSign(req, secretKey));

                var res = CreateOrder(apiUrl, req);

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = res.Success.ToString(),
                    Url = res.Url,
                    DepositInfo = depositInfo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"MTPay Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
            Dictionary<string, string> headers, Dictionary<string, string> input)
        {
            var transId = input["TransID"]; 
            var secretKey = input["SecretKey"];
            
            var req = FromJson<CallBackRequest>(bodyContent);
            var dic = new Dictionary<string, string>
            {
                { "code", req.code.ToString() },
                { "merchant_num", req.merchantNum },
                { "coin", req.Amount.ToString() },
                { "merchant_order", req.TransId },
                { "serial_number", req.ThirdId },
                { "order_time", req.orderTime },
                { "success_time", req.successTime },
            };

            if (!VerifySign(req.Sign, dic,secretKey))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }

            var result = new NotifyDepositResult
            {
                ThirdId = req.ThirdId,
                DepositId = req.TransId,
                TransStatus = TransStatus.Success, // MTPay只回調成功的單
                ActualAmount = req.Amount / 100, //單位是分 除以 100就是 實際總金額
                Response = MTPayDefinition.CallbackResponse
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
            var response = new WithdrawResult
            {
                Success = false,
                RawContent = "System Error"
            };
            
            var apiUrl = inputs["Host"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var strAmount = reqAmount.ToString("G29");
            var transId = inputs["TransID"];
            var accountName = inputs["AccountName"];
            var accountNo = inputs["AccountNo"];
            var notifyUrl = inputs["CallBackUrl"];
            var bankCode = inputs["BankCode"];
            var uid = inputs["UID"];
            var merchantNo = inputs["MerchantNo"];
            var secretKey = inputs["SecretKey"];

            try
            {
                
                var req = new Dictionary<string, string>
                {
                    {"uid", uid},
                    {"merchant_num", merchantNo},
                    {"order", transId},
                    {"coin", strAmount + "00"}, // 代付的金额，单位为分
                    {"target_bank", accountNo},
                    {"bank_name", bankCode},
                    {"target_bank_user", accountName},
                    {"order_date", DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss")},
                    {"notifyurl", notifyUrl},
                };

                req.Add("sign", GetSign(req, secretKey));

                var res = Payout(apiUrl, req);
                
                if(res.code == 1)
                { 
                    response = new WithdrawResult
                    {
                        Success = true,
                        RawContent = JsonConvert.SerializeObject(res)
                    };
                }
                else
                {
                    response = new WithdrawResult
                    {
                        Success = false,
                        RawContent = JsonConvert.SerializeObject(res)
                    };
                }

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"MTPay Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

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
            var result = new NotifyWithdrawResult();
            var dic = new Dictionary<string, string>();
            var secretKey = payResource["SecretKey"];
            var req = FromJson<CallBackRequest>(bodyContent);

            if (req.code == 1)
            {
                dic = new Dictionary<string, string>
                {
                    { "code", req.code.ToString() },
                    { "merchant_num", req.merchantNum },
                    { "coin", req.Amount.ToString() },
                    { "merchant_order", req.TransId },
                    { "serial_number", req.ThirdId },
                    { "order_time", req.orderTime },
                    { "success_time", req.successTime },
                };
            }
            else
            {
                dic = new Dictionary<string, string>
                {
                    { "merchant_num", req.merchantNum },
                    { "coin", req.Amount.ToString() },
                    { "merchant_order", req.TransId },
                    { "serial_number", req.ThirdId },
                    { "order_time", req.orderTime },
                    { "success_time", req.successTime },
                };
            }

            if (!VerifySign(req.Sign, dic, secretKey))
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.TransId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature verification failed."
                };
            }


            if (req.code == 1)
            {
                result = new NotifyWithdrawResult
                {
                    ThirdId = req.ThirdId,
                    TransStatus = TransStatus.Success,
                    Response = MTPayDefinition.CallbackResponse,
                    WithdrawId = req.TransId,
                };
            }
            else
            {
                result = new NotifyWithdrawResult
                {
                    ThirdId = req.ThirdId,
                    TransStatus = TransStatus.Fail,
                    Response = MTPayDefinition.CallbackFailResponse,
                    WithdrawId = req.TransId,
                };
            }

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
            var apiUrl = baseApiUrl + MTPayDefinition.Path.CreateOrder;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatOrderResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"MTPay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Success)
                throw new CustomException("MTPay creat order failed Failed.");

            return result;
        }

        private CreatPayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + MTPayDefinition.Path.CreatePayout;
            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostUrlEncoded<Dictionary<string, string>, CreatPayoutResponse>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"MTPay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (result.code != 1)
                throw new CustomException("MTPay create payout failed.");

            return result;
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string secretKey)
        {
            var sortedParams = param.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}");
            var str = string.Join("&", sortedParams) + $"&key={secretKey}";

            _logger.LogInformation(str);
            var sign = base.ToMD5(str).ToUpper();
            _logger.LogInformation(sign);

            return sign;
        }

        private bool VerifySign(string target, IEnumerable<KeyValuePair<string, string>> param, string secretKey)
        {
            var sign = GetSign(param, secretKey);

            return target.Equals(sign, StringComparison.OrdinalIgnoreCase);
        }

        private string GetBankCode(string channelName)
        {
            return channelName switch
            {
                "ACB" => "1",
                "BIDV" => "8",
                "SAC" => "14",
                "TCB" => "6",
                "VCB" => "5",
                "VTB" => "9",
                "VIB" => "12",
                "SHB" => "15",
                "TP Bank" => "13",
                "VPB" => "11",
                "MB" => "7",
                _ => ""
            };
        }

        #endregion
}