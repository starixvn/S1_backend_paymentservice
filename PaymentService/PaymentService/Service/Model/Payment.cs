using PaymentService.Service.Model.enums;

namespace PaymentService.Service.Model
{
    public interface IPay
    {
        int PayId { get; set; }
        string PayName { get; set; }
        int ChannelId { get; set; }
        int CurrencyId { get; set; }
    }
    
    public class DepositPayInfo : IPay
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
    }
    
    public class PayResult
    {
        public string Url { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public int Mode { get; set; }
        public string ThirdId { get; set; }
        public string Message { get; set; }
    }

    public class PayLanguage
    {
        public int PayId { get; set; }
        public string LangId { get; set; }
        public string PayLangId { get; set; }
    }

    public class Pay
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public string Setting { get; set; }
        public PayStatusEnum PayStatus { get; set; }
        public PayStatusEnum SitePayStatus { get; set; }
    }
}
