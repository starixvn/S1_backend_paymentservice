using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;



namespace Payment.SulifuPay
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

        private static Dictionary<int, DepositTradeStatus> DepositStatusMap =>
        new()
        {
            { -1, DepositTradeStatus.FailedToCreateAnOrder },
            { 0, DepositTradeStatus.Processing },
            { 1, DepositTradeStatus.DepositSuccessful },
            { 9, DepositTradeStatus.Reviewing }
        };
        private static Dictionary<int, WithdrawTradeStatus> WithdrawalStatusMap =>
       new()
       {
            { -2, WithdrawTradeStatus.FailedToCreateAnOrder },
            { -1, WithdrawTradeStatus.PaymentFailed },
            { 1, WithdrawTradeStatus.WithdrawalSuccessful },
            { 0, WithdrawTradeStatus.Processing },
            { 8, WithdrawTradeStatus.APIAudit },
            { 9, WithdrawTradeStatus.ManualAudit },
       };

        public class DepositResp
        {
            public int Success { get; set; }
            public string Message { get; set; }
            public string oid { get; set; }
            public string PayPage { get; set; }
            public DepositRespParams Params { get; set; }
        }
        public class DepositRespParams
        {
            public string bankAccount { get; set; }
            public string bankCode { get; set; }
            public string bankName { get; set; }
            public string branchName { get; set; }
            public string bankAccountName { get; set; }
            public string noteNo { get; set; }
            public string orig_money { get; set; }
            public string money { get; set; }
            public string pay_page_type { get; set; }
            public string qrcode_url { get; set; }
            public string phone_no { get; set; }
            public string pay_page_pin { get; set; }
        }
        public class DepositCallbackRequest
        {
            public string tradeNo { get; set; }
            public decimal topupAmount { get; set; }
            public int tradeStatus { get; set; }
            public string message { get; set; }
            public string sign { get; set; }
        }
        public class WithdrawResp
        {
            public int Success { get; set; }
            public string Message { get; set; }
            public string oid { get; set; }
        }
        public class WithdrawCallbackRequest
        {
            public string tradeNo { get; set; }
            public decimal orderAmount { get; set; }
            public int tradeStatus { get; set; }
            public string message { get; set; }
            public string sign { get; set; }
        }
        #region enum
        public enum DepositTradeStatus
        {
            FailedToCreateAnOrder,
            Processing,
            DepositSuccessful,
            Reviewing
        }

        public enum WithdrawTradeStatus
        {
            FailedToCreateAnOrder,
            PaymentFailed,
            WithdrawalSuccessful,
            Processing,
            APIAudit,
            ManualAudit
        }
        #endregion

        #region Deposit
        public override DepositResult Debit(Dictionary<string, string> inputs)
        {

            var ApiKey = inputs["key"];
            var apiUrl = inputs["host"];

            try
            {
                var merNo = inputs["merchantNo"];
                var tradeNo = inputs["TransID"];
                var orderAmount = decimal.Parse(inputs["Amount"]);

                var toMD5Data = merNo + tradeNo + orderAmount.ToString() + ApiKey;
                var sign = ToMD5(toMD5Data).ToLower();

                var req = new Dictionary<string, string>
                {
                    { "merNo", merNo },
                    { "tradeNo", tradeNo },
                    { "cType", "BankToBank" },
                    { "orderAmount", orderAmount.ToString() },
                    { "playerName", inputs["UserName"] },
                    { "playerPayBankName", inputs["BankName"] },
                    { "playerBirthday", inputs["Birth_y"] + inputs["Birth_m"] + inputs["Birth_d"]},
                    { "playerPhoneNumber", "0" + inputs["PhoneNo"] },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    //{ "playerPayAcc", inputs["PayAccount"] },
                    { "playerPayAcc", "2222444433331111" },
                    { "sign", sign }
                };

                var (resp, statusCode) = _httpHelper
                    .PostFormData<Dictionary<string, string>, DepositResp>(apiUrl + "pay/createOrder", req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException(
                        $"Generate orders failed, HttpStatus: {(int)statusCode} {statusCode}");

                if (resp.Success != 1)
                    throw new CustomException("Generate orders failed, msg=" + resp.Message);

                return new DepositResult
                {
                    Success = true,
                    ErrorCode = "0",
                    Url = resp.PayPage
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> inputs)
        {
            var req = FromJson<DepositCallbackRequest>(JsonConvert.SerializeObject(formData));
            var ApiKey = inputs["key"];
            var toMD5Data = req.tradeNo + req.topupAmount.ToString() + ApiKey;
            var sign = ToMD5(toMD5Data).ToLower();

            if (sign != req.sign)
            {
                return new NotifyDepositResult
                {
                    DepositId = req.tradeNo,
                    TransStatus = TransStatus.Pending,
                    Response = "sign does not match."
                };
            }
            var (comment, transStatus) = GetDepositStatusAndComment(req.tradeStatus);

            return new NotifyDepositResult
            {
                DepositId = req.tradeNo,
                ActualAmount = req.topupAmount,
                ThirdId = req.tradeNo,
                Comment = comment,
                TransStatus = transStatus,
                Response = "SUCCESS"
            };
        }
        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Withdraw
        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var ApiKey = inputs["key"];
            var apiUrl = inputs["host"];
            try
            {
                var merNo = inputs["merchantNo"];
                var tradeNo = inputs["TransID"];
                var bankCode = inputs["BankCode"];
                var orderAmount = decimal.Parse(inputs["Amount"]);

                var toMD5Data = merNo + tradeNo + bankCode + orderAmount.ToString() + ApiKey;
                var sign = ToMD5(toMD5Data).ToLower();

                var req = new Dictionary<string, string>
                {
                    { "merNo", merNo },
                    { "tradeNo", tradeNo },
                    { "cType", "Payout" },
                    { "bankCode", bankCode },
                    { "bankCardNo", inputs["AccountNo"] },
                    { "orderAmount", orderAmount.ToString() },
                    { "accountName", inputs["AccountName"] },
                    { "openProvince", "1" },
                    { "openCity", "1" },
                    { "notifyUrl", inputs["CallBackUrl"] },
                    { "sign", sign }
                };

                var (resp, statusCode) = _httpHelper
                    .PostFormData<Dictionary<string, string>, WithdrawResp>(apiUrl + "payout/createOrder", req);

                if (!statusCode.IsSuccessStatus())
                    throw new CustomException(
                        $"Create payout orders failed, HttpStatus: {(int)statusCode} {statusCode}");

                if (resp.Success != 1)
                    throw new CustomException("Create payout orders failed, msg=" + resp.Message);

                return new WithdrawResult
                {
                    Success = true,
                    WithdrawId = inputs["TransID"],
                    RawContent = JsonConvert.SerializeObject(resp)
                };
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> inputs)
        {
            var req = FromJson<WithdrawCallbackRequest>(JsonConvert.SerializeObject(formData));
            var ApiKey = inputs["key"];
            var toMD5Data = req.tradeNo + req.orderAmount.ToString() + ApiKey;
            var sign = ToMD5(toMD5Data).ToLower();

            if (sign != req.sign)
            {
                return new NotifyWithdrawResult
                {
                    WithdrawId = req.tradeNo,
                    TransStatus = TransStatus.Pending,
                    Message = "sign does not match.",
                };
            }

            var (comment, transStatus) = GetWithdrawStatusAndComment(req.tradeStatus);

            return new NotifyWithdrawResult
            {
                ThirdId = req.tradeNo,
                TransStatus = transStatus,
                Message = req.message,
                Response = "SUCCESS",
                WithdrawId = req.tradeNo
            };
        }
        #endregion

        #region other
        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion 

        #region private method
        private (string, TransStatus) GetDepositStatusAndComment(int tradeStatus)
        {
            DepositStatusMap.TryGetValue(tradeStatus, out var TradeStatus);

            TransStatus TransStatus = TradeStatus switch
            {
                DepositTradeStatus.FailedToCreateAnOrder => TransStatus.Fail,
                DepositTradeStatus.Processing => TransStatus.Pending,
                DepositTradeStatus.DepositSuccessful => TransStatus.Success,
                DepositTradeStatus.Reviewing => TransStatus.Pending
            };

            return (TradeStatus.ToString(), TransStatus);
        }

        private (string, TransStatus) GetWithdrawStatusAndComment(int tradeStatus)
        {
            WithdrawalStatusMap.TryGetValue(tradeStatus, out var TradeStatus);

            TransStatus TransStatus = TradeStatus switch
            {
                WithdrawTradeStatus.WithdrawalSuccessful => TransStatus.Success,
                WithdrawTradeStatus.FailedToCreateAnOrder => TransStatus.Fail,
                WithdrawTradeStatus.PaymentFailed => TransStatus.Fail,
                WithdrawTradeStatus.Processing => TransStatus.Pending,
                WithdrawTradeStatus.APIAudit => TransStatus.Pending,
                WithdrawTradeStatus.ManualAudit => TransStatus.Pending,
            };

            return (TradeStatus.ToString(), TransStatus);
        }
        #endregion
    }
}