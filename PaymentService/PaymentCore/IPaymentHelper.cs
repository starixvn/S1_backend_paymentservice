using PaymentCore.Model;

namespace PaymentCore;

public interface IPaymentHelper
{
    IEnumerable<Pay> GetPaymentAllList(int siteId);
    DepositRequest GetDepositRequest(string transId);
}