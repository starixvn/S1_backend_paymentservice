using Microsoft.Extensions.Caching.Memory;
using PaymentService.Model;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL
{
    public class CustInfoBLL
    {
        private readonly CustInfoService _custInfoService;
        private readonly Common _common;
        private readonly ICacheHelper _redisHelper;

        public CustInfoBLL(IServiceProvider provider)
        {
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _common = provider.GetRequiredService<Common>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }
        public int GetPointLevelWithCache(int custId)
        {
            return _redisHelper.TryGetOrCreate($"PointLevel:{custId}", TimeSpan.FromMinutes(1),
                () => _custInfoService.GetPointLevel(custId),
                RedisDataBase.Shared);
        }
        public Customer GetCustInfoWithCache(int custId)
        {
            return _redisHelper.TryGetOrCreate($"CustInfo:{custId}", TimeSpan.FromMinutes(1),
                () => _custInfoService.GetCustInfo(custId),
                RedisDataBase.Shared);
        }

        public decimal GetBalance(int custId)
        {
            return _common.Floor(_custInfoService.GetBalance(custId), 2);
        }
    }
}
