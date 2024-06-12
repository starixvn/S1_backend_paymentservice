using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL;

public class FaceBookPixelBLL
{
    private readonly string _commonServiceUrl;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CourierAdapter _courierAdapter;

    public FaceBookPixelBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<object> Send(PixelRequest req)
    {
        var Url = _commonServiceUrl + "FaceBookPixel/Send";
        var result = await _courierAdapter.MyPostAsync<ApiResult, PixelRequest>(Url, req);

        return result;
    }

    public PixelRequest CreatePixelRequest(int custId, int currencyId, decimal? value, IEnumerable<FaceBookPixel> pixelEvent)
    {
        return new PixelRequest
        {
            CustId = custId,
            CurrencyId = currencyId,
            Value = value,
            PixelEvent = pixelEvent
        };
    }
}