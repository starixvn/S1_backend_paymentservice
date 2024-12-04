using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL;

public class CRMBLL
{
    private readonly string _commonServiceUrl;
    private readonly CourierAdapter _courierAdapter;
    public CRMBLL(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _commonServiceUrl = configBll.GetServiceHostConfig().CommonService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
    }

    public async Task<object> Send(CRMRequest req)
    {
        var url = _commonServiceUrl + "CRM/CRMEventSend";
        var result = await _courierAdapter.MyPostAsync<ApiResult, CRMRequest>(url, req);

        return result;
    }
}
