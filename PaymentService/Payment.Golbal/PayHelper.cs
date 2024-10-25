using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using System.Net.NetworkInformation;

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
            var apiKey = inputs["APIKey"];
            var apiUrl = $"{inputs["APIUrl"]}{Definition.Deposit}";

            try
            {
                var p = new Dictionary<string, string>();

                p.Add("merchantUid", inputs["MerchantNo"]);
                p.Add("amount", inputs["Amount"]);
                p.Add("orderNo", inputs["TransID"]);
                p.Add("currency", inputs["PayCurrencyId"]);
                p.Add("channelId", inputs["PayCode"]);
                p.Add("notifyUrl", inputs["CallBackUrl"]);
                p.Add("time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
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
                    ErrorCode = result.Code,
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
            var transId = payResource["TransId"];
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

            if (req.Sign != GetSign(sign, payResource["APIKey"]))
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
            var apiKey = inputs["APIKey"];
            var apiUrl = $"{inputs["APIUrl"]}{Definition.Deposit}";

            try
            {
                var extra = new
                {
                    bank_code = inputs["bankcode"],
                    document = inputs["123"]
                };
                var p = new Dictionary<string, string>();

                p.Add("merchantUid", inputs["MerchantNo"]);
                p.Add("amount", inputs["Amount"]);
                p.Add("orderNo", inputs["TransID"]);
                p.Add("currency", inputs["PayCurrencyId"]);
                p.Add("channelId", inputs["PayCode"]);
                p.Add("accountName", inputs["AccountName"]);
                p.Add("accountNo", inputs["AccountNo"]);
                p.Add("extra", JsonConvert.SerializeObject(extra));
                p.Add("notifyUrl", inputs["CallBackUrl"]);
                p.Add("time", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
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

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(result)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Golbal deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult(ex.Message);
            }
        }
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            var transId = payResource["TransId"];
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

            if (req.Sign != GetSign(sign, payResource["APIKey"]))
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
