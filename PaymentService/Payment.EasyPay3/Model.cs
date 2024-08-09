using Newtonsoft.Json;

namespace Payment.EasyPay3;

public class CreatOrderResponse
{
    [JsonProperty("stt")]
    public int Status { get; set; }
    [JsonProperty("ex_stt")]
    public decimal ExtraStatus { get; set; }
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

public class CallBackRequest
{
    [JsonProperty("out_trade_no")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("request_amount")]
    public string RequestAmount { get; set; }

    [JsonProperty("trade_no")]
    public string ThirdId { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("data")]
    public CreatPayoutData Data { get; set; }

    [JsonProperty("errors")]
    public object Errors { get; set; }
}

public class CreatPayoutData
{
    [JsonProperty("trade_no")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("out_trade_no")]
    public string ThirdId { get; set; }

    [JsonProperty("bank_id")]
    public string BankId { get; set; }

    [JsonProperty("account_number")]
    public string AccountNumber { get; set; }

    [JsonProperty("bank_owner")]
    public string BankOwner { get; set; }

    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }
}
