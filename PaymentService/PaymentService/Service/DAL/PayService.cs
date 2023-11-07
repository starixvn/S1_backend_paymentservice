using PaymentCore.Model;
using PaymentCore.Model.enums;

using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.DAL
{
    public class PayService
    {
        private readonly MySqlClient _reportDbClient;

        public PayService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<BankInfoPay> GetBankInfoPayList(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_BankInfoPay_Get_V3"
                , new { _SiteId = siteId, _DepositType = depositType });
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout(int siteId)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_BankInfoPayForPayout_Get_V4", new { _SiteId = siteId });
        }

        public IEnumerable<EWalletInfoSys> GetEWalletPayList()
        {
            return _reportDbClient.Query<EWalletInfoSys>("Service_EWalletInfoSys_Get");
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout(int siteId)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_CryptoPayForPayout_Get_V2", new { _SiteId = siteId });
        }

        public IEnumerable<BankInfoPay> GetEWalletPayListForPayout(int siteId)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_EWalletPayForPayout_Get_V2", new { _SiteId = siteId });
        }

        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            return _reportDbClient.Query<PayLanguage>("PayLanguage_Get");
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            return _reportDbClient.Query<Pay>("Service_PaySetting_Get", new { _SiteId = siteId });
        }

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<PayInfo>("Service_PayInfo_Get_V2", new { _SiteId = siteId, _DepositType = (int)depositType });
        }

        public IEnumerable<CryptoExchangeSetting> GetCryptoExchangeSetting(int siteId)
        {
            return _reportDbClient.Query<CryptoExchangeSetting>("Service_CryptoPayForExchange_Get", new { _SiteId = siteId });
        }

    }
}
