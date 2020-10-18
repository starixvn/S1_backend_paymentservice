using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public int PayId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int ChannelId { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
        public bool SelBank { get; set; }
    }
    public class MobilePayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
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
    }
    public class CryptoPayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
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
    }
}
