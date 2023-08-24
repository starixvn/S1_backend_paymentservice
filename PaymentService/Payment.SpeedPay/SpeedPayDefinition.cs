using PaymentCore;

namespace Payment.SpeedPay;

internal class SpeedPayDefinition
{
    private static Dictionary<string, TransStatus> SpeedPayStatusMap =>
        new ()
        {
            { SpeedPayStatus.Success, TransStatus.Success },
            { SpeedPayStatus.Failed, TransStatus.Fail }
        };
    
    private static Dictionary<string, string> SpeedPayCurrencyMap =>
        new ()
        {
            { "2", "MYR" },
            { "4", "THB" }
        };
    
    public const string Version = "1.0";
    public const string CallbackResponse = "OK";
    public const string SuccessCode = "000";
    public const string TransSuccessCode = "0000";
    
    public static TransStatus GetTransStatus(string status)
        => SpeedPayStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
    
    public static string GetPayCurrency(string currencyId)
        => SpeedPayCurrencyMap.TryGetValue(currencyId, out var result)
            ? result : string.Empty;
    
    private class SpeedPayStatus
    {
        public const string Success = "SUCCESS";
        public const string Failed = "FAIL";
    }
    
    public class Path
    {
        public const string Deposit = "/qrh5";

        public const string Payout = "/payout";
    }
}
