namespace PaymentService.Service.Model
{
    public class BankInfoSys
    {
        public int SysId { set; get; }
        public int CurrencyId { set; get; }
        public string BankCode { set; get; }
        public string BankName { set; get; }
        public string BankLogo { set; get; }
        public string BankSite { set; get; }
    }
    public class BankInfoPay
    {
        public int SysId { get; set; }
        public int CurrencyId { get; set; }
        public string SysBankCode { get; set; }
        public string SysBankName { get; set; }
        public string BankLogo { get; set; }
        public int Method { get; set; }
        public int PayId { get; set; }
        public string BankCode { get; set; }
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
    }
    
    public class AutoChannelBankInfo : BankInfoPay
    {
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
    
    public class PayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public int GSort { get; set; }
        public int SSort { get; set; }
    }
    public class CryptoPayInfo
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
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public string PayCode { get; set; }
        public int GSort { get; set; }
        public int SSort { get; set; }
    }
    public class LoadCardPayInfo
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
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public string PayCode { get; set; }
        public int GSort { get; set; }
        public int SSort { get; set; }
        public long Updated_timestamp { get; set; }
        public string Code { get; set; }
        public string Serial { get; set; }
    }
}
