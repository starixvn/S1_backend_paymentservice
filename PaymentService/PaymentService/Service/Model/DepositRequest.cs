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
        public IDepositInfo depositInfo
        {
            get
            {
                return DepositType switch
                {
                    DepositType.OnlineBanking => Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo1_1>(DepositInfo),
                    DepositType.MobilePayment => Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo2_1>(DepositInfo),
                    DepositType.LocalBank => Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo3_1>(DepositInfo),
                    _ => Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfoGeneral_1>(DepositInfo)
                };
            }
        }
        public DepositStatus Status { set; get; }
    }
    public interface IDepositInfo
    {
        int Ver { get; set; }
    }
    public class DepositInfo1_1 : IDepositInfo, IPay
    {
        public int Ver { get; set; }
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
    }
    public class DepositInfo2_1 : IDepositInfo, IPay
    {
        public int Ver { get; set; }
        public int PayId { get; set; }
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
        public string BankCode { get; set; }
    }
    public class DepositInfoGeneral_1 : IDepositInfo, IPay
    {
        public int Ver { get; set; }
        public int PayId { get; set; }
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
        public string BankCode { get; set; }
    }
    public class DepositInfo3_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public int CurrencyId { get; set; }
    }
    public class Comments
    {
        public string Comment { get; set; }
        public DateTime CreateTime { get; set; }
        public int Action { get; set; }

        public string ActionString { get; set; }

        public decimal ActualDeposit { get; set; }
    }
    public class DepositAutoCancel
    {
        public int DepositId { get; set; }
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public string TransId { get; set; }
        public int Status { get; set; }
        public int DepositType { get; set; }
    }
}
