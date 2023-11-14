using PaymentService.Service.Model.enums;

namespace PaymentService.Service.Model
{
    public class DepositRequest
    {
        public string TransId { set; get; }
        public string ThirdId { set; get; }
        public DepositType DepositType { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
        public string PayName { set; get; }
        public string Setting { set; get; }
        public Dictionary<string, string> ISetting => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
        public string DepositInfo { set; get; }
        public int CurrencyId { set; get; }
        public decimal Amount { set; get; }
        public decimal ActualDeposit { set; get; }
        public DateTime CreateTime { set; get; }
        public DepositStatus Status { set; get; }
    }

    public class DepositAutoCancel
    {
        public int DepositId { get; set; }
    }
}
