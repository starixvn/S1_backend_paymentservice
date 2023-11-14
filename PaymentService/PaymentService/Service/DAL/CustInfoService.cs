using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.DAL
{
    public class CustInfoService
    {
        private readonly MySqlClient _reportDbClient;
        public CustInfoService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public int GetPointLevel(int custId)
        {
            return _reportDbClient.ExecuteScalar<int>("Member_PointLevel_Get", new { CustId = custId });
        }
        public Customer GetCustInfo(int custId)
        {
            return _reportDbClient.QueryFirst<Customer>("Customer_Get", new { CustId = custId });
        }
        public decimal GetBalance(int custId)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }
    }
}
