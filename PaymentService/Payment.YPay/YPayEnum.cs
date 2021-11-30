using System.ComponentModel;

namespace Payment.YPay
{
    public enum DepositStatus
    {
        Success = 0
    }

    public enum DepositCallbackStatus
    {
        Closed = 0,
        
        [Description("Pending")]
        WaitingForPayment = 1,
        
        Reminded = 2,
        
        [Description("Success")]
        Paid = 3,
        
        [Description("Success")]
        CallBacked = 5,
        
        [Description("Pending")]
        TimedOut = 6
    }
}