using System.Text;
using Flurl.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Interface;
using PaymentService.Service.BLL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers
{
    [ApiController]
    public class WithdrawalController : ControllerBase
    {
        private readonly CustInfoBLL _custInfoBll;
        private readonly PayBLL _payBll;
        private readonly string _outgoingIp;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WithdrawalController> _logger;
        private readonly Common _common;
        private readonly PaymentBLL _paymentBll;

        public WithdrawalController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
            _payBll = provider.GetRequiredService<PayBLL>();
            _configuration = provider.GetRequiredService<IConfiguration>();
            _outgoingIp = _configuration.GetValue<string>("OutgoingIp");
            _logger = provider.GetRequiredService<ILogger<WithdrawalController>>();
            _common = provider.GetRequiredService<Common>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
        }
        public class ReqWithdrawal
        {
            public int SysId { get; set; }
            public string TransId { get; set; }
            public int PayId { get; set; }
            public int ChannelId { get; set; }
            public int CustId { get; set; }
            public decimal Amount { get; set; }
            public string AccountName { set; get; }
            public string AccountNo { set; get; }
            public string IFSCCode { set; get; }
        }
        
        [HttpPost]
        [Route("Payment/Withdrawal")]
        public ApiResult Withdrawal(ReqWithdrawal reqWithdrawal)
        {
            var apiResult = new ApiResult();
            try
            {
                var custId = reqWithdrawal.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var onlineBank = _payBll.GetOnlineBankListForPayout(custInfo.SiteId)?.FirstOrDefault(x =>
                    x.PayId == reqWithdrawal.PayId
                    && x.SysId == reqWithdrawal.SysId
                    && x.CurrencyId == custInfo.CurrencyId);

                if (onlineBank == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                var pay = _payBll.GetPaymentList(custInfo.SiteId).FirstOrDefault(x => x.PayId == reqWithdrawal.PayId);
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError(
                        $"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var WithReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                WithReq.Add("CurrencyId", onlineBank.CurrencyId.ToString());
                WithReq.Add("PayCurrencyId", onlineBank.PayCurrencyId);
                WithReq.Add("ClientIP", _common.GetClientIP(HttpContext));
                WithReq.Add("OutgoingIp", _outgoingIp);
                WithReq.Add("CustId", custInfo.CustId.ToString());
                WithReq.Add("TransID", reqWithdrawal.TransId);
                WithReq.Add("Amount", reqWithdrawal.Amount.ToString());
                WithReq.Add("PayCode", onlineBank.PayCode);
                WithReq.Add("BankCode", onlineBank.BankCode);
                WithReq.Add("BankName", onlineBank.BankName);
                WithReq.Add("AccountName", reqWithdrawal.AccountName);
                WithReq.Add("AccountNo", reqWithdrawal.AccountNo);
                WithReq.Add("IsTest", _common.IsTest());
                WithReq.Add("IFSCCode", reqWithdrawal.IFSCCode);
                WithReq.Add("PhoneNo", custInfo.PhoneNo);
                WithReq.Add("PhoneVerified", custInfo.PhoneVerified.ToString());
                WithReq.Add("AreaCode", custInfo.AreaCode);
                WithReq.Add("WithdrawalType", "1");

                var callBackUrl = _configuration.GetValue<string>("CallBack");
                WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(WithReq)}");

                var result = payHelper.Credit(WithReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = result.Message;
                }
                else
                {
                    apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                    apiResult.Message = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                return apiResult;
            }
        }

        [HttpPost]
        [Route("Payment/CryptoWithdrawal")]
        public ApiResult CryptoWithdrawal(ReqWithdrawal reqWithdrawal)
        {
            var apiResult = new ApiResult();
            try
            {
                var custId = reqWithdrawal.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var crypto = _payBll.GetCryptoListForPayout(custInfo.SiteId)?.FirstOrDefault(x =>
                    x.PayId == reqWithdrawal.PayId
                    && x.ChannelId == reqWithdrawal.ChannelId
                    && x.CurrencyId == custInfo.CurrencyId);
                if (crypto == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                var pay = _payBll.GetPaymentList(custInfo.SiteId).FirstOrDefault(x => x.PayId == reqWithdrawal.PayId);
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError(
                        $"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var withdrawalInfo = _paymentBll.GetWithdrawalInfo(reqWithdrawal.TransId);
                var WithReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                WithReq.Add("CurrencyId", crypto.CurrencyId.ToString());
                WithReq.Add("PayCurrencyId", crypto.PayCurrencyId);
                //WithReq.Add("ClientIP", reqWithdrawal.ClientIP);
                WithReq.Add("OutgoingIp", _outgoingIp);
                WithReq.Add("CustId", custInfo.CustId.ToString());
                WithReq.Add("TransID", reqWithdrawal.TransId);
                WithReq.Add("Amount", reqWithdrawal.Amount.ToString());
                WithReq.Add("PayCode", crypto.PayCode);
                WithReq.Add("AccountName", reqWithdrawal.AccountName);
                WithReq.Add("AccountNo", reqWithdrawal.AccountNo);
                WithReq.Add("IsTest", _common.IsTest());
                var callBackUrl = _configuration.GetValue<string>("CallBack");
                WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");
                WithReq.Add("WithdrawalInfo", withdrawalInfo);
                WithReq.Add("WithdrawalType", "4");

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(WithReq)}");

                var result = payHelper.Credit(WithReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = result.Message;

                    if (!string.IsNullOrEmpty(result.WithdrawalInfo))
                    {
                        _paymentBll.UpdateWithdrawalInfo(reqWithdrawal.TransId, result.WithdrawalInfo);
                    }
                }
                else
                {
                    apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                    apiResult.Message = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                return apiResult;
            }
        }
        
        [HttpPost]
        [Route("Payment/EWalletWithdrawal")]
        public ApiResult EWalletWithdrawal(ReqWithdrawal reqWithdrawal)
        {
            var apiResult = new ApiResult();
            try
            {
                var custId = reqWithdrawal.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var eWalletPay = _payBll.GetEWalletPayListForPayout(custInfo.SiteId)?.FirstOrDefault(x =>
                    x.PayId == reqWithdrawal.PayId
                    && x.SysId == reqWithdrawal.SysId
                    && x.CurrencyId == custInfo.CurrencyId);

                if (eWalletPay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} Invalid Currency");
                    apiResult.ErrorCode = (int)ErrorCode.InvalidCurrency;
                    return apiResult;
                }

                var pay = _payBll.GetPaymentList(custInfo.SiteId).FirstOrDefault(x => x.PayId == reqWithdrawal.PayId);
                if (pay == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} pay Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var payHelper = _payBll.GetPayHelper(pay.PayName);
                if (payHelper == null)
                {
                    _logger.LogError(
                        $"Method={GetCurrentMethod()}, {reqWithdrawal.TransId} payHelper Setting Not Found");
                    apiResult.ErrorCode = (int)ErrorCode.SettingNotFound;
                    return apiResult;
                }

                var WithReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
                WithReq.Add("CurrencyId", eWalletPay.CurrencyId.ToString());
                WithReq.Add("PayCurrencyId", eWalletPay.PayCurrencyId);
                WithReq.Add("ClientIP", _common.GetClientIP(HttpContext));
                WithReq.Add("OutgoingIp", _outgoingIp);
                WithReq.Add("CustId", custInfo.CustId.ToString());
                WithReq.Add("TransID", reqWithdrawal.TransId);
                WithReq.Add("Amount", reqWithdrawal.Amount.ToString());
                WithReq.Add("PayCode", eWalletPay.PayCode);
                WithReq.Add("BankCode", eWalletPay.EWalletCode);
                WithReq.Add("BankName", eWalletPay.EWalletName);
                WithReq.Add("AccountName", reqWithdrawal.AccountName);
                WithReq.Add("AccountNo", reqWithdrawal.AccountNo);
                WithReq.Add("IsTest", _common.IsTest());
                WithReq.Add("IFSCCode", reqWithdrawal.IFSCCode);
                WithReq.Add("PhoneNo", custInfo.PhoneNo);
                WithReq.Add("PhoneVerified", custInfo.PhoneVerified.ToString());
                WithReq.Add("AreaCode", custInfo.AreaCode);
                WithReq.Add("WithdrawalType", "1");

                var callBackUrl = _configuration.GetValue<string>("CallBack");
                WithReq.Add("CallBackUrl", $"{callBackUrl}/Payment/WithdrawalCallBack/{reqWithdrawal.TransId}");

                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={pay.PayName}, Request={JsonConvert.SerializeObject(WithReq)}");

                var result = payHelper.Credit(WithReq);
                if (result.Success)
                {
                    apiResult.ErrorCode = 0;
                    apiResult.Message = result.Message;
                }
                else
                {
                    apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                    apiResult.Message = result.Message;
                }

                return apiResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                apiResult.ErrorCode = (int)ErrorCode.PayOutFail;
                return apiResult;
            }
        }

        [Route("Payment/WithdrawalCallBack/{TransId}")]
        public async Task<IActionResult> WithdrawalCallBackAsync(string transId)
        {
            try
            {
                string bodyText;
                var headers = Request.Headers
                    .ToDictionary(x => x.Key, x => x.Value.ToString());
                using (var reader = new StreamReader(Request.Body))
                {
                    bodyText = await reader.ReadToEndAsync();
                }
                var dicParam = new Dictionary<string, string>();
                if (Request.Method.Equals("GET"))
                {
                    if (!Request.QueryString.HasValue || Request.QueryString.Value == null)
                    {
                        _logger.LogInformation(
                            $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, DepositCallBack_GET Request.QueryString is null");
                        return Content("Request data is null");
                    }

                    dicParam = Request.QueryString.Value.TrimStart('?').ToKeyValuePairs()
                        .ToDictionary(x => x.Key, x => x.Value.ToString());
                }
                else
                {
                    if (Request.ContentType == null)
                    {
                        _logger.LogInformation(
                            $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, DepositCallBack_POST Request.ContentType is null");
                        return Content("Request data is null");
                    }

                    if (Request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ||
                        Request.ContentType.StartsWith("application/x-www-form-urlencoded",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var vFormData in Request.Form)
                        {
                            dicParam.Add(vFormData.Key, vFormData.Value);
                        }
                    }
                }
                
                _logger.LogInformation(
                    $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, Url={Request.Path}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");

                var withdrawalId = _common.GetId(transId);
                var cTransId = $"W{_common.GetEnv()}{withdrawalId}";
                if (cTransId != transId)
                {
                    var payHelperNoCallBackUrl = (IPayHelperNoCallBackUrl)_payBll.GetPayHelper(transId);
                    transId = payHelperNoCallBackUrl.FetchTransId(dicParam, bodyText);
                }

                var responseToPay = CallBackHandler(transId, dicParam, bodyText, headers);
                return Content(responseToPay);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentAsyncMethod());
                return Content(ErrorCode.Fail.ToString());
            }
        }

        private string CallBackHandler(string transId, Dictionary<string, string> dicParam, string bodyText, Dictionary<string, string> headers)
        {
            //查詢訂單在不在db
            var withdrawalRequest = _paymentBll.GetWithdrawalRequest(transId);
            //根據訂單的廠商決定要用那家金流去解讀回傳的資料
            if (withdrawalRequest != null)
            {
                var WithReq = withdrawalRequest.ISetting;
                WithReq.Add("WithdrawalType", withdrawalRequest.WithdrawalType.ToString(""));
                WithReq.Add("CurrencyId", withdrawalRequest.CurrencyId.ToString());
                
                //檢查namespace是否存在 
                if (!WithReq.ContainsKey("PayName"))
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, PayName of TransId={transId} is not exist");
                    return $"PayName of TransId={transId} is not exist";
                }

                var payHelper = _payBll.GetPayHelper(withdrawalRequest.PayName);
                //((PayHelperBase)payHelper).AddPaymentLog(InsertPaymentLog);

                //取得每家金流的成果 
                var rNotifyWithdrawalResult = payHelper.CallBackCredit(dicParam, bodyText, headers, WithReq);

                var temp = rNotifyWithdrawalResult.WithdrawId;
                while (temp.Substring(0, 1) == "0")
                {
                    temp = temp.Remove(0, 1);
                }
                if (!string.Equals(transId, temp, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, {transId} is not equal {rNotifyWithdrawalResult.WithdrawId} with PayName={withdrawalRequest.PayName}, form={JsonConvert.SerializeObject(dicParam)}, bodyText={bodyText}");
                    return $"{transId} is not equal {rNotifyWithdrawalResult.WithdrawId} with PayName={withdrawalRequest.PayName}";
                }

                if (withdrawalRequest.Status == WithdrawalStatus.Approved ||
                    withdrawalRequest.Status == WithdrawalStatus.Rejected)
                {
                    var status = rNotifyWithdrawalResult.TransStatus switch
                    {
                        TransStatus.Success => WithdrawalStatus.Approved,
                        TransStatus.Fail => WithdrawalStatus.Verify,
                        _ => WithdrawalStatus.Process
                    };
                    var message = $"{transId} status is currently {withdrawalRequest.Status.ToString()}, cannot change status to {status.ToString()}";
                    _logger.LogInformation($"Method={GetCurrentMethod()}, {message}");

                    _paymentBll.SetWithdrawalLog(transId, withdrawalRequest.SiteId,
                        message,
                        (int)withdrawalRequest.Status,
                        withdrawalRequest.PayName);
                    return rNotifyWithdrawalResult.Response;
                }

                if (rNotifyWithdrawalResult.TransStatus == TransStatus.Success)
                {
                    #region Transaction is Success
                    try
                    {
                        _paymentBll.WithdrawalSuccess(withdrawalRequest, rNotifyWithdrawalResult);
                    }
                    catch (MySqlException SqlEx)
                    {
                        /*Need Check Transaction Status*/
                        var errorMessages = new StringBuilder();
                        errorMessages.AppendLine($"Message: {SqlEx.Message}");
                        errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                        errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                        errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                        errorMessages.AppendLine($"Source: {SqlEx.Source}");
                        _logger.LogError(SqlEx, $"Method={GetCurrentMethod()}, transId={transId}");
                        return "Update UserMoney error(SQL)";
                    }
                    catch (Exception exForEachTransaction)
                    {
                        _logger.LogError(exForEachTransaction, $"Method={GetCurrentMethod()}, transId={transId}");
                        return "Update UserMoney error(Other)";
                    }
                    #endregion
                }
                else if (rNotifyWithdrawalResult.TransStatus == TransStatus.Fail)
                {
                    try
                    {
                        _paymentBll.WithdrawalFail(withdrawalRequest, rNotifyWithdrawalResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Method={GetCurrentMethod()}, transId={transId}");
                        return "Update WithdrawalFail error";
                    }
                }
                else
                {

                    try
                    {
                        _paymentBll.WithdrawalPending(withdrawalRequest, rNotifyWithdrawalResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Method={GetCurrentMethod()}, transId={transId}");
                        return "Update WithdrawalPending error";
                    }
                }

                return rNotifyWithdrawalResult.Response;
            }
            else
            {
                _logger.LogError($"Method={GetCurrentMethod()}, SerialId ={transId} is not exist");
                return $"SerialId={transId} is not exist";
            }
        }

        [Route("Payment/verifytransaction")]
        [HttpPost]
        public IActionResult verifytransaction(string transId, string key)
        {
            try
            {
                _logger.LogInformation($"Method={GetCurrentMethod()}, IP={_common.GetClientIP(HttpContext)}, Url={Request.Path}, transId={transId}, key={key}");
                var withdrawalRequest = _paymentBll.GetWithdrawalRequest(transId);
                if (withdrawalRequest != null)
                {

                    //var payHelper = payBLL.GetPayHelper(withdrawalRequest.PayName);
                    //payHelper.
                    //lib.ToMD5(transId,)
                    //withdrawalRequest.ISetting["key"];

                    return Content(true.ToString());
                }
                else
                {
                    return Content(false.ToString());
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return Content(false.ToString());
            }
        }

        /// <summary>
        /// 手動更改提款單狀態，改成 Approve
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Payment/WithdrawalManualApprove")]
        public  ApiResult WithdrawalManualApprove(WithdrawalManualApproveRequest request)
        {
            var apiResult = new ApiResult();
           
            #region Check

            // 檢查 IP 是否在白名單內
            const string payName = "Manual";
            var clientIp = _common.GetClientIP(HttpContext);
            if (!_common.IsWhiteList(clientIp, payName, request.TransId, GetCurrentMethod()))
            {
                _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {payName} requested of {request.TransId} from {clientIp} is Invalid IP!");
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.Message = "Invalid IP!";
                return apiResult;
            }

            // 查詢訂單在不在db
            var withdrawalRequest = _paymentBll.GetWithdrawalRequest(request.TransId);
            if (withdrawalRequest == null)
            {
                apiResult.ErrorCode = (int)ErrorCode.SystemError;
                apiResult.Message = $"withdrawal transId：{request.TransId} not found";
                return apiResult;
            }

            // 提款單狀態是否原本就 Approved
            if (withdrawalRequest.Status == WithdrawalStatus.Approved)
            {
                apiResult.ErrorCode = (int)ErrorCode.SystemError;
                apiResult.Message = $"withdrawal transId：{request.TransId} status already has approve，can't change";
                return apiResult;
            }

            #endregion

            // ELK log 紀錄原本的狀態變更至 Approve
            _logger.LogInformation($"{withdrawalRequest.TransId} old status is {withdrawalRequest.Status},prepare change status to {WithdrawalStatus.Approved}");

            try
            {
                _paymentBll.WithdrawalManualApprove(withdrawalRequest, "Manual Approve",request.ThirdId, request.Operator);
            }
            catch (Exception ex)
            {
                _logger.LogError($"withdrawal transId WithdrawalManualApprove Fail。exceptionMessage：{ex.Message}");
                apiResult.ErrorCode = (int)ErrorCode.SystemError;
                apiResult.Message = ex.Message;
                return apiResult;
            }

            apiResult.ErrorCode = 0;
            apiResult.Message = "Withdrawal Manual Approve Success";
            return apiResult;
        }

        /// <summary>
        /// 手動更改提款單狀態，改成 Reject
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Payment/WithdrawalManualReject")]
        public  ApiResult WithdrawalManualReject(WithdrawalManualRejectRequest request)
        {
            var apiResult = new ApiResult();

            #region Check

            // 檢查 IP 是否在白名單內
            const string payName = "Manual";
            var clientIp = _common.GetClientIP(HttpContext);
            if (!_common.IsWhiteList(clientIp, payName, request.TransId, GetCurrentMethod()))
            {
                _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {payName} requested of {request.TransId} from {clientIp} is Invalid IP!");
                apiResult.ErrorCode = (int)ErrorCode.Fail;
                apiResult.Message = "Invalid IP!";
                return apiResult;
            }

            // 查詢訂單在不在db
            var withdrawalRequest = _paymentBll.GetWithdrawalRequest(request.TransId);
            if (withdrawalRequest == null)
            {
                apiResult.ErrorCode = (int)ErrorCode.SystemError;
                apiResult.Message = $"withdrawal transId：{request.TransId} not found";
                return apiResult;
            }

            // 提款單狀態是否原本就 Rejected
            if (withdrawalRequest.Status == WithdrawalStatus.Rejected)
            {
                apiResult.ErrorCode = (int)ErrorCode.SystemError;
                apiResult.Message = $"withdrawal transId：{request.TransId} status already has rejected，can't change";
                return apiResult;
            }

            #endregion

            // ELK log 紀錄原本的狀態變更至 Rejected
            _logger.LogInformation($"{withdrawalRequest.TransId} old status is {withdrawalRequest.Status},prepare change status to {WithdrawalStatus.Approved}");

            try
            {
                _paymentBll.WithdrawalManualReject(withdrawalRequest, "Manual Rejected", request.ThirdId, request.Operator);
            }
            catch (Exception ex)
            {
                _logger.LogError($"withdrawal transId WithdrawalManualReject Fail。exceptionMessage：{ex.Message}");
                apiResult.ErrorCode = (int)ErrorCode.SystemError;
                apiResult.Message = ex.Message;
                return apiResult;
            }

            apiResult.ErrorCode = 0;
            apiResult.Message = "Withdrawal Manual Rejected Success";
            return apiResult;
        }
    }
}
