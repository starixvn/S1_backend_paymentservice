using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        //public virtual IEnumerable<Comments> Comments { get; set; }
    }
}
