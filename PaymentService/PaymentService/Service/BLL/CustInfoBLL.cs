using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL
{
    public class CustInfoBLL
    {
        private readonly CustInfoService custInfoService;
        private readonly Common common;
        private readonly CacheHelper _cacheHelper;

        public CustInfoBLL(IServiceProvider provider)
        {
            custInfoService = provider.GetRequiredService<CustInfoService>();
            common = provider.GetRequiredService<Common>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }
        public int GetVipLevelWithCache(int custId)
        {
            var key = $"VipLevelCache_{custId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => custInfoService.GetVipLevel(custId));
        }
        public Customer GetCustInfoWithCache(int custId)
        {
            var key = $"CustInfo_{custId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)),
                i => custInfoService.GetCustInfo(custId));
        }

        public decimal GetBalance(int custId)
        {
            return common.Floor(custInfoService.GetBalance(custId), 2);
        }
    }
}
