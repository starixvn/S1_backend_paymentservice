using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;



namespace Payment.Golbal
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
        #region debit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var currency = int.Parse(inputs["CurrencyId"]);
            var merchantNo = inputs[$"{currency}_MerchantNo"];
            var bankCode = inputs["BankCode"];
            var payCode = inputs["PayCode"];
            var apiKey = inputs[$"{currency}_APIKey"];
            var apiUrl = $"{inputs["APIUrl"]}{Definition.Deposit}";
            var extra = "";

            try
            {
                if (currency == (int)CurrencyEnum.INR)
                {
                    var extrapara = new
                    {
                        returnUrl = inputs["ReturnUrl"],
                        clientIp = inputs["ClientIP"],
                        payerName = inputs["AccountName"],
                        payerPhone = inputs["PhoneNo"],
                    };
                    extra = JsonConvert.SerializeObject(extrapara);
                }
                else if (currency == (int)CurrencyEnum.VND)
                {
                    //VND掃碼需拆分payCode拿參數
                    var bankName = "";
                    if (payCode.Contains('-'))
                    {
                        var s = payCode.Split('-');
                        payCode = s[0];
                        //代收掃碼由三方選擇，bankcode跟bankname用不到
                        bankCode = s[1];
                        bankName = s[2];
                    }
                    //onlinebanking
                    if (payCode == "1001")
                    {
                        bankName = inputs["BankName"];
                        var extrapara = new
                        {
                            bank_code = bankCode,
                            bank_name = bankName,
                        };
                        extra = JsonConvert.SerializeObject(extrapara);
                    }
                    
                }

                var p = new Dictionary<string, string>();

                p.Add("merchantUid", merchantNo);
                p.Add("amount", inputs["Amount"]);
                p.Add("orderNo", inputs["TransID"]);
                p.Add("currency", inputs["PayCurrencyId"]);
                p.Add("channelId", payCode);
                p.Add("notifyUrl", inputs["CallBackUrl"]);
                p.Add("time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                p.Add("extra", extra);
                p.Add("sign", GetSign(p, apiKey));

                var (result, statusCode) = _httpHelper
                    .WithHeader("Accept", "application/json")
                    .PostJson<Dictionary<string, string>, ApiResponse>(apiUrl, p, false);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Golbal deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }
                
                if (result != null && result.Code != "200")
                {
                    throw new CustomException(result.Msg);
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = result?.Code,
                    Url = result.Data.PayUrl,
                    Message = result.Msg,
                    ThirdId = result.Data.SysOrderNo,
                    DepositInfo = ""
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Golbal deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var transId = payResource["TransID"];
            var currency = payResource["CurrencyId"];
            var req = FromJson<DepositCallBackRequest>(bodyContent);

            var sign = new Dictionary<string, string>();
            sign.Add("merchantUid", req.MerchantUid);
            sign.Add("orderNo", req.OrderNo);
            sign.Add("sysOrderNo", req.SysOrderNo);
            sign.Add("amount", req.Amount);
            sign.Add("tax", req.Tax); 
            sign.Add("status", req.Status.ToString());
            sign.Add("createTime", req.CreateTime);
            sign.Add("payTime", req.PayTime);
            sign.Add("remark", req.Remark);
            sign.Add("extra", req.Extra);

            if (req.Sign != GetSign(sign, payResource[$"{currency}_APIKey"]))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = decimal.Parse(req.Amount),
                ThirdId = req.SysOrderNo,
                Comment = req.Status.ToString(),
                TransStatus = Definition.GetDepositTransStatus(req.Status),
                Response = ToJson(new { Code = "SUCCESS" }) 
            };
        }
        #endregion
        #region credit
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var currency = int.Parse(inputs["CurrencyId"]);
            var paycode = inputs["PayCode"];
            var merchantNo = inputs[$"{currency}_MerchantNo"];
            var apiKey = inputs[$"{currency}_APIKey"];
            var apiUrl = $"{inputs["APIUrl"]}{Definition.Withdrawal}";
            var extra = "";

            try
            {
                if (currency == (int)CurrencyEnum.BRL)
                {
                    var extrapara = new
                    {
                        bank_code = inputs["BankCode"],
                        document = inputs["AccountNo"]
                    };
                    extra = JsonConvert.SerializeObject(extrapara);
                }
                else if(currency == (int)CurrencyEnum.INR) 
                {
                    var extrapara = new
                    {
                        bank_code = inputs["BankCode"],
                        ifsc = inputs["IFSCCode"],
                        mobile = inputs["PhoneNo"],
                    };
                    extra = JsonConvert.SerializeObject(extrapara);
                }
                else if (currency == (int)CurrencyEnum.VND)
                {
                    //銀行
                    if (paycode == "5001")
                    {
                        var extrapara = new
                        {
                            bank_code = inputs["BankCode"],
                            bank_name = inputs["BankName"]
                        };
                        extra = JsonConvert.SerializeObject(extrapara);
                    }
                    
                }

                var p = new Dictionary<string, string>();

                p.Add("merchantUid", merchantNo);
                p.Add("amount", inputs["Amount"]);
                p.Add("orderNo", inputs["TransID"]);
                p.Add("currency", inputs["PayCurrencyId"]);
                p.Add("channelId", inputs["PayCode"]);
                p.Add("accountName", inputs["AccountName"]);
                p.Add("accountNo", inputs["AccountNo"]);
                p.Add("extra", extra);
                p.Add("notifyUrl", inputs["CallBackUrl"]);
                p.Add("time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                p.Add("sign", GetSign(p, apiKey));

                var (result, statusCode) = _httpHelper
                    .WithHeader("Accept", "application/json")
                    .PostJson<Dictionary<string, string>, ApiResponse>(apiUrl, p, false);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Golbal Withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }


                if (result != null && result.Code != "200")
                {
                    throw new CustomException(result.Msg);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Golbal Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var transId = payResource["TransId"];
            var currency = payResource["CurrencyId"];
            var req = FromJson<DepositCallBackRequest>(bodyContent);

            var sign = new Dictionary<string, string>();
            sign.Add("merchantUid", req.MerchantUid);
            sign.Add("amount", req.Amount);
            sign.Add("sysOrderNo", req.SysOrderNo);
            sign.Add("orderNo", req.OrderNo);
            sign.Add("createTime", req.CreateTime);
            sign.Add("payTime", req.PayTime);
            sign.Add("remark", req.Remark);
            sign.Add("status", req.Status.ToString());

            if (req.Sign != GetSign(sign, payResource[$"{currency}_APIKey"]))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = req.SysOrderNo,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            return new NotifyWithdrawResult
            {
                ThirdId = req.SysOrderNo,
                TransStatus = Definition.GetWithdrawalTransStatus(req.Status),
                Message = req.Remark,
                Response = ToJson(new { Code = "SUCCESS" }),
                WithdrawId = transId
            };
        }
        #endregion
        #region private
        private string GetSign(Dictionary<string, string> dic, string apikey)
        {
            var queryString = string.Join("&", dic
                .Where(x => !string.IsNullOrEmpty(x.Value)) // 過濾掉 null 或空字串的項目
                .OrderBy(x => x.Key, StringComparer.Ordinal) // 使用 ASCII 排序
                .Select(x => $"{x.Key}={x.Value}").ToArray());

            // 在最後加上 "key"
            queryString += $"&key={apikey}";
            return ToMD5(queryString);
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
    }
}
