using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Payment.YPay
{
    public class DepositReq
    {
        [Description("Merchant ID")]
        public int appid { get; set; }
        
        [Required]
        [Description("user ipv4 or ipv6 address")]
        public string cliIP { get; set; }
        
        [Required]
        [Description("User equipment")]
        public string cliNA { get; set; }
        
        [Description("The unique identification string of the user who placed the order")]
        public string uid { get; set; }
        
        [Description("Merchant order id")]
        public string order { get; set; }
        
        [Description("amount")]
        public string price { get; set; }
        
        [Description("Name of third party payment or bank")]
        public string payType { get; set; }
        
        [Description("Callback url, doesn’t participate in encryption")]
        public string notifyUrl { get; set; }
        
        [Description("Signature verification value")]
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
}