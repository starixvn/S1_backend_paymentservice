using Newtonsoft.Json;

namespace Payment.Coin2Pay;

internal class Coin2PayBaseResp<T>
{
    [JsonProperty("result")]
    public bool Result { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("data")]
    public T Data { get; set; }
}

internal class GetWalletAddressReq
{
    /// <summary>
    /// 交易編號
    /// </summary>
    [JsonProperty("receipt_token")]
    public string ReceiptToken { get; set; }

    /// <summary>
    /// 交易幣種
    /// </summary>
    [JsonProperty("billed_currency")]
    public string BilledCurrency { get; set; }
}


internal class GetAddressData
{
    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("network")]
    public string Network { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }
}

internal class HostedPage
{
    [JsonProperty("HostedPage_Deposit")]
    public string Deposit { get; set; }

    [JsonProperty("HostedPage_Withdrawal")]
    public string Withdrawal { get; set; }

    [JsonProperty("HostedPage_Withdrawal_B")]
    public string WithdrawalB{ get; set; }
}

internal class GetAddressResp : Coin2PayBaseResp<IEnumerable<GetAddressData>>
{
    [JsonProperty("hostedPage")]
    public HostedPage HostedPage { get; set; }
}

internal class GetWithdrawalEstimateData
{
    [JsonProperty("withdrawal_Credit")]
    public decimal WithdrawalCredit{ get; set; }

    [JsonProperty("withdrawal_credit_rate")]
    public decimal WithdrawalCreditRate{ get; set; }

    [JsonProperty("asset_Estimate_get")]
    public IEnumerable<AssetEstimateGet> AssetEstimateGet{ get; set; }
}

internal class AssetEstimateGet
{
    [JsonProperty("Token")]
    public string Token{ get; set; }

    [JsonProperty("Estimate_Get_Value")]
    public decimal EstimateGetValue{ get; set; }
}

internal class GetWithdrawalEstimateResp : Coin2PayBaseResp<IEnumerable<GetAddressData>>
{
    [JsonProperty("hostedPage")]
    public HostedPage HostedPage { get; set; }
}

internal class GetTokenPriceData
{
    public long UpdateTime { get; set; }

    public IEnumerable<PriceInfo> PriceList { get; set; }
}

internal class PriceInfo
{
    public string Token { get; set; }

    public string Network { get; set; }

    [JsonProperty("Price_Deposit")]
    public decimal PriceDeposit{ get; set; }

    [JsonProperty("Price_Withdraw")]
    public decimal PriceWithdrawal{ get; set; }

    [JsonProperty("Estimate_Given_Credit")]
    public decimal EstimateGivenCredit{ get; set; }
    
    [JsonProperty("Estimate_Withdrawal_Credit")]
    public decimal EstimateWithdrawalCredit{ get; set; }
}

internal class GetTokenPriceResp : Coin2PayBaseResp<GetTokenPriceData>
{
}


internal class GetReceptionTokenReq
{
    /// <summary>
    /// 與商戶結算幣種
    /// </summary>
    [JsonProperty("marked_currency")]
    public string MarkedCurrency { get; set; }

    /// <summary>
    /// 與商戶結算金額
    /// </summary>
    [JsonProperty("marked_price")]
    public string MarkedPrice { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    [JsonProperty("remark")]
    public string Remark { get; set; }

    /// <summary>
    /// 回調地址
    /// </summary>
    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }
}

internal class PayoutResp
{
    [JsonProperty("error")]
    public int Error { get; set; }

    /// <summary>
    /// 交易編號（商戶自訂 11 位數字）
    /// </summary>
    [JsonProperty("req_payout_id")]
    public int ReqPayoutId { get; set; }

    /// <summary>
    /// 第三方單號
    /// </summary>
    [JsonProperty("res_payout_id")]
    public string ResPayoutId { get; set; }
}

internal class PayoutQueryResp
{
    [JsonProperty("error")]
    public int Error { get; set; }

    /// <summary>
    /// 0: 處理中, 1: 交易取消, 2: 出幣成功
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; }

    /// <summary>
    /// 區塊鏈交易 Id
    /// </summary>
    [JsonProperty("txid")]
    public string TxId { get; set; }
}

internal class DepositCallBackReq
{
    [JsonProperty("uid")]
    public string TransId { get; set; }

    [JsonProperty("txid")]
    public string ThirdId { get; set; }

    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("amount_in_usdt")]
    public decimal AmountInUSDT { get; set; }

    [JsonProperty("quantity")]
    public decimal Quantity { get; set; }
    
    [JsonProperty("given_Credit")]
    public decimal GivenCredit { get; set; }
    
    [JsonProperty("credit_Rate")]
    public decimal CreditRate { get; set; }
    
    [JsonProperty("currency")]
    public string Currency { get; set; }

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}