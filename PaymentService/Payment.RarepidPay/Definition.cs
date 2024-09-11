namespace Payment.RarepidPay
{
    public class Definition
    {
        public const string SuccessCode = "0";
        public const string CallbackResponse = "OK";
    }

    internal class Path
    {
        public const string CreateDeposit = "/Api/QRDeposit";
    }

    internal class MediaType
    {
        public const string Json = "application/json";
    }
}