using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentService.Service.BLL;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers;

[ApiController]
public class ExchangeController : ControllerBase
{
    private readonly PayBLL _payBll;
    private readonly IEnumerable<CryptoPaymentEnum> _cryptoPayment;
    private readonly ILogger<ExchangeController> _logger;
    
    public ExchangeController(IServiceProvider provider)
    {
        _payBll = provider.GetRequiredService<PayBLL>();
        _cryptoPayment = new[] {CryptoPaymentEnum.DirePay, CryptoPaymentEnum.FXBit}; // 市場要求優先使用 DirePay
        _logger = provider.GetRequiredService<ILogger<ExchangeController>>();
    }
    
    // GET
    public class CryptoExchangeReq
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string ChannelName { get; set; }
        public decimal Amount { get; set; }
        public ExchangeTypeEnum Type { get; set; }
    }
    
    [HttpPost]
    [Route("Payment/CryptoExchange")]
    public ApiResult<CryptoExchangeInfo> CryptoExchange(CryptoExchangeReq req)
    {
        try
        {
            var cryptoList = _payBll.GetCryptoListForPayout(req.SiteId)
                .Where(x => x.ChannelName.Equals(req.ChannelName, StringComparison.OrdinalIgnoreCase)
                && req.CurrencyId == x.CurrencyId);

            if (!cryptoList.Any())
            {
                _logger.LogError($"Method={GetCurrentMethod()}, Invalid currency");
                return new ApiResult<CryptoExchangeInfo>(ErrorCode.InvalidCurrency);
            }

            var payList = _payBll.GetPaymentList(req.SiteId);
            if (!payList.Any())
            {
                _logger.LogError($"Method={GetCurrentMethod()}, Pay list is empty.");
                return new ApiResult<CryptoExchangeInfo>(ErrorCode.SettingNotFound);
            }

            foreach (var payment in _cryptoPayment)
            {
                var paySetting = payList.FirstOrDefault(x => x.PayId == (int)payment);

                if (paySetting == null)
                {
                    _logger.LogDebug($"Method={GetCurrentMethod()}, {payment} payment setting not found");
                    continue;
                }

                var cryptoInfo = cryptoList.FirstOrDefault(x => x.PayId == (int) payment);
                if (cryptoInfo == null)
                {
                    _logger.LogDebug($"Method={GetCurrentMethod()}, {payment} {req.ChannelName} crypto info not found");
                    continue;
                }
                
                var payHelper = (IExchangeHelper)_payBll.GetPayHelper(paySetting.PayName);
                if (payHelper == null)
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, payHelper Setting Not Found");
                    return new ApiResult<CryptoExchangeInfo>(ErrorCode.SettingNotFound);
                }

                var exchangeReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
                exchangeReq.Add("PayCurrencyId", cryptoInfo.PayCurrencyId);
                exchangeReq.Add("Amount", req.Amount.ToString());
                exchangeReq.Add("PayCode", cryptoInfo.PayCode);
                exchangeReq.Add("CurrencyId", req.CurrencyId.ToString());
                exchangeReq.Add("ExchangeType", ((int)req.Type).ToString());
                
                _logger.LogInformation(
                    $"Method={GetCurrentMethod()}, PayName={payment}, Request={JsonConvert.SerializeObject(exchangeReq)}");

                var result = RetryExtension.Do(payHelper.GetExchange, exchangeReq, 3, TimeSpan.FromMilliseconds(300));
                
                if (result.ErrorCode == "0")
                {
                    return new ApiResult<CryptoExchangeInfo>((int)ErrorCode.Success, result.Message, result.ExchangeInfo);
                }
            }

            return new ApiResult<CryptoExchangeInfo>(ErrorCode.GetExchangeRateFailed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, GetCurrentMethod());
            return new ApiResult<CryptoExchangeInfo>(ErrorCode.SystemError);
        }
    }
}