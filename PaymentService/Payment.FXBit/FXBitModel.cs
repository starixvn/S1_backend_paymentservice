using Newtonsoft.Json;

namespace Payment.FXBit
{
    internal class FxBitExchangeRate
    {
        /// <summary>
        /// 匯率更新時間戳
        /// </summary>
        [JsonProperty("updated_timestamp")]
        public DateTimeOffset UpdatedTimestamp { get; set; }

        /// <summary>
        /// 買入匯率
        /// </summary>
        [JsonProperty("buyRate")]
        public string BuyRate { get; set; }

        /// <summary>
        /// 賣出匯率
        /// </summary>
        [JsonProperty("sellRate")]
        public string SellRate { get; set; }
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
    

    internal class GetWalletAddressResp
    {
        [JsonProperty("public_address")]
        public string PublicAddress { get; set; }

        [JsonProperty("billed_price")]
        public decimal BilledPrice { get; set; }

        [JsonProperty("index")]
        public string Index { get; set; }

        [JsonProperty("rate")]
        public string Rate { get; set; }

        [JsonProperty("receipt_token")]
        public string Expire { get; set; }

        [JsonProperty("error")]
        public int Error { get; set; }

        [JsonProperty("error_message")]
        public int ErrorMessage { get; set; }
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

    internal class GetReceptionTokenResp
    {
        /// <summary>
        /// 店鋪標示
        /// </summary>
        [JsonProperty("shop_identifier")]
        public string ShopIdentifier { get; set; }

        /// <summary>
        /// 交易編號
        /// </summary>
        [JsonProperty("receipt_token")]
        public string ReceiptToken { get; set; }

        [JsonProperty("error")]
        public int Error { get; set; }

        [JsonProperty("err")]
        public int ErrorMessage { get; set; }
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
        [JsonProperty("receipt_token")]
        public string ReceiptToken { get; set; }

        [JsonProperty("marked_currency")]
        public string MarkedCurrency { get; set; }

        [JsonProperty("marked_price")]
        public decimal MarkedPrice { get; set; }

        [JsonProperty("billed_currency")]
        public string BilledCurrency { get; set; }

        [JsonProperty("billed_price")]
        public string BilledPrice { get; set; }

        [JsonProperty("received_amount")]
        public decimal ReceivedAmount { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}
