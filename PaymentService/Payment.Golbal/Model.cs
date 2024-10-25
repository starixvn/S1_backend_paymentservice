using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Golbal
{
    public class ApiResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }
    public class Data
    {
        [JsonProperty("merchantUid")]
        public string MerchantUid { get; set; }

        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("sysOrderNo")]
        public string SysOrderNo { get; set; }

        [JsonProperty("payUrl")]
        public string PayUrl { get; set; }

        [JsonProperty("qrcode")]
        public string Qrcode { get; set; }

        [JsonProperty("createTime")]
        public long CreateTime { get; set; }
        [JsonProperty("amount")]
        public string Amount { get; set; }
        [JsonProperty("accountNo")]
        public string AccountNo { get; set; }
        [JsonProperty("accountName")]
        public string AccountName { get; set; }
    }

    public class DepositCallBackRequest
    {
        [JsonProperty("merchantUid")]
        public string MerchantUid { get; set; }

        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("sysOrderNo")]
        public string SysOrderNo { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("tax")]
        public string Tax { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        [JsonProperty("payTime")]
        public string PayTime { get; set; }

        [JsonProperty("remark")]
        public string Remark { get; set; }

        [JsonProperty("extra")]
        public string Extra { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }
    }
}
