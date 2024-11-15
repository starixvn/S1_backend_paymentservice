using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.K2Pay
{
    public class DepositReq
    {
        public int appid { get; set; }

        public string uid { get; set; }

        public string order { get; set; }

        public string price { get; set; }

        public string payType { get; set; }

        public string notifyUrl { get; set; }

        public string sn { get; set; }
    }
    public class DepositRes
    {
        [Description("0 Return for normal，other non 0 value is wrong")]
        public int e { get; set; }

        [Description("Error message")]
        public string m { get; set; }

        public DepositResData d { get; set; }
    }
    public class DepositResData
    {
        [Description("QR code content (scan code channel)")]
        public string qr { get; set; }

        [Description("Bank card number (transfer channel)")]
        public string account { get; set; }

        [Description("Bank name (transfer channel)")]
        public string bankType { get; set; }

        [Description("Cardholder's name (transfer channel)")]
        public string accountName { get; set; }

        [Description("The payment page shown to paying users")]
        public string h5 { get; set; }
    }
    public class WithdrawalReq
    {
        [Required]
        public int appid { get; set; }

        [Required]
        public string orderId { get; set; }

        [Required]
        [Description("Name of bank account holder")]
        public string name { get; set; }

        [Required]
        [Description("Withdraw amount")]
        public string money { get; set; }

        [Required]
        [Description("Abbreviation of bank name")]
        public string bankMark { get; set; }

        [Required]
        [Description("Bank card number")]
        public string recAcc { get; set; }

        [Description("Remarks (not required)")]
        public string remark { get; set; }

        [Required]
        [Description("Callback url,don’t participate in encryption, no callback if you don’t pass ")]
        public string notifyUrl { get; set; }

        [Required]
        [Description("Signature verification value")]
        public string sn { get; set; }
    }
    public class WithdrawalRes
    {
        [Description("0 is normal,-1 is error")]
        public int code { get; set; }

        public dynamic data { get; set; }

        [Description("Error message")]
        public string desc { get; set; }
    }
    public class CallBackCreditReq
    {
        [Description("Order Id")]
        public string order { get; set; }

        [Description("Amount")]
        public decimal amount { get; set; }

        [Description("Processing Timestamp")]
        public string completeTime { get; set; }

        [Description("Order status (completed: 4 rejected: 5)")]
        public int status { get; set; }

        [Description("Signature verification value")]
        public string sn { get; set; }

        public string remark { get; set; }
    }
    public class CallBackDebitReq
    {
        [Description("The passed order id string placed by the merchant")]
        public string order { get; set; }

        [Description("User ID passed when the merchant places an order")]
        public string uid { get; set; }

        [Description("Please provide empty string")]
        public string gid { get; set; }

        [Description("The amount paid in IDR")]
        public decimal amount { get; set; }

        [Description("Platform order serial number")]
        public string transaction_id { get; set; }

        [Description("Signature verification value, same as the order interface algorithm")]
        public string sn { get; set; }
    }
    public class DepositInfo
    {
        public string BankCode { get; set; }
    }
    public enum DepositStatus
    {
        Success = 0
    }
    public enum WithdrawalStatus
    {
        Success = 0
    }
    public enum WithdrawalCallbackStatus
    {
        Completed = 4,
        Rejected = 5
    }
}
