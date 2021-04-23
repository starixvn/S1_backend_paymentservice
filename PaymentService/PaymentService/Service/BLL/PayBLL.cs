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
            var bankInfoPayListCache = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayListCache != null)
            {
                return bankInfoPayListCache;
            }
            var bankInfoPayList = payService.GetOnlineBankList(siteId);

            if (bankInfoPayList != null)
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.IsDevelopment() || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return bankInfoPayList;
        }
        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int siteId)
        {
            string key = $"MobilePayInfoList_{siteId}";
            var mobilePayInfoList = cache.Get<IEnumerable<MobilePayInfo>>(key);
            if (mobilePayInfoList == null)
            {
                mobilePayInfoList = payService.GetMobilePayInfo(siteId);

                if (mobilePayInfoList != null)
                {
                    var expireTime = TimeSpan.FromMinutes(1);
                    if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                    {
                        expireTime = TimeSpan.FromMinutes(5);
                    }

                    cache.Set(key, mobilePayInfoList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
                }
            }
            return mobilePayInfoList;
        }
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoList(int siteId)
        {
            string key = $"CryptoPayInfoList_{siteId}";
            var cryptoPayInfoList = cache.Get<IEnumerable<CryptoPayInfo>>(key);
            if (cryptoPayInfoList == null)
            {
                cryptoPayInfoList = payService.GetCryptoPayInfo(siteId);

                if (cryptoPayInfoList != null)
                {
                    var expireTime = TimeSpan.FromMinutes(1);
                    if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                    {
                        expireTime = TimeSpan.FromMinutes(5);
                    }

                    cache.Set(key, cryptoPayInfoList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
                }
            }
            return cryptoPayInfoList;
        }
        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
        {
            string key = $"LoadCardPayInfoList_{siteId}";
            var loadCardPayInfoList = cache.Get<IEnumerable<LoadCardPayInfo>>(key);
            if (loadCardPayInfoList == null)
            {
                loadCardPayInfoList = payService.GetLoadCardPayInfo(siteId);

                if (loadCardPayInfoList != null)
                {
                    var expireTime = TimeSpan.FromMinutes(1);
                    if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                    {
                        expireTime = TimeSpan.FromMinutes(5);
                    }

                    cache.Set(key, loadCardPayInfoList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
                }
            }
            return loadCardPayInfoList;
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
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return bankInfoPayList;
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout()
        {
            string key = "CryptoListForPayout";
            var cryptoPayListCache = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (cryptoPayListCache != null)
            {
                return cryptoPayListCache;
            }
            var cryptoPayList = payService.GetCryptoListForPayout();

            if (cryptoPayList != null)
            {
                var expireTime = TimeSpan.FromMinutes(1);
                if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
                {
                    expireTime = TimeSpan.FromMinutes(5);
                }

                cache.Set(key, cryptoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
            }

            return cryptoPayList;
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
            string key = $"Pay_{siteId}";
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
