using PaymentCore.Models;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL;

public class FaceBookPixelBLL
{
    private readonly string _commonServiceUrl;
    private readonly CourierAdapter _courierAdapter;
    private readonly IEnumerable<SiteEnum> _siteEnums;

    public FaceBookPixelBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _commonServiceUrl = configBll.GetServiceHostConfig().CommonService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _siteEnums = new[] { SiteEnum.Nova88, SiteEnum.Bwing, SiteEnum.Goal123 };
    }

    private async Task<object> Send(PixelRequest req)
    {
        var url = _commonServiceUrl + "FaceBookPixel/Send";
        var result = await _courierAdapter.MyPostAsync<ApiResult, PixelRequest>(url, req);

        return result;
    }

    public PixelRequest CreatePixelRequest(int siteId, int custId, int currencyId, decimal? value, IEnumerable<FaceBookPixel> pixelEvent)
    {
        return new PixelRequest
        {
            SiteId = siteId,
            CustId = custId,
            CurrencyId = currencyId,
            Value = value,
            PixelEvent = pixelEvent
        };
    }
    
    public void Send2PixelDepositSuccess(int siteId, int custId, int currencyId, decimal amount, bool is1stDeposit)
    {
        if (_siteEnums.All(x => (int)x != siteId))
            return;
        
        var pixelEvent = new List<FaceBookPixel>
        {
            FaceBookPixel.purchase,
            is1stDeposit ? FaceBookPixel.startTrial : FaceBookPixel.redeposit
        };

        _ = Send(new PixelRequest
        {
            SiteId = siteId,
            CustId = custId,
            CurrencyId = currencyId,
            Value = amount,
            PixelEvent = pixelEvent
        });
    }
    
    public void Send2PixelWithdrawalSuccess(int siteId, int custId, int currencyId, decimal amount)
    {
        if (_siteEnums.All(x => (int)x != siteId))
            return;
        
        var pixelEvent = new List<FaceBookPixel>
        {
            FaceBookPixel.withdraw
        };

        _ = Send(new PixelRequest
        {
            SiteId = siteId,
            CustId = custId,
            CurrencyId = currencyId,
            Value = amount,
            PixelEvent = pixelEvent
        });
    }
}