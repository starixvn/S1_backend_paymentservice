using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using PaymentCore;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.BLL
{
    public class PayBLL
    {
        private readonly PayService payService;
        private readonly IHostEnvironment environment;
        private readonly ILogger<PayBLL> logger;
        private static ConcurrentDictionary<string, IPayHelper> PayHelperContainer = new ConcurrentDictionary<string, IPayHelper>();
        private readonly CacheHelper _cacheHelper;
        private readonly IServiceProvider _provider;

        public PayBLL(IServiceProvider provider)
        {
            payService = provider.GetRequiredService<PayService>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            logger = provider.GetRequiredService<ILogger<PayBLL>>();
            PayHelperBase.HttpHelper = provider.GetRequiredService<HttpHelper>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _provider = provider;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            var key = $"OnlineBankList_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetOnlineBankList(siteId));
        }

        public IEnumerable<BankInfoPay> GetAutoChannelBankInfoList(int siteId, DepositType depositType)
        {
            return _cacheHelper.TryGetOrCreate($"BankInfoPayList_{siteId}_{depositType}",
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetAutoChannelBankInfoList(siteId, depositType));
        }

        public IEnumerable<PayInfo> GetPayInfoList(int siteId, DepositType depositType)
        {
            return _cacheHelper.TryGetOrCreate($"PayInfoList_{siteId}_{depositType}",
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetPayInfo(siteId, depositType));
        }

        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoList(int siteId)
        {
            var key = $"CryptoPayInfoList_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetCryptoPayInfo(siteId));
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
        {
            var key = $"LoadCardPayInfoList_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetLoadCardPayInfo(siteId));
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout()
        {
            var key = "OnlineBankListForPayout";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetOnlineBankListForPayout());
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout()
        {
            var key = "CryptoListForPayout";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)),
                i => payService.GetCryptoListForPayout());
        }

        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            var key = "PayCurrency";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)),
                i => payService.GetPayCurrency());
        }

        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            var key = "PayLanguage";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)),
                i => payService.GetPayLanguage());
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            var key = $"Pay_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)),
                i => payService.GetPaymentList(siteId));
        }

        public IEnumerable<Pay> GetPaymentAllList(int siteId)
        {
            var key = $"PayAll_{siteId}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)),
                i => payService.GetPaymentAllList(siteId));
        }

        public IPayHelper GetPayHelper(string PayName)
        {
            if (!PayHelperContainer.ContainsKey(PayName))
            {
                var assembly = Assembly.Load($"Payment.{PayName}");
                var assemblyType = assembly.GetType($"Payment.{PayName}.PayHelper");

                if (PayHelperContainer.TryAdd(PayName
                        , (IPayHelper)ActivatorUtilities.CreateInstance(_provider, assemblyType)))
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
        
        private TimeSpan GetExpireTime()
        {
            var expireTime = TimeSpan.FromMinutes(1);
            if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "QA")
            {
                expireTime = TimeSpan.FromMinutes(5);
            }

            return expireTime;
        }
    }
}
