using System;
using PaymentService.Service.Model;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.DAL
{
    public class PayService
    {
        private readonly MySqlClient _reportDbClient;

        public PayService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_BankInfoPay_Get", new { SiteId = siteId });
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout()
        {
            return _reportDbClient.Query<BankInfoPay>("Common_BankInfoPayForPayout_Get");
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout()
        {
            return _reportDbClient.Query<BankInfoPay>("Common_CryptoPayForPayout_Get");
        }

        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            return _reportDbClient.Query<PayCurrency>("PayCurrency_Get");
        }

        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            return _reportDbClient.Query<PayLanguage>("PayLanguage_Get");
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            return _reportDbClient.Query<Pay>("PaySetting_Get", new { SiteId = siteId });
        }

        public IEnumerable<Pay> GetPaymentAllList(int siteId)
        {
            return _reportDbClient.Query<Pay>("PaySettingAll_Get", new { SiteId = siteId });
        }

        public IEnumerable<CryptoPayInfo> GetCryptoPayInfo(int siteId)
        {
            return _reportDbClient.Query<CryptoPayInfo>("Common_CryptoPay_Get", new { SiteId = siteId });
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfo(int siteId)
        {
            return _reportDbClient.Query<LoadCardPayInfo>("Common_LoadCardPay_Get", new { SiteId = siteId });
        }

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<PayInfo>("Service_PayInfo_Get", new { SiteId = siteId, DepositType = (int)depositType });
        }

        public IEnumerable<BankInfoPay> GetAutoChannelBankInfoList(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<BankInfoPay>("Service_AutoChannelBankInfo_Get", new { SiteId = siteId, DepositType = (int)depositType });
        }
    }
}
