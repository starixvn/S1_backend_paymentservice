namespace PaymentService.Service.Model
{
    public class CRM
    {
    }
    public class CRMRequest
    {
        public int Event { get; set; }
        public int CustId { get; set; }
        public decimal OrderAmount { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
    }
    public enum CRMEvent
    {
        Register_successful,
        Deposit_successful
    }
}
