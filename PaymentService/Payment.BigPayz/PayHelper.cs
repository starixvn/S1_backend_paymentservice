using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using static Payment.BigPayz.BigPayzModel;
using static Payment.BigPayz.BigPayzDefinition;
using Newtonsoft.Json;
using System.Net;

namespace Payment.BigPayz
{
    public class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
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
            var baseApiUrl = input["host"];
            var qrBaseApiUrl = input["QRhost"];
            var key = input["APIKey"];
            var merchantNo = input["MerchantCode"];
            var reqAmount = decimal.Parse(input["Amount"]);
            var payCode = input["PayCode"];
            var transaction = "";
            var redirectURL = "";

            try
            {
                //PromptPay
                Dictionary<string, string> req;
                DepositResponse response;
                HttpStatusCode statusCode;
                
                if (payCode == "BigQR")
                {
                    req = new Dictionary<string, string>
                    {
                        { "merchant_code", merchantNo },
                        { "ref_id", input["TransID"] },
                        { "player_username", input["UserName"] },
                        { "player_ip", input["ClientIP"] },
                        { "currency_code", input["PayCurrencyId"] },
                        { "amount", reqAmount.ToString("G29") },
                        { "client_url", input["ReturnUrl"] },
                    };

                    req.Add("hash", GetSign(req, key));
                    req.Add("lang", input["Lang"]);

                    var url = input["PayCurrencyId"] == "THB"
                        ? $"{qrBaseApiUrl}{BigPayzDefinition.Path.QRDeposit}"
                        : $"{baseApiUrl}{BigPayzDefinition.Path.DuitNow}";
                    (response, statusCode) = _httpHelper
                            .PostJson<Dictionary<string, string>, DepositResponse>(url, req);

                    transaction = response.InvoiceNumber;
                    redirectURL = response.RedirectTo;
                }
                //BankTransfer
                else
                {
                    req = new Dictionary<string, string>
                    {
                        { "MerchantCode", merchantNo },
                        { "ItemID", input["TransID"] },
                        { "Currency", input["PayCurrencyId"] },
                        { "Amount", reqAmount.ToString("G29") }
                        
                        
                    };

                    req.Add("Hash", GetSign(req, key));
                    req.Add("ReturnURL", input["CallBackUrl"]);
                    req.Add("FailedReturnURL", input["CallBackUrl"]);
                    req.Add("HTTPPostURL", input["CallBackUrl"]);
                    req.Add("PlayerId", input["UserName"]);
                    req.Add("ItemDescription", input["TransID"]);

                    (response, statusCode) = _httpHelper
                            .PostJson<Dictionary<string, string>, DepositResponse>($"{baseApiUrl}{BigPayzDefinition.Path.Deposit}", req);

                    transaction = response.transaction;
                    redirectURL = response.RedirectTo.Replace("redirectlink=", "");
                }
                

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Bigpayz deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                //BankTransfer不回傳errorcode
                if (response.ErrorCode != null && !ErrorCode.Success.Equals(response.ErrorCode))
                {
                    //BankTransfer回傳message，PromptPay回傳errormessage
                    throw new CustomException(response.Message + response.ErrorMessage, response.ErrorCode);
                }
                

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = redirectURL,
                    ThirdId = transaction
                };
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
            var apiKey = input["APIKey"];
            var depositType = input["DepositType"];
            Dictionary<string, string> reqForSign;
            string transId;
            string sign;
            string status;
            decimal amount;

            //MobilePayment
            if (depositType == "2")
            {
                var dicReq = FromJson<Dictionary<string, string>>(bodyContent);

                reqForSign = new Dictionary<string, string>
                {
                    // ‘InvoiceNo + ReceiverBank + ReceiverAccount + Currency + Amount + PaymentDate’ 
                    { "InvoiceNo", dicReq["InvoiceNo"] },
                    { "ReceiverBank", dicReq["ReceiverBank"] },
                    { "ReceiverAccount", dicReq["ReceiverAccount"] },
                    { "Currency", dicReq["Currency"] },
                    { "Amount", dicReq["Amount"] },
                    { "PaymentDate", dicReq["PaymentDate"] }
                };
                transId = dicReq["RefId"];
                sign = dicReq["Signature"];
                status = dicReq["Status"];
                amount = decimal.Parse(dicReq["OriginalAmount"]);
            }
            ////BankTransfer
            else
            {
                reqForSign = new Dictionary<string, string>
                {
                    // ‘transaction_id + status code + Currency + Amount + created_at’
                    { "TransactionId", formData["transaction"] },
                    { "status", formData["status"] },
                    { "Currency", formData["Currency"] },
                    { "Amount", formData["Amount"] },
                    { "createdat", formData["created_at"] }
                };
                transId = formData["ItemID"];
                sign = formData["signature2"];
                status = formData["status"];
                amount = decimal.Parse(formData["Amount"]);
            }
            
            if (!VerifySign(reqForSign, apiKey, sign))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = BigPayzDefinition.GetTransStatus(status);

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = amount,
                ThirdId = transId,
                Comment = status,
                TransStatus = transStatus,
                Response = BigPayzDefinition.CallbackResponse
            };

        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var baseApiUrl = inputs["Payouthost"];
            var key = inputs["APIKey"];
            var merchantNo = inputs["MerchantCode"];
            var reqAmount = decimal.Parse(inputs["Amount"]);
            var transId = inputs["TransID"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "merchant_code", merchantNo },
                    { "ref_id", transId },
                    { "player_username", inputs["UserName"] },
                    { "player_ip", inputs["ClientIP"] },
                    { "currency_code", inputs["PayCurrencyId"] },
                    { "amount", reqAmount.ToString("G29") },
                    { "bank_code", inputs["BankCode"] },
                    { "beneficiary_account", inputs["AccountNo"] },
                    { "beneficiary_name", inputs["AccountName"] },
                };

                req.Add("hash", GetSign(req, key));
                req.Add("account_type", "1");

                var (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, WithdrawalResponse>($"{baseApiUrl}{BigPayzDefinition.Path.Withdrawal}", req);

                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"Bigpayz withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                if (!ErrorCode.Success.Equals(response.ErrorCode))
                {
                    throw new CustomException(response.Message, response.ErrorCode);
                }

                return new WithdrawResult
                {
                    Success = true,
                    RawContent = JsonConvert.SerializeObject(response)
                };
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

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var apikey = payResource["APIKey"];
            var json = ToJson(formData);
            var req = FromJson<WithdrawalCallbackRequest>(json);
            
            var reqForSign = new Dictionary<string, string>
                {
                    //InvoiceNo + Status + Currency + Amount + PaymentDate
                    { "InvoiceNo", formData["InvoiceNo"] },
                    { "Status", formData["Status"] },
                    { "Currency", formData["Currency"] },
                    { "Amount", formData["Amount"] },
                    { "PaymentDate", formData["PaymentDate"] }
                };

            if (!VerifySign(reqForSign, apikey, req.Signature))
            {
                return new NotifyWithdrawResult
                {
                    ThirdId = req.RefId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = BigPayzDefinition.GetTransStatus(req.Status);

            return new NotifyWithdrawResult
            {
                ThirdId = req.InvoiceNo,
                TransStatus = transStatus,
                Message = req.StatusMessage,
                Response = BigPayzDefinition.CallbackResponse,
                WithdrawId = req.RefId
            };
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Private
        
        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var str = string.Join("", param
                .Select(x => x.Value));

            var sign = ToHMACSHA256(str, apiKey);

            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            string refId;
            
            if (formData is { Count: > 0 })
            {
                formData.TryGetValue("RefId", out refId);
            }
            else
            {
                var data = FromJson<Dictionary<string, string>>(bodyContent);
                data.TryGetValue("RefId", out refId);
            }

            return refId;
        }

        #endregion
    }
}