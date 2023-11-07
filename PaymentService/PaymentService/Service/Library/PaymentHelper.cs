using PaymentCore;
using PaymentCore.Model;

using PaymentService.Service.BLL;

namespace PaymentService.Service.Library;

public class PaymentHelper : IPaymentHelper
{
    private readonly PayBLL _payBll;
    private readonly PaymentBLL _paymentBll;

    public PaymentHelper(IServiceProvider provider)
    {
        _payBll = provider.GetRequiredService<PayBLL>();
        _paymentBll = provider.GetRequiredService<PaymentBLL>();
    }

    public IEnumerable<Pay> GetPaymentAllList(int siteId)
    {
        return _payBll.GetPaymentAllList(siteId);
    }

    public DepositRequest GetDepositRequest(string transId)
    {
        return _paymentBll.GetDepositRequest(transId);
    }
}