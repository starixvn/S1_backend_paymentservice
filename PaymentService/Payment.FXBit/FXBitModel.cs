using System;
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
        public string BilledPrice { get; set; }

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
        public decimal MarkedPrice { get; set; }

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
    

    internal class CreditReq
    {
        public string receipt_token { get; set; }
    }

    internal class CreditResp
    {
        public string error { get; set; }
        public string receipt_token { get; set; }
        public string shop_name { get; set; }
        public string marked_currency { get; set; }
        public string marked_price { get; set; }
        public string billed_currency { get; set; }
        public string billed_price { get; set; }
        public string received_amount { get; set; }
        public string payment_status { get; set; }
        public string net { get; set; }
        public string fee { get; set; }
        public string transaction_hash { get; set; }
        public string from { get; set; }
    }

    internal class DepositCallBackReq
    {
        [JsonProperty("receipt_token")]
        public string ReceiptToken { get; set; }

        [JsonProperty("marked_currency")]
        public string MarkedCurrency { get; set; }

        [JsonProperty("marked_price")]
        public string MarkedPrice { get; set; }

        [JsonProperty("billed_currency")]
        public string BilledCurrency { get; set; }

        [JsonProperty("billed_price")]
        public string BilledPrice { get; set; }

        [JsonProperty("received_amount")]
        public string ReceivedAmount { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}
