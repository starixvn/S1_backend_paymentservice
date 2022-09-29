using System.Collections.Concurrent;
using System.Reflection;
using PaymentCore;
using PaymentCore.Model;
using PaymentCore.Model.enums;
using PaymentService.Model;
using PaymentCore.Interface;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL
{
    public class PayBLL
    {
        private readonly PayService payService;
        private readonly IHostEnvironment environment;
        private readonly ILogger<PayBLL> logger;
        private static ConcurrentDictionary<string, IPayHelper> PayHelperContainer = new ConcurrentDictionary<string, IPayHelper>();
        private readonly ICacheHelper _redisHelper;
        private readonly IServiceProvider _provider;

        public PayBLL(IServiceProvider provider)
        {
            payService = provider.GetRequiredService<PayService>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            logger = provider.GetRequiredService<ILogger<PayBLL>>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _provider = provider;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            var key = $"BankInfoPayList:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => payService.GetOnlineBankList(siteId).OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
        }

        public IEnumerable<AutoChannelBankInfo> GetAutoChannelBankInfoList(int siteId, DepositType depositType)
        {
            var key = $"BankInfoPayList:{siteId}:{depositType}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => payService.GetAutoChannelBankInfoList(siteId, depositType), RedisDataBase.Shared);
        }

        public IEnumerable<PayInfo> GetPayInfoList(int siteId, DepositType depositType)
        {
            var key = $"PayInfoList:{siteId}:{depositType}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => payService.GetPayInfo(siteId, depositType).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
        }

        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoList(int siteId)
        {
            var key = $"CryptoPayInfoList:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => payService.GetCryptoPayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
            RedisDataBase.Shared);
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
        {
            var key = $"LoadCardPayInfoList:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => payService.GetLoadCardPayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout()
        {
            return _redisHelper.TryGetOrCreate("OnlineBankListForPayout", TimeSpan.FromSeconds(10),
                () => payService.GetOnlineBankListForPayout());
        }
        
        public IEnumerable<EWalletInfoSys> GetEWalletPayList()
        {
            return _redisHelper.TryGetOrCreate("EWalletPayList", TimeSpan.FromSeconds(10),
                () => payService.GetEWalletPayList());
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout()
        {
            return _redisHelper.TryGetOrCreate("CryptoListForPayout", TimeSpan.FromSeconds(10),
                () => payService.GetCryptoListForPayout());
        }

        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            return _redisHelper.TryGetOrCreate("PayCurrency", TimeSpan.FromMinutes(5),
                () => payService.GetPayCurrency(), RedisDataBase.Shared);
        }

        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            return _redisHelper.TryGetOrCreate("PayLanguage", TimeSpan.FromMinutes(5),
                () => payService.GetPayLanguage());
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            var key = $"Pay:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => payService.GetPaymentList(siteId));
        }

        public IEnumerable<Pay> GetPaymentAllList(int siteId)
        {
            var key = $"PayAll:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => payService.GetPaymentAllList(siteId));
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
    }
}
