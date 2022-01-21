using System.Collections.Generic;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.Model
{
    public class WithdrawalRequest : IPay
    {
        public string TransId { set; get; }
        public int SiteId { set; get; }
        public string PayName { set; get; }
        public string Setting { set; get; }
        public Dictionary<string, string> ISetting => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
        public int PayId { set; get; }
        public int ChannelId { set; get; }
        public int CurrencyId { set; get; }
        public decimal Amount { set; get; }
        public string BankCode { get; set; }
        public WithdrawalStatus Status { get; set; }

        //public virtual IEnumerable<Comments> Comments { get; set; }
    }
}
