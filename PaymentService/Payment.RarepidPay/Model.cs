using Newtonsoft.Json;

namespace Payment.RarepidPay
{
    public class DepositResponse
    {
        [JsonProperty("statuscode")] 
        public int StatusCode { get; set; }
        [JsonProperty("message")] 
        public string Message { get; set; }
        [JsonProperty("url")] 
        public string Url { get; set; }
    }

    public class DepositRequest
    {
        [JsonProperty("name")] 
        public string Name { get; set; }
        [JsonProperty("amount")] 
        public int Amount { get; set; }
        [JsonProperty("txnid")] 
        public string TxnId { get; set; }
        [JsonProperty("callback_url")] 
        public string CallbackUrl { get; set; }
    }

    public class PaymentReceive
    {
        [JsonProperty("statuscode")] 
        public int StatusCode { get; set; }
        [JsonProperty("message")] 
        public string Message { get; set; }
        [JsonProperty("token")] 
        public string Token { get; set; }
        [JsonProperty("amount")] 
        public string Amount { get; set; }
        [JsonProperty("txnid")] 
        public string TxnId { get; set; }
        [JsonProperty("status")] 
        public string Status { get; set; }
        [JsonProperty("signature")] 
        public string Signature { get; set; }
        [JsonProperty("updated")] 
        public string Updated { get; set; }
    }

    public class WithdrawalRequest
    {
        [JsonProperty("name")] 
        public string Name { get; set; }
        [JsonProperty("bankcode")] 
        public string BankCode { get; set; }
        [JsonProperty("receiver")] 
        public string Receiver { get; set; }
        [JsonProperty("account")] 
        public string Account { get; set; }
        [JsonProperty("ifsc")] 
        public string Ifsc { get; set; }
        [JsonProperty("amount")] 
        public int Amount { get; set; }
        [JsonProperty("txnid")] 
        public string TxnId { get; set; }
        [JsonProperty("callback_url")] 
        public string CallbackUrl { get; set; }
    }
    
    public class WithdrawalResponse
    {
        [JsonProperty("statuscode")]
        public int StatusCode { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
    }
    
    internal class CallbackResponse
    {
        public CallbackResponse(int code, string msg)
        {
            Code = code;
            Message = msg;
        }
    
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}