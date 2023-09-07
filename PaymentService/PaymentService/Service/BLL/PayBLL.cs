using System.Collections.Concurrent;
using System.Reflection;
using PaymentService.Model;
using PaymentCore.Interface;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.BLL
{
    public class PayBLL
    {
        private readonly PayService payService;
        private readonly ILogger<PayBLL> logger;
        private static ConcurrentDictionary<string, IPayHelper> PayHelperContainer = new ConcurrentDictionary<string, IPayHelper>();
        private readonly ICacheHelper _redisHelper;
        private readonly IServiceProvider _provider;

        public PayBLL(IServiceProvider provider)
        {
            payService = provider.GetRequiredService<PayService>();
            logger = provider.GetRequiredService<ILogger<PayBLL>>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _provider = provider;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId, DepositType depositType)
        {
            var key = $"PaySetting:OnlineBanking:{siteId}:{depositType}";
            return _redisHelper.TryGetOrCreate<IEnumerable<BankInfoPay>>(key, TimeSpan.FromMinutes(5),
                () => payService.GetBankInfoPayList(siteId, depositType)
                    .OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
        }
        
        public IEnumerable<PayInfo> GetPayInfoList(int siteId, DepositType depositType)
        {
            var key = $"PaySetting:{siteId}:{depositType}";
            return _redisHelper.TryGetOrCreate<IEnumerable<PayInfo>>(key, TimeSpan.FromMinutes(5),
                () => payService.GetPayInfo(siteId, depositType).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout(int siteId)
        {
            return _redisHelper.TryGetOrCreate($"PaySetting:OnlineBank:Payout:{siteId}", TimeSpan.FromMinutes(5),
                () => payService.GetOnlineBankListForPayout(siteId));
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout(int siteId)
        {
            return _redisHelper.TryGetOrCreate($"PaySetting:Crypto:Payout:{siteId}", TimeSpan.FromMinutes(5),
                () => payService.GetCryptoListForPayout(siteId));
        }

        public IEnumerable<BankInfoPay> GetEWalletPayListForPayout(int siteId)
        {
            return _redisHelper.TryGetOrCreate($"PaySetting:EWallet:Payout:{siteId}", TimeSpan.FromMinutes(5),
                () => payService.GetEWalletPayListForPayout(siteId));
        }
        
        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            return _redisHelper.TryGetOrCreate("PayLanguage", TimeSpan.FromMinutes(5),
                () => payService.GetPayLanguage());
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            var key = $"PaymentSetting:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => payService.GetPaymentList(siteId)
                    .Where(x => x.PayStatus == PayStatusEnum.Enabled
                                && x.SitePayStatus == PayStatusEnum.Enabled));
        }

        public IEnumerable<Pay> GetPaymentAllList(int siteId)
        {
            var key = $"PaymentSetting:AllStatus:{siteId}";
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => payService.GetPaymentList(siteId));
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
        
        public IEnumerable<CryptoExchangeSetting> GetCryptoExchangeSetting(int siteId)
        {
            return _redisHelper.TryGetOrCreate($"PaySetting:Crypto:Exchange:{siteId}", TimeSpan.FromMinutes(5),
                () => payService.GetCryptoExchangeSetting(siteId));
        }
    }
}
