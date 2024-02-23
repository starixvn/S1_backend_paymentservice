using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentService.Exceptions;
using PaymentService.Extensions;
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
        private readonly PayBLL _payBll;
        private readonly ILogger<WithdrawalController> _logger;
        private readonly Common _common;
        private readonly PaymentBLL _paymentBll;
        private readonly WhitelistBLL _whitelistBll;

        public WithdrawalController(IServiceProvider provider)
        {
            _payBll = provider.GetRequiredService<PayBLL>();
            _logger = provider.GetRequiredService<ILogger<WithdrawalController>>();
            _common = provider.GetRequiredService<Common>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _whitelistBll = provider.GetRequiredService<WhitelistBLL>();
        }

        [Route("Payment/VerifyTransaction/{PayName}")]
        [HttpPost]
        public async Task<IActionResult> VerifyTransactionAsync(string payName)
        {
            try
            {
                var (header, bodyContent, dicParams) = await HttpExtension.GetCallbackInfo(Request);

                _logger.LogInformation(
                    $"Method={GetCurrentAsyncMethod()}, IP={_common.GetClientIP(HttpContext)}, Url={Request.Path}, form={JsonConvert.SerializeObject(dicParams)}, bodyText={bodyContent}");
                var payHelper = (IVerifyTransaction)_payBll.GetPayHelper(payName);


                var (transId, data) = payHelper.FetchTransIdAndDataBeforeVerify(dicParams, bodyContent, header);

                var withdrawalRequest = _paymentBll.GetWithdrawalRequest(transId);
                var input = withdrawalRequest == null ? new Dictionary<string, string>() : withdrawalRequest.ISetting;
                input.Add("TransIdExists", (withdrawalRequest != null).ToString());

                var result = payHelper.VerifyTransaction(data, input);

                return Content(result.Response, result.ContentType);
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
        public ApiResult WithdrawalManualApprove(WithdrawalManualApproveRequest request)
        {
            var apiResult = new ApiResult();

            #region Check

            // 檢查 IP 是否在白名單內
            const string payName = "Manual";
            var clientIp = _common.GetClientIP(HttpContext);
            if (!_whitelistBll.IsWhitelist(clientIp, payName, request.TransId, GetCurrentMethod())
                && !_common.IsInnerIP(clientIp))
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
            
            if (withdrawalRequest.WithdrawalType.IsNotOnlinePayment())
            {
                throw new PaymentException($"TransId={request.TransId}, Type={withdrawalRequest.WithdrawalType} is not online payment, cannot do manual action.");
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
                _paymentBll.WithdrawalManualApprove(withdrawalRequest, "Manual Approve", request.ThirdId, request.Operator);
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
        public ApiResult WithdrawalManualReject(WithdrawalManualRejectRequest request)
        {
            var apiResult = new ApiResult();

            #region Check

            // 檢查 IP 是否在白名單內
            const string payName = "Manual";
            var clientIp = _common.GetClientIP(HttpContext);
            if (!_whitelistBll.IsWhitelist(clientIp, payName, request.TransId, GetCurrentMethod()))
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
            
            if (withdrawalRequest.WithdrawalType.IsNotOnlinePayment())
            {
                throw new PaymentException($"TransId={request.TransId}, Type={withdrawalRequest.WithdrawalType} is not online payment, cannot do manual action.");
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
