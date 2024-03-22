using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using static Payment.VTPay.VTPayModel;
using static Payment.VTPay.VTPayDefinition;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;


namespace Payment.VTPay
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
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var baseUrl = inputs["host"];
            var payCode = inputs["PayCode"];
            var currency = inputs["PayCurrencyId"];
            var transId = inputs["TransID"];
            var returnUrl = inputs["ReturnUrl"];
            var callbackUrl = inputs["CallBackUrl"];
            var amount = inputs["Amount"];
            var dateTime = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-d HH:mm:ss");//+7時區
            var IPAddress = inputs["ClientIP"];
            var bankCode = "";
            var merchantId = "";
            var passKey = "";
            var redirectURL = "";

            try
            {
                Dictionary<string, string> req;
                DepositResponse response;
                HttpStatusCode statusCode;

                switch (payCode)
                {
                    case "QRIS":
                        merchantId = inputs["MerchantIDForPQR"];
                        passKey = inputs["PassKeyForQR"];
                        bankCode = "QRIS";
                        break;
                    case "P2P":
                        merchantId = inputs["MerchantIDForP2P"];
                        passKey = inputs["PassKeyForP2P"];
                        bankCode = inputs["BankCode"];
                        break;
                    case "VA":
                        merchantId = inputs["MerchantIDForVA"];
                        passKey = inputs["PassKeyForVA"];
                        bankCode = inputs["BankCode"];
                        break;
                }

                req = new Dictionary<string, string>
                    {
                        { "MerchantID",merchantId },
                        { "Amount",amount},
                        { "BankCode",bankCode},
                        { "DateRequest",dateTime}
                    };
                req.Add("Signtrue", GetSign(req, passKey));
                req.Add("Currency", currency);
                req.Add("MerchantTransactionID", transId);
                req.Add("ReturnUrl", returnUrl);
                req.Add("RequestUrl", callbackUrl);
                req.Add("Language", "en-US");
                req.Add("IPAddress", IPAddress);
                req.Add("ParamOptional", "");

                (response, statusCode) = _httpHelper
                    .PostJson<Dictionary<string, string>, DepositResponse>($"{baseUrl}{VTPayDefinition.Path.Deposit}", req);

                redirectURL = response.Content.URL;


                if (!statusCode.IsSuccessStatus())
                {
                    throw new CustomException($"VTPay deposit failed, HttpStatus: {(int)statusCode} {statusCode}");
                }

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = redirectURL,
                };
            }
            catch (CustomException ex)
            {
                _logger.LogError($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            var dicReq = FromJson<Dictionary<string, string>>(bodyContent);
            var payCode = dicReq["MerchantID"];
            var authenticateCode = dicReq["AuthenticateCode"];
            var passKey = "";
            string transId;
            decimal amount;
            string sign;
            string status;
            Dictionary<string, string> req;

            switch (payCode)
            {
                case "883DB1A81B"://QRIS
                    passKey = payResource["PassKeyForQR"];
                    break;
                case "6BDCED5AFA"://P2P
                    passKey = payResource["PassKeyForP2P"];
                    break;
                case "630678E2C9"://VA
                    passKey = payResource["PassKeyForVA"];
                    break;
            }

            req = new Dictionary<string, string>
            {
                { "MerchantID",dicReq["MerchantID"] },
                { "MerchantTransactionID",dicReq["MerchantTransactionID"]},
                { "Amount",dicReq["Amount"] },
                { "Status",dicReq["Status"]},
                { "Currency",dicReq["Currency"]},
            };

            transId = dicReq["MerchantTransactionID"];
            amount = decimal.Parse(dicReq["Amount"]);
            sign = dicReq["Signature"];
            status = dicReq["Status"];

            if (!VerifySign(req, passKey, sign))
            {
                return new NotifyDepositResult
                {
                    DepositId = transId,
                    TransStatus = TransStatus.Pending,
                    Response = "Signature does not match."
                };
            }

            var transStatus = GetTransStatus(status);

            var response = new DepositResponse
            {
                ResultCode = 1,
                ErrorMessage = "",
                Content = new Content
                {
                    Status = "success",
                    MerchantTransactionID = transId,
                    AuthenticateCode = authenticateCode
                }
            };
            string responseString = ToJson(response);

            return new NotifyDepositResult
            {
                DepositId = transId,
                ActualAmount = amount,
                ThirdId = dicReq["PGTransactionID"],
                Comment = status,
                TransStatus = transStatus,
                Response = responseString
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
            throw new NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region private
        public string FetchTransId(Dictionary<string, string> formData, string bodyContent)
        {
            //string result = "";
            ////QR callback
            //if (formData.Count == 0)
            //{
            //    var data = FromJson<DepositCallbackRequest>(bodyContent);
            //    result = data.RefId;
            //}
            //else//bank transfer callback
            //{
            //    result = formData["RefId"];
            //}

            //return result;
            throw new NotImplementedException();
        }

        private string GetSign(IEnumerable<KeyValuePair<string, string>> param, string apiKey)
        {
            var str = string.Join("", param
                .Select(x => x.Value));

            var sign = ToSHA512(str + apiKey);

            return sign;
        }

        private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string apiKey, string target)
        {
            var sign = GetSign(param, apiKey);

            return sign.Equals(target);
        }
        #endregion
    }
}
