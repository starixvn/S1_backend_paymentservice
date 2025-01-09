using Newtonsoft.Json;
using System.Threading.Channels;
using PaymentCore.Models.Result;
namespace Payment.GameWallet
{
    internal class DepositCallbackRequest
    {
        public string actualAmount { get; set; }
        public string amount { get; set; }
        public string bank { get; set; }
        public string currency { get; set; }
        public string customer { get; set; }
        public string datetime { get; set; }
        public string key { get; set; }
        public string merchant { get; set; }
        public string reference { get; set; }
        public string refId { get; set; }
        public string statementDate { get; set; }
        public string statusCode { get; set; }
        public string statusMessage { get; set; }
        public string transactionId { get; set; }
    }

    internal class DepositPost
    {
        [JsonProperty("a")] public string Amount { get; set; }
        [JsonProperty("b")] public string Bank { get; set; }
        [JsonProperty("c4")] public string CallbackUri { get; set; }
        [JsonProperty("c3")] public string ClientIP { get; set; }
        [JsonProperty("c1")] public string Currency { get; set; }
        [JsonProperty("c2")] public string Customer { get; set; }
        [JsonProperty("d")] public string Datetime { get; set; }
        [JsonProperty("fu")] public string FailUrl { get; set; }
        [JsonProperty("k")] public string Key { get; set; }
        [JsonProperty("l")] public string Language { get; set; }
        [JsonProperty("m")] public string Merchant { get; set; }
        [JsonProperty("mt")] public string Method { get; set; }
        [JsonProperty("n")] public string Note { get; set; }
        [JsonProperty("r")] public string Reference { get; set; }
        [JsonProperty("su")] public string SuccessUrl { get; set; }
        [JsonProperty("pt")] public string PaymentChannelType { get; set; }
    }

    internal class GameWalletSetting
    {
        public string DepositUrl { get; set; }
        public string Key { get; set; }
        public string MerchantCode { get; set; }
        public string WithdrawalUrl { get; set; }
    }

    internal class WithdrawalCallBackRequest
    {
        public string ActualAmount { get; set; }
        public string Amount { get; set; }
        public string Bank { get; set; }
        public string Currency { get; set; }
        public string Customer { get; set; }
        public string Datetime { get; set; }
        public string key { get; set; }
        public string Merchant { get; set; }
        public string Reference { get; set; }
        public string RefId { get; set; }
        public string StatementDate { get; set; }
        public string StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public string TransactionId { get; set; }
    }

    internal class WithdrawalPost
    {
        public string key;
        public string accountName { get; set; }
        public string accountNumber { get; set; }
        public string amount { get; set; }
        public string bank { get; set; }
        public string callbackUri { get; set; }
        public string currency { get; set; }
        public string customer { get; set; }
        public string datetime { get; set; }
        public string merchant { get; set; }
        public string note { get; set; }
        public string reference { get; set; }
    }

    internal class WithdrawalResponse
    {
        public string actualAmount { get; set; }
        public string amount { get; set; }
        public string bank { get; set; }
        public string currency { get; set; }
        public string customer { get; set; }
        public string datetime { get; set; }
        public string key { get; set; }
        public string merchant { get; set; }
        public string reference { get; set; }
        public string refId { get; set; }
        public string statementDate { get; set; }
        public string statusCode { get; set; }
        public string statusMessage { get; set; }
        public string transactionId { get; set; }
    }

    internal class VerifyWithdrawalResp
    {       
        public string Message { get; set; }

        public bool Verified { get; set; }
    }

    public  class VerifyTransactionResultGW : VerifyTransactionResult
    {
        //public VerifyTransactionResultGW(int status, string contentType, string response)
        //: base(contentType, response)
        //{
        //    Status = status;
        //}
        public int Status { get; set; }
    }
}
