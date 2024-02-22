using PaymentCore.Models;
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
        public int CustId { get; set; }
        public string UserName { get; set; }
        public string AccountNo { get; set; }
        public string AccountType { get; set; }
        public WithdrawalType WithdrawalType { get; set; }

        public string WithdrawalInfo { get; set; }
    }

    public class WithdrawalManualApproveRequest
    {
        public string TransId { set; get; }

        public string ThirdId { get; set; }

        public string Operator { get; set; }
    }

    public class WithdrawalManualRejectRequest
    {
        public string TransId { set; get; }

        public string ThirdId { get; set; }

        public string Operator { get; set; }
    }
}
