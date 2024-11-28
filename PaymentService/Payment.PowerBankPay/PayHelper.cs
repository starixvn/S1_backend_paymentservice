using Newtonsoft.Json;

using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

using static System.Enum;

namespace Payment.PowerBankPay
{
    class PayHelper : PayHelperBase, IPayHelperNoCallBackUrl
    {
        #region Models

        public class DebitResponse
        {
            [JsonProperty("result")]
            public int Result { get; set; }
            [JsonProperty("url")]
            public string Url { get; set; }
            [JsonProperty("msg")]
            public string Message { get; set; }
        }

        public class CreditResponse
        {
            [JsonProperty("result")]
            public int Result { get; set; }
            [JsonProperty("msg")]
            public string Message { get; set; }
        }

        #endregion

        #region Deposit

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            sUrl += "SetDepositV3";

            var formData = new Dictionary<string, string>
            {
                { "MemberCode", "nova88" },
                { "TransactionID", inputs["TransID"] },
                { "CustomerName", inputs["AccountName"] },
                { "Type", inputs["BankCode"] },
                { "Amount", decimal.Parse(inputs["Amount"]).ToString("#.00") },
                { "Merchant", inputs["merchantCode"] },
                { "NotifUrl", inputs["CallBackUrl"] }
            };
            var parameter = $"{formData["Merchant"]}{inputs["key"]}{formData["MemberCode"]}{formData["TransactionID"]}{formData["CustomerName"]}{formData["Type"]}{formData["Amount"]}{formData["NotifUrl"]}";
            formData.Add("Signature", ToHMACSHA256(parameter));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}"));
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"], true);

            var depositRes = JsonConvert.DeserializeObject<DebitResponse>(sResponse);

            var oDepositResult = new DepositResult();
            if (depositRes.Result == (int)PowerBankEnum.ResultCode.Success)
            {
                var resData = new Dictionary<string, string>
                {
                    {"Result", depositRes.Result.ToString()},
                    {"Url", depositRes.Url}
                };

                oDepositResult.Url = depositRes.Url;
                oDepositResult.FormData = resData;
                oDepositResult.Success = true;
                oDepositResult.Message = depositRes.Message;
            }
            else
            {
                oDepositResult.Success = false;
                oDepositResult.ErrorCode = depositRes.Result.ToString();
                oDepositResult.Message = depositRes.Message;
            }

            return oDepositResult;
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyDepositResult
            {
                DepositId = payResource["TransID"],
                Comment = "success",
                RefThirdId = formData["TID"],
                ThirdId = formData["TID"]
            };

            var signature = $"{formData["Merchant"]}{payResource["key"]}{formData["MemberCode"]}{formData["TransactionID"]}{formData["CustomerName"]}{formData["TID"]}{formData["Amount"]}{formData["Status"]}";
            var sign = ToHMACSHA256(signature);

            TryParse(formData["Status"], out PowerBankEnum.ResultCode code);

            if (sign == formData["Signature"])
            {
                switch (code)
                {
                    case PowerBankEnum.ResultCode.Approved:
                        formData["Amount"] = formData["Amount"].Replace(",", "");

                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.Message = "APPROVED";
                        oNotify.RequestAmount = decimal.Parse(formData["Amount"]);
                        oNotify.ActualAmount = decimal.Parse(formData["Amount"]);
                        oNotify.Response = "SUCCESS";
                        break;

                    case PowerBankEnum.ResultCode.Rejected:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Message = "REJECTED";
                        oNotify.Response = "SUCCESS";
                        break;

                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Comment = "Unknown status";
                        oNotify.Response = "Unknown status";
                        break;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Pending";
                oNotify.Response = "Signature Wrong";
            }

            return oNotify;
        }

        #endregion

        #region Withdrawal

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            sUrl += "SetWithdrawal";

            var formData = new Dictionary<string, string>
            {
                { "WithdrawalID", inputs["TransID"] },
                { "FirstName", "" },
                { "LastName", inputs["AccountName"] },
                { "Type", inputs["BankCode"] },
                { "Amount", decimal.Parse(inputs["Amount"]).ToString("#.00") },
                { "Merchant", inputs["merchantCode"] },
                { "NotifUrl", inputs["CallBackUrl"] }
            };

            string parameter;
            if (inputs["BankCode"] == "Wing")
            {
                formData.Add("WingAccount", inputs["AccountNo"]);
                parameter = $"{formData["Merchant"]}{inputs["key"]}{formData["WithdrawalID"]}{formData["FirstName"]}{formData["LastName"]}{formData["Type"]}{formData["WingAccount"]}{formData["Amount"]}{formData["NotifUrl"]}";
            }
            else
            {
                formData.Add("BankName", inputs["BankName"]);
                formData.Add("BankAccNo", inputs["AccountNo"]);
                parameter = $"{formData["Merchant"]}{inputs["key"]}{formData["WithdrawalID"]}{formData["FirstName"]}{formData["LastName"]}{formData["Type"]}{formData["BankName"]}{formData["BankAccNo"]}{formData["Amount"]}{formData["NotifUrl"]}";
            }

            formData.Add("Signature", ToHMACSHA256(parameter));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}"));
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", inputs["TransID"], true);

            var withdrawalRes = JsonConvert.DeserializeObject<CreditResponse>(sResponse);
            var withdrawResult = new WithdrawResult();

            if (withdrawalRes.Result == (int)PowerBankEnum.ResultCode.Success)
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.Message;
                withdrawResult.RawContent = withdrawalRes.Message;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.Message;
                withdrawResult.RawContent = withdrawalRes.Message;
            }

            return withdrawResult;
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var oNotify = new NotifyWithdrawResult
            {
                WithdrawId = formData["WithdrawalID"],
                Message = "success"
            };

            var signature = $"{formData["Merchant"]}{payResource["key"]}{formData["WithdrawalID"]}{formData["FirstName"]}{formData["LastName"]}{formData["Amount"]}{formData["Status"]}";
            var sign = ToHMACSHA256(signature);

            TryParse(formData["Status"], out PowerBankEnum.ResultCode code);

            if (sign == formData["Signature"])
            {
                switch (code)
                {
                    case PowerBankEnum.ResultCode.Approved:
                        oNotify.TransStatus = TransStatus.Success;
                        oNotify.RequestAmount = decimal.Parse(formData["Amount"]);
                        oNotify.Message = "Withdrawal Success";
                        oNotify.Response = "SUCCESS";
                        break;

                    case PowerBankEnum.ResultCode.Rejected:
                        oNotify.TransStatus = TransStatus.Fail;
                        oNotify.Message = "Withdrawal Reject";
                        oNotify.Response = "SUCCESS";
                        break;

                    case PowerBankEnum.ResultCode.Pending:
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Message = "Withdrawal Pending";
                        oNotify.Response = "SUCCESS";
                        break;

                    default:
                        oNotify.TransStatus = TransStatus.Pending;
                        oNotify.Response = "Unknown Status";
                        break;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Response = "Signature Wrong";
            }

            return oNotify;
        }

        #endregion

        #region Not Implemented

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
