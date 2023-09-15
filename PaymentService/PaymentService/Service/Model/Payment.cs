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

    public class PayLanguage
    {
        public int PayId { get; set; }
        public string LangId { get; set; }
        public string PayLangId { get; set; }
    }
}
