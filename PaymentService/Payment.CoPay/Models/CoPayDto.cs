namespace Payment.CoPay.Models
{
    public class Result
    {
        public string transactionid { get; set; }
        public string payurl { get; set; }
    }

    public class PayResponse
    {
        public int status { get; set; }
        public Result result { get; set; }
        public string sign { get; set; }
    }
}