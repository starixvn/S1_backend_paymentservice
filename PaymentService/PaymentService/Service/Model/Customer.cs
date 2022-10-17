namespace PaymentService.Service.Model
{
    public class Customer
    {
        public string SiteName { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public string Userpwd { get; set; }
        public int CurrencyId { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public int AgentId { get; set; }
        public string AllAgentId { get; set; }
        public int UserLevel { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public int Status { get; set; }
        public bool PhoneVerified { get; set; }
        public bool IsResetPwd { get; set; }
        public string PhoneNo { get; set; }
        public string AreaCode { get; set; }
        public string AccountName { get; set; }
        public string Aff { get; set; }
        public string AffCode { get; set; }
        public string Token { get; set; }
    }
}
