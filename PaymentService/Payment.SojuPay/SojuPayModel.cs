using Newtonsoft.Json;
using PaymentCore.Models.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.SojuPay
{
    internal class SojuPayModel
    {
        public class DepositResponse
        {
            [JsonProperty("error_code")]
            public string ErrorCode { get; set; }
            [JsonProperty("error_msg")]
            public string ErrorMessage { get; set; }
            [JsonProperty("data")]
            public Data Data { get; set; }
        }

        public class WithdrawalResponse
        {
            [JsonProperty("error_code")]
            public string ErrorCode { get; set; }
            [JsonProperty("error_msg")]
            public string ErrorMessage { get; set; }
            [JsonProperty("date")]
            public Data Data { get; set; }
        }


        public class Data
        {
            [JsonProperty("payout_id")]
            public string PayoutId { get; set; }
            [JsonProperty("link")]
            public string Link { get; set; }
            [JsonProperty("register_link")]
            public string RegisterLink { get; set; }
        }
    }
}
