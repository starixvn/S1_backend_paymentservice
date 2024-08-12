using Newtonsoft.Json;

namespace Payment.EasyPay3;

public class CreatOrderResponse
{
    [JsonProperty("stt")]
    public int Status { get; set; }
    [JsonProperty("ex_stt")]
    public double ExtraStatus { get; set; }
    [JsonProperty("msg")]
    public string Msg { get; set; }

    [JsonProperty("data")]
    public CreatOrderData Data { get; set; }
}

public class CreatOrderData
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("qr_url")]
    public string QrUrl { get; set; }

    [JsonProperty("payment_url")]
    public string PaymentUrl { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("phonName")]
    public string PhoneName { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("chargeType")]
    public string ChargeType { get; set; }

    [JsonProperty("redirect")]
    public string Redirect { get; set; }

    [JsonProperty("redirect_ssl")]
    public string RedirectSsl { get; set; }

    [JsonProperty("bank_privoder")]
    public string BankPrivoder { get; set; }

    [JsonProperty("timeToExpired")]
    public int TimeToExpired { get; set; }

    [JsonProperty("usdt_rate")]
    public decimal UsdtRate { get; set; }

    [JsonProperty("deeplink")]
    public string DeepLink { get; set; }
}


public class CreatPayoutResponse
{
    [JsonProperty("stt")]
    public int Status { get; set; }
    [JsonProperty("ex_stt")]
    public double ExtraStatus { get; set; }
    [JsonProperty("msg")]
    public string Msg { get; set; }

    [JsonProperty("data")]
    public CreatPayoutData Data { get; set; }
}

public class CreatPayoutData
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}
