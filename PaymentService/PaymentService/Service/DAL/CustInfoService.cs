using PaymentService.Service.Model;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Model;
using PaymentService.Service.Library;

namespace PaymentService.Service.DAL
{
    public class CustInfoService
    {
        private readonly MySqlClient _reportDbClient;
        public CustInfoService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public int GetVipLevel(int custId)
        {
            return _reportDbClient.ExecuteScalar<int>("VipLevel_Get", new { CustId = custId });
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
