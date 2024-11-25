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
        _commonServiceUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").CommonService;
        _courierAdapter = provider.GetRequiredService<CourierAdapter>();
        _siteEnums = new[] { SiteEnum.Nova88, SiteEnum.Bwing, SiteEnum.Goal123 };
    }

    private async Task<object> Send(PixelRequest req)
    {
        var url = _commonServiceUrl + "FaceBookPixel/Send";
        var result = await _courierAdapter.MyPostAsync<ApiResult, PixelRequest>(url, req);

        return result;
    }
    
    public void Send2PixelDepositSuccess(string transId ,int siteId, int custId, decimal amount, bool is1stDeposit)
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
            TransId = transId,
            SiteId = siteId,
            CustId = custId,
            Value = amount,
            PixelEvent = pixelEvent
        });
    }
    
    public void Send2PixelWithdrawalSuccess(string transId ,int siteId, int custId, decimal amount)
    {
        if (_siteEnums.All(x => (int)x != siteId))
            return;
        
        var pixelEvent = new List<FaceBookPixel>
        {
            FaceBookPixel.withdraw
        };

        _ = Send(new PixelRequest
        {
            TransId = transId,
            SiteId = siteId,
            CustId = custId,
            Value = amount,
            PixelEvent = pixelEvent
        });
    }
}