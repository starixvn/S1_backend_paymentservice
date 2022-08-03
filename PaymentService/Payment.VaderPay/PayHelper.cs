using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;

namespace Payment.VaderPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        private readonly ILogger<PayHelper> _logger;
        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        }
        
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
                });
            }

            return oNotify;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var onlineBank = (formData != null && formData.Count > 0);
            var oNotify = new NotifyDepositResult();

            if (onlineBank)
            {
                var data = FromJson<CallBackDebitReq>(ToJson(formData));
                var parameter = $"{data.transaction}{(int)data.status}{data.Currency}{data.Amount}{data.created_at}";
                var sign = ToHMACSHA256(parameter, payResource["key"]);
                oNotify.DepositId = data.ItemID;
                oNotify.ThirdId = data.transaction;
                oNotify.Message = data.status_message;
                oNotify.Comment = data.status_message;
                oNotify.Response = ToJson(new CallBackCreditRes
                {
                    error_code = (int)DepositResponseStatus.OperationSuccess,
                    message = DepositResponseStatus.OperationSuccess.ToString()
                });

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
                        case DepositCallbackStatus.Pending:
                            oNotify.TransStatus = TransStatus.Pending;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Comment = "Signature error";
                    oNotify.Message = "Signature error";
                    oNotify.Response = ToJson(new CallBackCreditRes
                    {
                        error_code = (int)DepositResponseStatus.InvalidParameter,
                        message = "Signature error"
                    });
                }
            }
            else
            {
                var data = FromJson<CallBackDebitMobileReq>(bodyContent);
                var parameter = $"{data.InvoiceNo }{data.ReceiverBank}{data.ReceiverAccount}{data.Currency}{data.Amount}{data.PaymentDate}";
                var sign = ToHMACSHA256(parameter, payResource["key"]);
                oNotify.DepositId = data.RefId;
                oNotify.ThirdId = data.InvoiceNo;
                oNotify.Comment = $"ReceiverBank:{data.ReceiverBank}, ReceiverAccount:{data.ReceiverAccount}";
                oNotify.Response = ToJson(new CallBackCreditRes
                {
                    error_code = (int)DepositResponseStatus.OperationSuccess,
                    message = DepositResponseStatus.OperationSuccess.ToString()
                });
                
                if (sign == data.Signature)
                {
                    switch (data.Status)
                    {
                        case DepositCallbackStatus.Success:
                        case DepositCallbackStatus.Approved:
                            oNotify.TransStatus = TransStatus.Success;
                            oNotify.RequestAmount = data.Amount;
                            oNotify.ActualAmount = data.Amount;
                            break;
                        case DepositCallbackStatus.Rejected:
                            oNotify.TransStatus = TransStatus.Fail;
                            break;
                        case DepositCallbackStatus.Pending:
                            oNotify.TransStatus = TransStatus.Pending;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.Comment = "Signature error";
                    oNotify.Message = "Signature error";
                    oNotify.Response = ToJson(new CallBackCreditRes
                    {
                        error_code = (int)DepositResponseStatus.InvalidParameter,
                        message = "Signature error"
                    });
                }
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
                beneficiary_name = inputs["AccountName"]
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
            var sUrl = inputs["depositHost"];
            var sResponse = string.Empty;
            var onlineBank = string.IsNullOrEmpty(inputs["PayCode"]);
            if (onlineBank)
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
                    ReturnURL = inputs["ReturnUrl"],
                    FailedReturnURL = inputs["ReturnUrl"]
                };
                var dicP = FromJson<Dictionary<string, string>>(ToJson(depositReq));
                var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["key"], null, depositReq.ItemID , false);
            }
            else
            {
                sUrl = inputs["qrHost"];
                var mobileDepositReq = new MobileDepositReq
                {
                    merchant_code = inputs["merchant"],
                    ref_id = inputs["TransID"],
                    player_username = inputs["CustId"],
                    player_ip = inputs["ClientIP"],
                    currency_code = inputs["PayCurrencyId"],
                    amount = Convert.ToDecimal(inputs["Amount"]).ToString(),
                    client_url = inputs["ReturnUrl"]
                };
                var parameter = $"{mobileDepositReq.merchant_code}{mobileDepositReq.ref_id}{mobileDepositReq.player_username}{mobileDepositReq.player_ip}{mobileDepositReq.currency_code}{mobileDepositReq.amount}{mobileDepositReq.client_url}";
                mobileDepositReq.hash = ToHMACSHA256(parameter, inputs["key"]);
                
                _logger.LogInformation($"TransId={mobileDepositReq.ref_id}, parameter={parameter}, key={inputs["key"]}, hash={mobileDepositReq.hash}");
                sResponse = HttpPost(sUrl, ToJson(mobileDepositReq), "application/json", mobileDepositReq.ref_id, false);
            }

            var depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var depositResult = new DepositResult();
            if ((onlineBank && (depositRes.status == (int)DepositStatus.Initiated || depositRes.status == (int)DepositStatus.Accepted)) ||
                (!onlineBank && depositRes.error_code == (int)MobileDepositStatus.OperationSuccess))
            {
                depositResult.ThirdId = onlineBank ? depositRes.transaction : depositRes.invoice_number;
                var responseData = new Dictionary<string, string>
                {
                    { "DepositAmount", onlineBank ? depositRes.Amount.ToString() : depositRes.amount.ToString() }
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
        
        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            string refId;
            if (formData != null && formData.Count > 0)
            {
                formData.TryGetValue("RefId", out refId);
            }
            else
            {
                var data = FromJson<CallBackCreditReq>(bodyContent);
                refId = data.RefId;
            }

            return refId;
        }
    }
}
