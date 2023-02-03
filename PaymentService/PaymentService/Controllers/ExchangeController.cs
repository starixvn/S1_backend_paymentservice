using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PaymentCore.Interface;
using PaymentCore.Model.enums;
using PaymentService.Service.BLL;
using PaymentService.Service.Model;
using static PaymentService.Extensions.MethodExtension;

namespace PaymentService.Controllers;

[ApiController]
public class ExchangeController : ControllerBase
{
    private readonly CustInfoBLL _custInfoBll;
    private readonly PayBLL _payBll;
    private readonly IEnumerable<CryptoPaymentEnum> _cryptoPayment;
    private readonly ILogger<ExchangeController> _logger;
    
    public ExchangeController(IServiceProvider provider)
    {
        _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
        _payBll = provider.GetRequiredService<PayBLL>();
        _cryptoPayment = new[] {CryptoPaymentEnum.DirePay, CryptoPaymentEnum.FXBit}; // 市場要求優先使用 DirePay
        _logger = provider.GetRequiredService<ILogger<ExchangeController>>();
    }
    
    // GET
    public class CryptoExchangeReq
    {
        public string ChannelName { get; set; }
        public int CustId { get; set; }
        public decimal Amount { get; set; }
        public ExchangeTypeEnum Type { get; set; }
    }
    
    [HttpPost]
        [Route("Payment/CryptoExchange")]
        public ApiResult<ExchangeResult> CryptoExchange(CryptoExchangeReq cryptoExchangeReq)
        {
            try
            {
                var custId = cryptoExchangeReq.CustId;
                var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
                var cryptoList = _payBll.GetCryptoListForPayout(custInfo.SiteId)
                    .Where(x => x.ChannelName.Equals(cryptoExchangeReq.ChannelName, StringComparison.OrdinalIgnoreCase)
                    && custInfo.CurrencyId == x.CurrencyId);

                if (!cryptoList.Any())
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, Invalid currency");
                    return new ApiResult<ExchangeResult>(ErrorCode.InvalidCurrency);
                }

                var payList = _payBll.GetPaymentList(custInfo.SiteId);
                if (!payList.Any())
                {
                    _logger.LogError($"Method={GetCurrentMethod()}, Pay list is empty.");
                    return new ApiResult<ExchangeResult>(ErrorCode.SettingNotFound);
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
                        _logger.LogDebug($"Method={GetCurrentMethod()}, {payment} {cryptoExchangeReq.ChannelName} crypto info not found");
                        continue;
                    }
                    
                    var payHelper = (IExchangeHelper)_payBll.GetPayHelper(paySetting.PayName);
                    if (payHelper == null)
                    {
                        _logger.LogError($"Method={GetCurrentMethod()}, payHelper Setting Not Found");
                        return new ApiResult<ExchangeResult>(ErrorCode.SettingNotFound);
                    }

                    var exchangeReq = JsonConvert.DeserializeObject<Dictionary<string, string>>(paySetting.Setting);
                    exchangeReq.Add("PayCurrencyId", cryptoInfo.PayCurrencyId);
                    exchangeReq.Add("Amount", cryptoExchangeReq.Amount.ToString());
                    exchangeReq.Add("PayCode", cryptoInfo.PayCode);
                    exchangeReq.Add("CurrencyId", custInfo.CurrencyId.ToString());
                    exchangeReq.Add("ExchangeType", ((int)cryptoExchangeReq.Type).ToString());
                    
                    _logger.LogInformation(
                        $"Method={GetCurrentMethod()}, PayName={payment}, Request={JsonConvert.SerializeObject(exchangeReq)}");

                    var result = payHelper.GetExchange(exchangeReq);
                    
                    if (result.ErrorCode == "0")
                    {
                        var data = new ExchangeResult
                        {
                            FormData = result.FormData
                        };
                        return new ApiResult<ExchangeResult>((int)ErrorCode.Success, result.Message, data);
                    }
                }

                return new ApiResult<ExchangeResult>(ErrorCode.GetExchangeRateFailed);
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                return new ApiResult<ExchangeResult>(ErrorCode.SystemError);
            }
        }
}