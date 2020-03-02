using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentService.Service.DAL;
using PaymentService.Service.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PaymentService.Service.BLL
{
    public class PayBLL
    {
        private readonly PayService payService;
        private readonly IMemoryCache cache;
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<PayBLL> logger;
        private static ConcurrentDictionary<string, IPayHelper> PayHelperContainer = new ConcurrentDictionary<string, IPayHelper>();

        public PayBLL(ILogger<PayBLL> logger, IMemoryCache cache, IWebHostEnvironment environment, IConfiguration configuration, PayService payService)
        {
            this.payService = payService;
            this.cache = cache;
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
        }
        public IEnumerable<BankInfoPay> GetOnlineBankList()
        {
            string key = "OnlineBankList";
            var bankInfoPayListCache = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayListCache != null)
            {
                return bankInfoPayListCache;
            }
            var bankInfoPayList = payService.GetOnlineBankList();

            if (bankInfoPayList != null)
            {
                var expireTime = TimeSpan.FromHours(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return bankInfoPayList;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout()
        {
            string key = "OnlineBankListForPayout";
            var bankInfoPayListCache = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayListCache != null)
            {
                return bankInfoPayListCache;
            }
            var bankInfoPayList = payService.GetOnlineBankListForPayout();

            if (bankInfoPayList != null)
            {
                var expireTime = TimeSpan.FromHours(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return bankInfoPayList;
        }

        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            string key = "PayCurrency";
            var payCurrencyCache = cache.Get<IEnumerable<PayCurrency>>(key);
            if (payCurrencyCache != null)
            {
                return payCurrencyCache;
            }
            var payCurrency = payService.GetPayCurrency();
            if (payCurrency != null)
            {
                var expireTime = TimeSpan.FromMinutes(5);
                cache.Set(key, payCurrency, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return payCurrency;
        }
        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            string key = "PayLanguage";
            var payLanguageCache = cache.Get<IEnumerable<PayLanguage>>(key);
            if (payLanguageCache != null)
            {
                return payLanguageCache;
            }
            var payLanguage = payService.GetPayLanguage();
            if (payLanguage != null)
            {
                var expireTime = TimeSpan.FromDays(1);
                cache.Set(key, payLanguage, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return payLanguage;
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            string key = "Pay";
            var payCache = cache.Get<IEnumerable<Pay>>(key);
            if (payCache != null)
            {
                return payCache;
            }
            var pay = payService.GetPaymentList(siteId);
            if (pay != null)
            {
                var expireTime = TimeSpan.FromDays(1);
                cache.Set(key, pay, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return pay;
        }

        public IPayHelper GetPayHelper(string PayName)
        {
            if (!PayHelperContainer.ContainsKey(PayName))
            {
                var assembly = Assembly.Load($"Payment.{PayName}");
                var assemblyType = assembly.GetType($"Payment.{PayName}.PayHelper");

                if (PayHelperContainer.TryAdd(PayName, (IPayHelper)Activator.CreateInstance(assemblyType)))
                {
                    PayHelperContainer[PayName].InsertPaymentLog += InsertPaymentLog;
                }
            }

            return PayHelperContainer[PayName];
        }

        public void InsertPaymentLog(string url, string input, string output, string refId)
        {
            logger.LogInformation($"Url={url}, Input={input}, Output={output}, RefId={refId}");
        }
    }
}
