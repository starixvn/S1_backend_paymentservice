using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL;

public class FaceBookPixelBLL
{
    private readonly string _commonServiceUrl;
    private readonly CourierAdapter _courierAdapter;

    public FaceBookPixelBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<object> Send(PixelRequest req)
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
}