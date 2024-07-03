using Newtonsoft.Json;

namespace Payment.PayPro
{
    public class CreateOrderResponse
    {
        [JsonProperty("status")] 
        public string Status { get; set; }
        [JsonProperty("transactionId")] 
        public string TransactionId { get; set; }
        [JsonProperty("referenceId")] 
        public string ReferenceId { get; set; }
        [JsonProperty("referenceNo")] 
        public string ReferenceNo { get; set; }
        [JsonProperty("redirectUrl")] 
        public string RedirectUrl { get; set; }
        [JsonProperty("designatedBankRemark")] 
        public string DesignatedBankRemark { get; set; }
        [JsonProperty("deposit_pin")] 
        public string DepositPin { get; set; }
        [JsonProperty("dateExpired")] 
        public DateTime DateExpired { get; set; }
        [JsonProperty("currency")] 
        public string Currency { get; set; }
        [JsonProperty("deposit_amount")] 
        public decimal DepositAmount { get; set; }
        [JsonProperty("signed")] 
        public string Signed { get; set; }
        [JsonProperty("errors")] 
        public string Errors { get; set; }
    }

    public class CreateOrderData
    {
        [JsonProperty("trade_no")] 
        public string TransId { get; set; }

        [JsonProperty("amount")] 
        public string Amount { get; set; }

        [JsonProperty("out_trade_no")] 
        public string ThirdId { get; set; }

        [JsonProperty("uri")] 
        public string Url { get; set; }

        [JsonProperty("qrcode")] 
        public string QrCode { get; set; }
    }

    public class CallBackDebitRequest
    {
        [JsonProperty("merchantId")]
        public string MerchantId { get; set; } 

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("message")]
        public decimal Message { get; set; } 
    }
    
    public class CallBackDebitData
    {
        [JsonProperty("referenceId")]
        public string MerchantId { get; set; } 
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("statusDesc")]
        public string StatusDesc { get; set; } 
        [JsonProperty("requestDate")]
        public string RequestDate { get; set; } 
        [JsonProperty("transactionType")]
        public string TransactionType { get; set; }
        [JsonProperty("transactionTypeDesc")]
        public decimal TransactionTypeDesc { get; set; } 
        [JsonProperty("currency")]
        public string Currency { get; set; } 
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        [JsonProperty("transactionDate")]
        public DateTime TransactionDate { get; set; } 
        [JsonProperty("remark")]
        public string Remark { get; set; } 
        [JsonProperty("designatedBank")]
        public DesignateBank DesignateBank { get; set; }
        [JsonProperty("transactionAmount")]
        public decimal TransactionAmount { get; set; } 
        [JsonProperty("serviceFee")]
        public decimal ServiceFee { get; set; } 
        [JsonProperty("adjustment")]
        public Adjustment Adjustment { get; set; }
        [JsonProperty("refundedReason")]
        public decimal RefundedReason { get; set; } 
    }

    public class DesignateBank
    {
        [JsonProperty("bankName")]
        public string BankName { get; set; }
    }

    public class Adjustment
    {
        [JsonProperty("refundedReason")]
        public string RefundedReason { get; set; }
        [JsonProperty("adjustedDate")]
        public DateTime AdjustedDate { get; set; }
        [JsonProperty("adjustedReason")]
        public string AdjustedReason { get; set; }
        [JsonProperty("serviceFeeBeforeAdjusted")]
        public decimal ServiceFeeBeforeAdjusted { get; set; }
    }

    public class CreatePayoutResponse
    {
        [JsonProperty("code")]
        public int code { get; set; } // 1表示成功，失败不会通知

        [JsonProperty("msg")]
        public string msg { get; set; } // 商户号
    }

    public class GetBankList
    {
        public string status { get; set; }
        public GetBankListResult result { get; set; }
        public string errors { get; set; }
    }

    public class GetBankListResult
    {
        public string merchantId { get; set; }
        public string currency { get; set; }
        public string payMethod { get; set; }
        public decimal amount { get; set; }
        public List<Banks> banks { get; set; }
    }

    public class Banks
    {
        public int bankId { get; set; }
        public string bankCode { get; set; }
        public string bankName { get; set; }
        public string bankLogo { get; set; }
    }
}