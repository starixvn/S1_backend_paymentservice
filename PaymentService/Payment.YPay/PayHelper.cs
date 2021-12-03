using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.YPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private const string CliNa = "PC";

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
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

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var key = payResource["transfer_key"];
            if (string.IsNullOrEmpty(payResource["BankCode"]))
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
            var request = new WithdrawalReq
            {
                appid = Convert.ToInt32(inputs["appid"]),
                orderId = inputs["TransID"],
                name = inputs["AccountName"],
                money = Convert.ToDecimal(inputs["Amount"]).ToString("#"),
                bankMark = inputs["BankCode"],
                recAcc = inputs["AccountNo"],
                notifyUrl = UrlEncode(inputs["CallBackUrl"])
            };
            var parameter = $"appid={request.appid}bankMark={request.bankMark}money={request.money}name={request.name}orderId={request.orderId}recAcc={request.recAcc}secret={inputs["key"]}";
            request.sn = ToMD5(parameter).ToLower();
            
            var sUrl = inputs["withdrawalhost"] + inputs["withdraw"];
            var dicP = FromJson<Dictionary<string, string>>(ToJson(request));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpGet(sUrl, sParam, inputs["TransID"], true);
            
            var withdrawResult = new WithdrawResult();
            var withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);
            if (withdrawalRes.code == (int)WithdrawalStatus.Success)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.desc;
                withdrawResult.RawContent = withdrawalRes.data.ToString();
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.desc;
                withdrawResult.RawContent = withdrawalRes.data.ToString();
            }

            return withdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var appid = Convert.ToInt32(inputs["transfer_appid"]);
            var key = inputs["transfer_key"];
            var payType = inputs["BankCode"];
            if (inputs["PayCode"] != null)
            {
                appid = Convert.ToInt32(inputs["scan_appid"]);
                key = inputs["scan_Key"];
                payType = inputs["PayCode"];
            }

            var depositReq = new DepositReq
            {
                appid = appid,
                cliIP = inputs["ClientIP"],
                cliNA = CliNa,
                uid = inputs["CustId"],
                order = inputs["TransID"],
                price = Convert.ToDecimal(inputs["Amount"]).ToString("#"),
                payType = payType,
                notifyUrl = UrlEncode(inputs["CallBackUrl"])
            };

            depositReq.sn = GetSignature(depositReq.appid, depositReq.cliIP, depositReq.cliNA, depositReq.order,
                depositReq.payType, depositReq.price, depositReq.uid, key);

            var sUrl = inputs["host"] + inputs["deposit"];
            var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpGet(sUrl, sParam, inputs["TransID"], true);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var depositResult = new DepositResult();
            if (depositRes.e == (int)DepositStatus.Success)
            {
                // oDepositResult.ThirdId = depositRes.data.orderNo;
                var responseData = new Dictionary<string, string>
                {
                    { "DepositAmount", depositReq.price },
                    { "BankName", depositRes.d.bankType },
                    { "AccountName", depositRes.d.accountName },
                    { "AccountNumber", depositRes.d.account }
                };
                depositResult.Url = depositRes.d.h5;
                depositResult.FormData = responseData;
                depositResult.Success = true;
                depositResult.Message = "";
            }
            else
            {
                depositResult.Success = false;
                depositResult.Message = depositRes.m;
            }

            return depositResult;
        }
        
        private string GetSignature(int appid,string cliIp, string cliNa, string order, string payType, string price, string uid, string key)
        {
            var type = string.Empty;
            if (!string.IsNullOrEmpty(payType))
            {
                type = $"payType={payType}";
            }
            var parameter = $"appid={appid}cliIP={cliIp}cliNA={cliNa}order={order}{type}price={price}uid={uid}secret={key}";
            
            return ToMD5(parameter).ToLower();
        }

        private string GetCallbackSignature(string amount, string gid, string order, string transactionId, string uid,
            string key)
        {

            var parameter = $"amount={amount}gid={gid}order={order}transaction_id={transactionId}uid={uid}secret={key}";
            return ToMD5(parameter).ToLower();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            throw new NotImplementedException();
        }
    }
}
