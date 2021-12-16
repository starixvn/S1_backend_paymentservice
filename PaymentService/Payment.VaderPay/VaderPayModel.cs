using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Payment.VaderPay
{
    public class DepositReq
    {
        [Required]
        [Description("Merchant Username")]
        public string Seller { get; set; }
        
        [Required]
        [Description("the URL of the page where you want the client to be redirected after a successful transaction")]
        public string ReturnURL { get; set; }
        
        [Required]
        [Description("the URL of the page where you want the client to be redirected after a failed transaction")]
        public string FailedReturnURL { get; set; }
        
        [Required]
        [Description("a URL on the merchant’s website where we will automatically POST the transaction status")]
        public string HTTPPostURL { get; set; }
        
        [Required]
        [Description("Amount to debit from the client’s bank account")]
        public decimal Amount { get; set; }
        
        [Required]
        [Description("The preferred currency for the transaction")]
        public string Currency { get; set; }
        
        [Required]
        [Description("merchant provided transaction unique identifier and cannot resubmit same unique identifier")] 
        public string ItemID { get; set; }
        
        [Required]
        [Description("merchant provided description of the transaction")]
        public string ItemDescription { get; set; }
        
        [Required]
        [Description("the client’s full name")]
        public string ClientName { get; set; }
    }

    public class MobileDepositReq
    {
        [Required]
        [Description("Merchant Username")]
        public string merchant_code { get; set; }

        [Required]
        [Description("Merchant provided transaction identifier(Unique Key)")]
        public string ref_id { get; set; }
        
        [Required]
        [Description("The client’s player user name")]
        public string player_username { get; set; }

        [Required]
        [Description("The client’s player ip")]
        public string player_ip { get; set; }

        [Required]
        [Description("The transaction’s currency")]
        public string currency_code { get; set; }

        [Required]
        [Description("Amount credit to beneficiary bank account. Decimal point must be zero.")]
        public decimal amount { get; set; }

        [Required]
        [Description("The client’s page url")]
        public string client_url { get; set; }

        [Required]
        [Description("xxxxxxxxxxxxxxxxxxx")]
        public string hash { get; set; }
    }

    public class DepositRes
    {
        public string transaction { get; set; }
            
        public int status { get; set; }
            
        public string token { get; set; }
        public string redirect_to { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public int error_code { get; set; }
        public string message { get; set; }
        public string error_message { get; set; }
        public string invoice_number { get; set; }
        public string currency_code { get; set; }
        public decimal amount { get; set; }
    }

    public class CallBackDebitReq
    {
        [Required]
        [Description("a unique identifier for the contract involved")]
        public string contract { get; set; }
        
        [Required]
        [Description("the unique internal id of your API key")]
        public string apikey { get; set; }
        
        [Required]
        [Description("unique transaction identifier")]
        public string transaction { get; set; }

        [Required]
        [Description("[1 (Success/Accepted), 2 (Rejected), 3 (Approved)]")]
        public DepositCallbackStatus status { get; set; }

        [Required]
        [Description("[“Success”, “Accepted”, “Rejected”, “ERROR”]")]
        public string status_message { get; set; }

        [Required]
        [Description("merchant provided transaction identifier")]
        public string ItemID { get; set; }

        [Required]
        [Description("merchant provided description of the transaction")]
        public string ItemDescription { get; set; }

        [Required]
        [Description("Amount to debit from the client’s bank account")]
        public decimal Amount { get; set; }
        
        [Required]
        [Description("the transaction’s currency")]
        public string Currency { get; set; }

        [Required]
        [Description("the client’s full name")]
        public string ClientName { get; set; }

        [Required]
        [Description("hash_hmac() with sha256 of ‘transaction_id + status code + Currency + Amount + created_at’ signed with your API Key")]
        public string signature2 { get; set; }
        
        [Required]
        [Description("creation date at the timezone of the API key’s owner")]
        public DateTime created_at { get; set; }
        
        [Required]
        [Description("last update at the timezone of the API key’s owner")]
        public DateTime updated_at { get; set; }

        [Description("the name of the transfer’s originating bank")]
        public string bank_name { get; set; }
        
        [Description("the bank account number where the funds were transfered")]
        public string bank_account { get; set; }

        [Description("internal id of the bank account where the funds were transfered")]
        public string BusinessAcct { get; set; }

        [Description("bank account number of the paying user")]
        public string src_bank_account { get; set; }

        [Description("bank transaction reference number")]
        public string bank_reference { get; set; }

        public string decline_reason { get; set; }
    }

    public class CallBackDebitMobileReq
    {
        [Required]
        [Description("An unique identifier for the contract involved")]
        public string InvoiceNo { get; set; }
        
        [Required]
        [Description("The client’s bank code")]
        public string ReceiverBank { get; set; }
        
        [Required]
        [Description("The client’s bank account number")]
        public string ReceiverAccount { get; set; }
        
        [Required]
        [Description("The transaction’s currency")]
        public string Currency { get; set; }
        
        [Required]
        [Description("Amount credit to Receiver bank account")]
        public decimal Amount { get; set; }
        
        [Required]
        [Description("Original amount credit to Receiver bank account")]
        public decimal OriginalAmount { get; set; }
        
        [Required]
        [Description("Payment date at the timezone of the API key’s owner")]
        public DateTime PaymentDate { get; set; }
        
        [Required]
        [Description("Merchant Username")]
        public string MerchantCode { get; set; }
        
        [Required]
        [Description("Merchant provided transaction identifier")]
        public string RefId { get; set; }
        
        [Required]
        [Description("Latest transaction status")]
        public DepositCallbackStatus Status { get; set; }
        
        [Required]
        public string Signature { get; set; }
    }

    public class WithdrawalReq
    {
        [Required]
        [Description("Merchant Username")]
        public string merchant_code { get; set; }
        
        [Required]
        [Description("Merchant provided transaction identifier")]
        public string ref_id { get; set; }
        
        [Required]
        [Description("the client’s full name")]
        public string player_username { get; set; }
        
        [Required]
        [Description("the client’s ip")]
        public string player_ip { get; set; }
        
        [Required]
        [Description("The preferred currency for the transaction")]
        public string currency_code { get; set; }
        
        [Required]
        [Description("Amount to debit to the client’s bank account")]
        public string amount { get; set; }
        
        [Required]
        [Description("the client’s bank code")]
        public string bank_code { get; set; }
        
        [Required]
        [Description("the client’s bank account name")]
        public string beneficiary_account { get; set; }

        [Required]
        [Description("the client’s bank account name")]
        public string beneficiary_name { get; set; }

        [Required]
        [Description("Signature verification")]
        public string hash { get; set; }

        [Description("the client’s ifsc (required for INR transaction only)")]
        public string ifsc { get; set; }
    }

    public class WithdrawalRes
    {
        public WithdrawalStatus error_code { get; set; }
        public string error_message { get; set; }
        public string message { get; set; }
        public string invoice_number { get; set; }
        public string currency_code { get; set; }
        public decimal merchant_balance { get; set; }
    }

    public class CallBackCreditReq
    {
        [Required]
        [Description("An unique identifier for the contract involved")]
        public string InvoiceNo { get; set; }
        
        [Required]
        [Description("The client’s bank account number")]
        public string BeneficiaryAccount { get; set; }

        [Required]
        [Description("The client’s bank code")]
        public string BeneficiaryBankCode { get; set; }

        [Required]
        [Description("The client’s bank account name")]
        public string BeneficiaryName { get; set; }

        [Required]
        [Description("Submission Date at the timezone of the API key’s owner")]
        public DateTime SubmissionDate { get; set; }

        [Required]
        public WithdrawalCallbackStatus Status { get; set; }

        [Required]
        public string StatusMessage { get; set; }
        
        [Required]
        [Description("Merchant Username")]
        public string MerchantCode { get; set; }

        [Required]
        [Description("Merchant provided transaction identifier")]
        public string RefId { get; set; }

        [Required]
        [Description("The transaction’s currency")]
        public string Currency { get; set; }

        [Required]
        [Description("Amount credit to beneficiary bank account")]
        public decimal Amount { get; set; }

        [Required]
        [Description("Payment date at the timezone of the API key’s owner")]
        public DateTime PaymentDate { get; set; }

        [Required]
        public string Signature { get; set; }
    }

    public class CallBackCreditRes
    {
        public int error_code { get; set; }
        public string message { get; set; }
    }
}