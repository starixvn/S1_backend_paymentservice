using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PaymentCore.Extensions;
using PaymentService.Exceptions;
using PaymentService.Service.BLL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers
{
    [ApiController]
    public class DepositController : ControllerBase
    {
        private readonly ILogger<DepositController> _logger;
        private readonly PaymentBLL _paymentBll;
        private readonly Common _common;
        private readonly WhitelistBLL _whitelistBll;

        public DepositController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<DepositController>>();
            _paymentBll = provider.GetRequiredService<PaymentBLL>();
            _common = provider.GetRequiredService<Common>();
            _whitelistBll = provider.GetRequiredService<WhitelistBLL>();
        }

        [Route("Payment/GetClientIp")]
        [HttpGet, HttpPost]
        public string GetClientIp()
        {
            var clientIp = _common.GetClientIP(HttpContext);
            return clientIp;
        }

        [Route("Payment/getheaders")]
        [HttpGet]
        public ActionResult<Dictionary<string, StringValues>> GetHeaders()
        {
            return Ok(Request.Headers.ToDictionary(d => d.Key));
        }

        [Route("Payment/DepositManual")]
        [HttpPost]
        public ActionResult<DepositManualResponse> DepositManual(DepositManualRequest request)
        {
            try
            {
                var response = new DepositManualResponse
                {
                    ErrorCode = (int)ErrorCode.Fail,
                    Message = ErrorCode.Fail.ToString(),
                    Amount = 0
                };

                const string payName = "Manual";
                var clientIp = _common.GetClientIP(HttpContext);
                if (!_whitelistBll.IsWhitelist(clientIp, payName, request.TransId, GetCurrentMethod())
                    && !_common.IsInnerIP(clientIp))
                {
                    _logger.LogInformation($"Method={GetCurrentAsyncMethod()}, {payName} requested of {request.TransId} from {clientIp} is Invalid IP!");
                    response.ErrorCode = (int)ErrorCode.Fail;
                    response.Message = "Invalid IP!";
                    response.Amount = 0;
                    return response;
                }

                var depositInfo = _paymentBll.GetDepositInfo(request.TransId);
                
                
                if (depositInfo == null)
                {
                    response.ErrorCode = (int)ErrorCode.Fail;
                    response.Message = $"{request.TransId} is not exist";
                    response.Amount = 0;
                    return response;
                }
                
                if (depositInfo.DepositType.IsNotOnlinePayment())
                {
                    throw new PaymentException($"TransId={request.TransId}, Type={depositInfo.DepositType} is not online payment, cannot do manual action.");
                }

                if (depositInfo.Status == (int)DepositStatus.Approved)
                {
                    response.ErrorCode = (int)ErrorCode.Fail;
                    response.Message = $"{request.TransId} status is already approved and cannot be changed";
                    response.Amount = 0;
                    return response;
                }

                depositInfo.ActualDeposit = (request.Amount == 0) ? depositInfo.ActualDeposit : request.Amount;
                depositInfo.Comment = "Manual Approved";
                depositInfo.Status = (int)DepositStatus.Approved;
                depositInfo.RefId = string.IsNullOrEmpty(request.ThirdId) ? null : request.ThirdId;
                depositInfo.Operator = string.IsNullOrEmpty(request.Operator) ? "Manual" : request.Operator;

                var success = _paymentBll.DepositManualSuccess(depositInfo);
                if (success)
                {
                    response.ErrorCode = (int)ErrorCode.Success;
                    response.Message = ErrorCode.Success.ToString();
                    response.Amount = depositInfo.ActualDeposit;
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentAsyncMethod());
                return new DepositManualResponse
                {
                    ErrorCode = (int)ErrorCode.SystemError,
                    Message = ErrorCode.SystemError.ToString(),
                    Amount = 0
                };
            }
        }
    }
}
