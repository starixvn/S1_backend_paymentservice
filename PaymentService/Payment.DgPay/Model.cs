using Newtonsoft.Json;

namespace Payment.DgPay
{
    public class PaymentResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("refId")]
        public string RefId { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("amountToDeposit")]
        public float AmountToDeposit { get; set; }

        [JsonProperty("adminFee")]
        public float AdminFee { get; set; }

        [JsonProperty("playerFee")]
        public float PlayerFee { get; set; }

        [JsonProperty("agentFee")]
        public float AgentFee { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("paymentUrl")]
        public string PaymentUrl { get; set; }
    }

}
