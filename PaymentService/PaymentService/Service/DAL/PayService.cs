using PaymentService.Service.Model;
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

        public IEnumerable<BankInfoPay> GetBankInfoPayList(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_BankInfoPay_Get_V2"
                , new { _SiteId = siteId, _DepositType = depositType });
        }

        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout(int siteId)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_BankInfoPayForPayout_Get_V3", new { _SiteId = siteId });
        }

        public IEnumerable<BankInfoPay> GetCryptoListForPayout(int siteId)
        {
            return _reportDbClient.Query<BankInfoPay>("Common_CryptoPayForPayout_Get_V2", new { _SiteId = siteId });
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
            return _reportDbClient.Query<CryptoPayInfo>("Service_PayInfo_Get_V2",
                new { _SiteId = siteId, _DepositType = DepositType.CryptoPayPayment });
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfo(int siteId)
        {
            return _reportDbClient.Query<LoadCardPayInfo>("Service_PayInfo_Get_V2",
                new { _SiteId = siteId, _DepositType = DepositType.LoadCardPayment });
        }

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<PayInfo>("Service_PayInfo_Get_V2", new { _SiteId = siteId, _DepositType = (int)depositType });
        }
    }
}
