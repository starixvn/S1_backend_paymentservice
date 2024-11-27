using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using Newtonsoft.Json;
using Flurl;
using PaymentCore.Extensions;

namespace Payment.K2Pay
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
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(ToJson(formData));
            var parameter = $"amount={data.amount.ToString("#")}completeTime={data.completeTime}order={data.order}remark={data.remark}status={data.status}secret={payResource["key"]}";
            var sign = ToMD5(parameter).ToLower();

            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = data.order,
            };

            if (sign == data.sn)
            {
                switch (data.status)
                {
                    case (int)WithdrawalCallbackStatus.Completed:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.RequestAmount = data.amount;
                        oNotify.Response = "success";
                        break;
                    case (int)WithdrawalCallbackStatus.Rejected:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Response = "success";
                        break;
                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Response = "fail";
                        break;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "fail";
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var key = payResource["transfer_key"];
            var depositInfo = payResource["DepositInfo"];
            var di = JsonConvert.DeserializeObject<DepositInfo>(depositInfo);

            if (di == null || string.IsNullOrEmpty(di.BankCode))
            {
                key = payResource["scan_Key"];
            }

            var p = data.sn;
            var sign = GetCallbackSignature(data.amount.ToString("#"), data.gid, data.order, data.transaction_id, data.uid, key);

            oNotify.DepositId = data.order;
            oNotify.ThirdId = data.transaction_id;

            if (sign == p)
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = data.amount;
                oNotify.ActualAmount = data.amount;
                oNotify.Response = "success";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "fail";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            try
            {
                var bankMark = string.IsNullOrWhiteSpace(inputs["PayCode"]) ? inputs["BankCode"] : inputs["PayCode"];
                var request = new WithdrawalReq
                {
                    appid = Convert.ToInt32(inputs["appid"]),
                    orderId = inputs["TransID"],
                    name = inputs["AccountName"],
                    money = Convert.ToDecimal(inputs["Amount"]).ToString("#"),
                    bankMark = bankMark,
                    recAcc = inputs["AccountNo"],
                    notifyUrl = UrlEncode(inputs["CallBackUrl"])
                };
                var parameter = $"appid={request.appid}bankMark={request.bankMark}money={request.money}name={request.name}orderId={request.orderId}recAcc={request.recAcc}secret={inputs["key"]}";
                request.sn = ToMD5(parameter).ToLower();

                var sUrl = inputs["withdrawalhost"] + inputs["withdraw"];
                var dicP = FromJson<Dictionary<string, string>>(ToJson(request));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var (result, statusCode) = _httpHelper.Get<WithdrawalRes>(sUrl, sParam);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"K2Pay create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                var withdrawResult = new WithdrawResult();

                if (result.code == (int)WithdrawalStatus.Success)
                {
                    withdrawResult.Success = true;
                    withdrawResult.Message = result.desc;
                    withdrawResult.RawContent = result.data.ToString();
                }
                else
                {
                    withdrawResult.Success = false;
                    withdrawResult.Message = result.desc;
                    withdrawResult.RawContent = result.data.ToString();
                }

                return withdrawResult;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation(ex.Message);

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            try
            {
                var appid = Convert.ToInt32(inputs["transfer_appid"]);
                var key = inputs["transfer_key"];
                var payType = inputs["BankCode"];
                if (!string.IsNullOrEmpty(inputs["PayCode"]))
                {
                    appid = Convert.ToInt32(inputs["scan_appid"]);
                    key = inputs["scan_Key"];
                    payType = inputs["PayCode"];
                }

                var depositReq = new DepositReq
                {
                    appid = appid,
                    uid = inputs["CustId"],
                    order = inputs["TransID"],
                    price = Convert.ToDecimal(inputs["Amount"]).ToString("#"),
                    payType = payType,
                    notifyUrl = UrlEncode(inputs["CallBackUrl"])
                };

                depositReq.sn = GetSignature(depositReq.appid, depositReq.order,
                depositReq.payType, depositReq.price, depositReq.uid, key);

                var sUrl = inputs["host"] + inputs["deposit"];
                var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var (result, statusCode) = _httpHelper.Get<DepositRes>(sUrl, sParam);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"K2Pay creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                var depositResult = new DepositResult();
                if (result.e == (int)DepositStatus.Success)
                {

                    var responseData = new Dictionary<string, string>
                    {
                        { "DepositAmount", depositReq.price },
                        { "BankName", result.d.bankType },
                        { "AccountName", result.d.accountName },
                        { "AccountNumber", result.d.account }
                    };
                    depositResult.Url = result.d.h5;
                    depositResult.FormData = responseData;
                    depositResult.Success = true;
                    depositResult.Message = "";
                }
                else
                {
                    depositResult.Success = false;
                    depositResult.Message = result.m;
                }

                return depositResult;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation(ex.Message);

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        private string GetSignature(int appid, string order, string payType, string price, string uid, string key)
        {
            var type = string.Empty;
            if (!string.IsNullOrEmpty(payType))
            {
                type = $"payType={payType}";
            }
            var parameter = $"appid={appid}order={order}{type}price={price}uid={uid}secret={key}";

            return ToMD5(parameter).ToLower();
        }
        private string GetCallbackSignature(string amount, string gid, string order, string transactionId, string uid,
            string key)
        {

            var parameter = $"amount={amount}gid={gid}order={order}transaction_id={transactionId}uid={uid}secret={key}";
            return ToMD5(parameter).ToLower();
        }
    }
}
