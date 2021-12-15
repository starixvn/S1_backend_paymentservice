using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PaymentCore;

namespace Payment.VaderPay
{
    class PayHelper : PayHelperBase
    {
        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(ToJson(formData));
            var parameter = $"{data.InvoiceNo}{(int)data.Status}{data.Currency}{data.Amount}{data.PaymentDate}";
            var sign = ToHMACSHA256(parameter, payResource["key"]);

            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = data.RefId,
                Message = data.StatusMessage,
                Response = ToJson(new CallBackCreditRes
                {
                    error_code = (int)WithdrawalResponseStatus.OperationSuccess,
                    message = WithdrawalResponseStatus.OperationSuccess.ToString()
                })
            };

            if (sign == data.Signature)
            {
                switch (data.Status)
                {
                    case WithdrawalCallbackStatus.Success:
                    case WithdrawalCallbackStatus.ManualApprove:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.RequestAmount = data.Amount;
                        break;
                    case WithdrawalCallbackStatus.Reject:
                        oNotify.TransStatus = TransStatus.Fail;
                        break;
                    case WithdrawalCallbackStatus.Pending:
                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        break;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = ToJson(new CallBackCreditRes
                {
                    error_code = (int)WithdrawalResponseStatus.InvalidParameter,
                    message = "Signature error"
                });;
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult();
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var parameter = $"{data.transaction}{(int)data.status}{data.Currency}{data.Amount}{data.created_at}";
            var sign = ToHMACSHA256(parameter, payResource["key"]);

            oNotify.DepositId = data.ItemID;
            oNotify.ThirdId = data.transaction;
            oNotify.Message = data.status_message;
            oNotify.Comment = data.status_message;

            if (sign == data.signature2)
            {
                switch (data.status)
                {
                    case DepositCallbackStatus.Success:
                    case DepositCallbackStatus.Approved:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.RequestAmount = data.Amount;
                        oNotify.ActualAmount = data.Amount;
                        break;
                    case DepositCallbackStatus.Rejected:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Comment = data.decline_reason;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

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
                merchant_code = inputs["merchant"],
                ref_id = inputs["TransID"],
                player_username = inputs["CustId"],
                player_ip = inputs["ClientIP"],
                currency_code = inputs["PayCurrencyId"],
                amount = Convert.ToDecimal(inputs["Amount"]).ToString(),
                bank_code = inputs["BankCode"],
                beneficiary_account = inputs["AccountNo"],
                beneficiary_name = inputs["AccountName"],
                CallBackURL = UrlEncode(inputs["CallBackUrl"])
            };
            if (request.currency_code == "INR")
            {
                request.ifsc = inputs["IFSCCode"];
            }

            var parameter = $"{request.merchant_code}{request.ref_id}{request.player_username}{request.player_ip}{request.currency_code}{request.amount}{request.bank_code}{request.beneficiary_account}{request.beneficiary_name}";
            request.hash = ToHMACSHA256(parameter, inputs["key"]);
            
            var sUrl = inputs["withdrawHost"];
            var sResponse = HttpPost(sUrl, ToJson(request), "application/json", request.ref_id, false);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);
            if (withdrawalRes.error_code == WithdrawalStatus.Success)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.error_message;
                withdrawResult.RawContent = withdrawalRes.error_message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.message;
                withdrawResult.RawContent = withdrawalRes.message;
            }

            return withdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var depositReq = new DepositReq
            {
                Seller = inputs["merchant"],
                ItemID = inputs["TransID"],
                ItemDescription = inputs["TransID"],
                Amount = Convert.ToDecimal(inputs["Amount"]),
                Currency = inputs["PayCurrencyId"],
                HTTPPostURL = UrlEncode(inputs["CallBackUrl"]),
                ClientName = inputs["CustId"],
                ReturnURL = "",
                FailedReturnURL = ""
            };

            var sUrl = inputs["depositHost"];
            var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["key"], null,
                depositReq.ItemID);

            var depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var depositResult = new DepositResult();
            if (depositRes.status == (int)DepositStatus.Initiated || depositRes.status == (int)DepositStatus.Accepted)
            {
                depositResult.ThirdId = depositRes.transaction;
                var responseData = new Dictionary<string, string>
                {
                    { "DepositAmount", depositRes.Amount.ToString() }
                };
                depositResult.Url = depositRes.redirect_to.Replace("redirectlink=", "");
                depositResult.FormData = responseData;
                depositResult.Success = true;
            }
            else
            {
                depositResult.Success = false;
                depositResult.Message = string.IsNullOrEmpty(depositRes.error_message)
                    ? depositRes.message
                    : depositRes.error_message;
            }

            return depositResult;
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
