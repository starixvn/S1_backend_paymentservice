namespace Payment.VTPay;

internal class VTPayModel
{
    internal class DepositResponse
    {
        public Content Content { get; set; }
        public int ResultCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    internal class Content
    {
        public string URL { get; set; }
        public string Status { get; set; }
        public string MerchantTransactionID { get; set; }
        public string AuthenticateCode { get; set; }
    }
}