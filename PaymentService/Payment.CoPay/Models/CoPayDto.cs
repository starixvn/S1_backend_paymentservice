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

    public class CallBackDebitResult
    {
        public int transactionid { get; set; }
        public string orderid { get; set; }
        public decimal amount { get; set; }
        public decimal real_amount { get; set; }
        public string custom { get; set; }
    }

    public class PayoutResult
    {
        public string transactionid { get; set; }
    }

    public class PayoutResponse
    {
        public int status { get; set; }
        public PayoutResult result { get; set; }
        public string sign { get; set; }
    }

    public class CallBackPayoutResult
    {
        public int transactionid { get; set; }
        public string orderid { get; set; }
        public decimal amount { get; set; }
        public decimal real_amount { get; set; }
        public string custom { get; set; }
    }
}