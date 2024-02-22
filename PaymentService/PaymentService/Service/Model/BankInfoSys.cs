using Newtonsoft.Json;
using PaymentCore.Models;

namespace PaymentService.Service.Model
{
    public interface IPayInfo : IPay
    {
        public int CurrencyId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
    public class BankInfoPay : IPayInfo
    {
        public int SysId { get; set; }
        public int CurrencyId { get; set; }
        public string SysBankCode { get; set; }
        public string SysBankName { get; set; }
        public string BankLogo { get; set; }
        public int PayId { get; set; }
        public string BankCode { get; set; }
        public string EWalletName { get; set; }
        public string EWalletCode { get; set; }
        public string BankPayoutCode { get; set; }
        public string BankName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayName { get; set; }
        public string PayCode { get; set; }
        public int MSort { get; set; }
        public int SSort { get; set; }
        public bool SelBank { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class EWalletInfoSys
    {
        public int SysId { get; set; }
        public int CurrencyId { get; set; }
        public string EWalletCode { get; set; }
        public string EWalletName { get; set; }
        public string EWalletLogo { get; set; }
        public string EWalletSite { get; set; }
        public int Status { get; set; }
        public int Sort { get; set; }
    }

    public class PayInfo : IPayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public DepositType DepositType { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null
            ? null : JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public int GSort { get; set; }
        public int SSort { get; set; }
    }
    public class CryptoPayInfo : IPayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string CurrencyName => ChannelName?.Split('-').FirstOrDefault();
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public string PayCode { get; set; }
        public int GSort { get; set; }
        public int SSort { get; set; }
    }
    public class LoadCardPayInfo : IPayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public string PayCode { get; set; }
        public int GSort { get; set; }
        public int SSort { get; set; }
        public long Updated_timestamp { get; set; }
        public string Code { get; set; }
        public string Serial { get; set; }
    }

    public class CryptoExchangeSetting
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string PayCurrencyId { get; set; }
        public string PayCode { get; set; }
        public int CurrencyId { get; set; }
    }
}
