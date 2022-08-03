using System.Collections.Generic;

namespace PaymentService.Service.Model
{
    public class PayResult
    {
        public string Url { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public int Mode { get; set; }
        public string ThirdId { get; set; }
        public string Message { get; set; }
    }

    public class PayCurrency
    {
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        public string AmountShow { get; set; }
    }

    public class PayLanguage
    {
        public int PayId { get; set; }
        public string LangId { get; set; }
        public string PayLangId { get; set; }
    }
    
    public class PayGroup
    {

    }

    public class ExchangeResult
    {
        public Dictionary<string, string> FormData { get; set; }
    }
}
