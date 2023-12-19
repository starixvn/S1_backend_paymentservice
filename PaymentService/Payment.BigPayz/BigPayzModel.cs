using Newtonsoft.Json;
using System.Transactions;


namespace Payment.BigPayz;

internal class BigPayzModel
{

    internal class DepositDetail
    {
        [JsonProperty("PayURL")]
        public string PayUrl { get; set; }
    }


    internal class DepositResponse
    {
        [JsonProperty("transaction")]
        public string transaction { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("redirect_to")]
        public string RedirectTo { get; set; }

        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }

        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("invoice_number")]
        public string InvoiceNumber { get; set; }
    }


    internal class DepositCallbackRequest
    {
        [JsonProperty("ItemID")]
        public string TransId { get; set; }

        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("signature2")]
        public string Sign { get; set; }
    }

    internal class CallbackRequest
    {
        [JsonProperty("InvoiceNo")]
        public string InvoiceNo { get; set; }

        [JsonProperty("ReceiverBank")]
        public string ReceiverBank { get; set; }

        [JsonProperty("ReceiverAccount")]
        public string ReceiverAccount { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("OriginalAmount")]
        public decimal OriginalAmount { get; set; }

        [JsonProperty("PaymentDate")]
        public string PaymentDate { get; set; }

        [JsonProperty("MerchantCode")]
        public string MerchantCode { get; set; }

        [JsonProperty("RefId")]
        public string RefId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }

        [JsonProperty("StatusMessage")]
        public string StatusMessage { get; set; }

        [JsonProperty("DeclineReason")]
        public string DeclineReason { get; set; }

        [JsonProperty("BeneficiaryAccount")]
        public string BeneficiaryAccount { get; set; }

        [JsonProperty("BeneficiaryBankCode")]
        public string BeneficiaryBankCode { get; set; }

        [JsonProperty("BeneficiaryName")]
        public string BeneficiaryName { get; set; }

        [JsonProperty("SubmissionDate")]
        public string SubmissionDate { get; set; }
    }

    internal class WithdrawalResponse
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }

        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("invoice_number")]
        public string InvoiceNumber { get; set; }

        [JsonProperty("currency_code")]
        public string Currency { get; set; }

        [JsonProperty("merchant_balance")]
        public decimal MerchantBalance { get; set; }
    }

    internal class WithdrawalCallbackRequest
    {
        [JsonProperty("InvoiceNo")]
        public string InvoiceNo { get; set; }

        [JsonProperty("BeneficiaryAccount")]
        public decimal BeneficiaryAccount { get; set; }

        [JsonProperty("BeneficiaryBankCode")]
        public string BeneficiaryBankCode { get; set; }

        [JsonProperty("BeneficiaryName")]
        public string BeneficiaryName { get; set; }

        [JsonProperty("SubmissionDate")]
        public string SubmissionDate { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("StatusMessage")]
        public string StatusMessage { get; set; }

        [JsonProperty("MerchantCode")]
        public string MerchantCode { get; set; }

        [JsonProperty("RefId")]
        public string RefId { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Amount")]
        public decimal Amount { get; set; }

        [JsonProperty("PaymentDate")]
        public string PaymentDate { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }
    }

}
