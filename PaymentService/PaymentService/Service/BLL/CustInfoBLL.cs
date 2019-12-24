using Microsoft.Extensions.Caching.Memory;
using PaymentService.Service.DAL;
using PaymentService.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Service.BLL
{
    public class CustInfoBLL
    {
        private readonly IMemoryCache cache;
        private readonly CustInfoService custInfoService;
        public CustInfoBLL(IMemoryCache cache, CustInfoService custInfoService)
        {
            this.cache = cache;
            this.custInfoService = custInfoService;
        }
        public int GetVipLevelWithCache(int custId)
        {
            string key = $"VipLevelCache_{custId}";
            var vipLevelCache = cache.Get<int?>(key) ?? -1;
            if (vipLevelCache != -1)
            {
                return vipLevelCache;
            }

            vipLevelCache = custInfoService.GetVipLevel(custId);
            if (vipLevelCache != 0)
            {
                cache.Set(key, vipLevelCache, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
            }

            return vipLevelCache;
        }
        public Customer GetCustInfoWithCache(int custId)
        {
            string key = $"CustInfo_{custId}";
            var customer = cache.Get<Customer>(key);
            if (customer != null)
            {
                return customer;
            }

            customer = custInfoService.GetCustInfo(custId);

            if (customer != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
                cache.Set(key, customer, cacheEntryOptions);
            }

            return customer;
        }
    }
}
