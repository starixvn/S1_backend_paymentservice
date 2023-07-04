using PaymentCore;

namespace Payment.DirePay
{
    internal class DirePayDefinition
    {
        private static Dictionary<string, TransStatus> FiatPayinStatusMap =>
            new ()
            {
                { DirePayFiatPaymentStatus.Completed, TransStatus.Success },
                { DirePayFiatPaymentStatus.Expired, TransStatus.Fail },
                { DirePayFiatPaymentStatus.Rejected, TransStatus.Fail }
            };
        
        private static Dictionary<string, TransStatus> FiatPayoutStatusMap =>
            new ()
            {
                { DirePayFiatPaymentStatus.Completed, TransStatus.Success },
                { DirePayFiatPaymentStatus.Rejected, TransStatus.Fail },
                { DirePayFiatPaymentStatus.Refunded, TransStatus.Fail }
            };

        private static Dictionary<string, TransStatus> CryptoPayinStatusMap =>
            new ()
            {
                { DirePayCryptoPaymentStatus.Completed, TransStatus.Success },
                { DirePayCryptoPaymentStatus.Pending, TransStatus.Pending },
                { DirePayCryptoPaymentStatus.Expired, TransStatus.Fail },
                { DirePayCryptoPaymentStatus.TooLittle, TransStatus.Success },
                { DirePayCryptoPaymentStatus.TooMuch, TransStatus.Success },
            };
        
        private static Dictionary<string, TransStatus> CryptoPayoutStatusMap =>
            new ()
            {
                { DirePayCryptoPaymentStatus.Completed, TransStatus.Success },
                { DirePayCryptoPaymentStatus.Pending, TransStatus.Pending },
                { DirePayCryptoPaymentStatus.Rejected, TransStatus.Fail },
            };
        
        public static TransStatus GetFiatPayinTransStatus(string status)
            => FiatPayinStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    
        public static TransStatus GetFiatPayoutTransStatus(string status)
            => FiatPayoutStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
        
        public static TransStatus GetCryptoPayinTransStatus(string status)
            => CryptoPayinStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    
        public static TransStatus GetCryptoPayoutTransStatus(string status)
            => CryptoPayoutStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    }
    
    internal class DirePayFiatPaymentStatus
    {
        public const string Completed = "completed";

        public const string Rejected = "rejected";

        public const string Expired = "expired";
        
        public const string Refunded = "refunded";
    }
    
    internal class DirePayCryptoPaymentStatus
    {
        public const string Completed = "completed";

        public const string Pending = "pending";

        public const string Rejected = "rejected";

        public const string Expired = "expired";
        
        public const string TooLittle = "too_little";
        
        public const string TooMuch = "too_much";
    }

    internal class ErrorCode
    {
        public const string Success = "0";
    }

    internal class Path
    {
        public const string Login = "/api/login";
        
        public const string Action = "/api/callBack";
    }
    
    internal class Command
    {
        public const string FiatWithdrawal = "fiat_withdrawal";
        public const string PartnerWithdraw = "partner_withdraw";
        public const string Payment = "payment";
        public const string FiatPayment = "fiat_payment";
        public const string Rates = "rates";
    }
}
