using Newtonsoft.Json;

namespace Payment.VTPay;

internal class VTPayModel
{
    public class DepositRequestData
    {
        public string MerchantID { get; set; }
        public decimal Amount { get; set; }
        public string BankCode { get; set; }
        public string DateRequest { get; set; }
        public string Currency { get; set; }
        public string MerchantTransactionID { get; set; }
        public string ReturnUrl { get; set; }
        public string RequestUrl { get; set; }
        public string Language { get; set; }
        public string IPAddress { get; set; }
        public string ParamOptional { get; set; }
        public string Signature { get; set; }
    }
    public class WithdrawalRequestData
    {
        public string MerchantID { get; set; }
        public decimal Amount { get; set; }
        public string BankCode { get; set; }
        public string DateRequest { get; set; }
        public string MerchantTransactionID { get; set; }
        public string RequestURL { get; set; }
        public string Language { get; set; }
        public string IPAddress { get; set; }
        public string Currency { get; set; }
        public string BankAccountName { get; set; }
        public string BankAccountNo { get; set; }
        public string BankBranch { get; set; }
        public string BankCity { get; set; }
        public string BankProvince { get; set; }
        public string ParamOptional { get; set; }
        public string Signature { get; set; }
    }
    internal class DepositResponse
    {
        public Content Content { get; set; }
        public int ResultCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    internal class WithdrawalResponse
    {
        [JsonProperty("ResultCode")]
        public int ResultCode { get; set; }

        [JsonProperty("ErrorMessage")]
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