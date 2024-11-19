using Newtonsoft.Json;

namespace Payment.NewXfuoo
{
    public class Response<T>
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

    public class DepositData
    {
        [JsonProperty("payUrl")]
        public string PayUrl { get; set; }
    }

    public class WithdrawalData
    {
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }
    }

    public class ExchangeRateData
    {
        [JsonProperty("fiatCurrency")]
        public string FiatCurrency { get; set; }

        [JsonProperty("rate")]
        public string Rate { get; set; }
    }
}
