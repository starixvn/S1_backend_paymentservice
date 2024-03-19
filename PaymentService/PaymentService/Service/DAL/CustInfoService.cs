using Dapper;
using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.DAL
{
    public class CustInfoService
    {
        private readonly MySqlClient _mainDbClient;
        
        public CustInfoService(IServiceProvider provider)
        {            
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public int GetPointLevel(int custId)
        {
            return _mainDbClient.ExecuteScalar<int>("Member_PointLevel_Get", new { CustId = custId });
        }
        public Customer GetCustInfo(int custId)
        {
            return _mainDbClient.QueryFirst<Customer>("Customer_Get", new { CustId = custId });
        }
        public decimal GetBalance(int custId)
        {
            return _mainDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }
        
        public CustVerificationReward GetCustVerificationReward(int custId)
        {
            return _mainDbClient.QueryFirst<CustVerificationReward>("Service_CustVerificationRewardByCustId_Get", new { _CustId = custId });
        }
        
        public int InsertQueueJob(int custId, QueueJob type, string data)
        {
            var p = new DynamicParameters(new
            {
                _CustId = custId,
                _Type = (int) type,
                _Data = data
            });
            return _mainDbClient.Execute("Service_QueueJob_Ins", p);
        }
    }
}
