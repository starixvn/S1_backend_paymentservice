using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
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
        private readonly IHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<PayBLL> logger;
        private static ConcurrentDictionary<string, IPayHelper> PayHelperContainer = new ConcurrentDictionary<string, IPayHelper>();

        public PayBLL(ILogger<PayBLL> logger, HttpHelper httpHelper, IMemoryCache cache, IHostEnvironment environment, IConfiguration configuration, PayService payService)
        {
            this.payService = payService;
            this.cache = cache;
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
            PayHelperBase.HttpHelper = httpHelper;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            string key = $"OnlineBankList_{siteId}";
            var bankInfoPayList = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayList != null && bankInfoPayList.Any())
            {
                return bankInfoPayList;
            }

            bankInfoPayList = payService.GetOnlineBankList(siteId);
            if (bankInfoPayList != null && bankInfoPayList.Any())
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime);
                cache.Set(key, bankInfoPayList, cacheEntryOptions);
            }

            return bankInfoPayList;
        }

        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int siteId)
        {
            string key = $"MobilePayInfoList_{siteId}";
            var mobilePayInfoList = cache.Get<IEnumerable<MobilePayInfo>>(key);
            if (mobilePayInfoList != null && mobilePayInfoList.Any())
            {
                return mobilePayInfoList;
            }

            mobilePayInfoList = payService.GetMobilePayInfo(siteId);
            if (mobilePayInfoList != null && mobilePayInfoList.Any())
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime);
                cache.Set(key, mobilePayInfoList, cacheEntryOptions);
            }

            return mobilePayInfoList;
        }

        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoList(int siteId)
        {
            string key = $"CryptoPayInfoList_{siteId}";
            var cryptoPayInfoList = cache.Get<IEnumerable<CryptoPayInfo>>(key);
            if (cryptoPayInfoList != null && cryptoPayInfoList.Any())
            {
                return cryptoPayInfoList;

            }

            cryptoPayInfoList = payService.GetCryptoPayInfo(siteId);
            if (cryptoPayInfoList != null && cryptoPayInfoList.Any())
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime);
                cache.Set(key, cryptoPayInfoList, cacheEntryOptions);
            }

            return cryptoPayInfoList;
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
        {
            string key = $"LoadCardPayInfoList_{siteId}";
            var loadCardPayInfoList = cache.Get<IEnumerable<LoadCardPayInfo>>(key);
            if (loadCardPayInfoList != null && loadCardPayInfoList.Any())
            {
                return loadCardPayInfoList;
            }

            loadCardPayInfoList = payService.GetLoadCardPayInfo(siteId);
            if (loadCardPayInfoList != null && loadCardPayInfoList.Any())
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime);
                cache.Set(key, loadCardPayInfoList, cacheEntryOptions);
            }

            return loadCardPayInfoList;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout()
        {
            string key = "OnlineBankListForPayout";
            var bankInfoPayList = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayList != null && bankInfoPayList.Any())
            {
                return bankInfoPayList;
            }
            
            bankInfoPayList = payService.GetOnlineBankListForPayout();
            if (bankInfoPayList != null)
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime));
            }

            return bankInfoPayList;
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout()
        {
            string key = "CryptoListForPayout";
            var cryptoPayList = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (cryptoPayList != null && cryptoPayList.Any())
            {
                return cryptoPayList;
            }
            
            cryptoPayList = payService.GetCryptoListForPayout();
            if (cryptoPayList != null && cryptoPayList.Any())
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, cryptoPayList, new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime));
            }

            return cryptoPayList;
        }

        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            string key = "PayCurrency";
            var payCurrency = cache.Get<IEnumerable<PayCurrency>>(key);
            if (payCurrency != null && payCurrency.Any())
            {
                return payCurrency;
            }
            
            payCurrency = payService.GetPayCurrency();
            if (payCurrency != null && payCurrency.Any())
            {
                var expireTime = TimeSpan.FromMinutes(5);
                cache.Set(key, payCurrency, new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime));
            }

            return payCurrency;
        }

        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            string key = "PayLanguage";
            var payLanguage = cache.Get<IEnumerable<PayLanguage>>(key);
            if (payLanguage != null && payLanguage.Any())
            {
                return payLanguage;
            }
            
            payLanguage = payService.GetPayLanguage();
            if (payLanguage != null && payLanguage.Any())
            {
                var expireTime = TimeSpan.FromMinutes(5);
                cache.Set(key, payLanguage, new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime));
            }

            return payLanguage;
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            string key = $"Pay_{siteId}";
            var pay = cache.Get<IEnumerable<Pay>>(key);
            if (pay != null && pay.Any())
            {
                return pay;
            }
            
            pay = payService.GetPaymentList(siteId);
            if (pay != null && pay.Any())
            {
                var expireTime = TimeSpan.FromMinutes(5);
                cache.Set(key, pay, new MemoryCacheEntryOptions().SetSlidingExpiration(expireTime));
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
