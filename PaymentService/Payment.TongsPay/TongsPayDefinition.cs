using PaymentCore;

namespace Payment.TongsPay;

public class TongsPayDefinition
{
    private static Dictionary<string, TransStatus> TongsPayinStatusMap =>
        new()
        {
            { "Success", TransStatus.Success },
            { "Fail", TransStatus.Fail }
        };
    
    private static Dictionary<string, TransStatus> TongsPayoutStatusMap =>
        new()
        {
            { "ChangeBankCard", TransStatus.Fail },
            { "FAIL", TransStatus.Fail },
            { "PAYING", TransStatus.Pending },
            { "SUCCESS", TransStatus.Success }
        };
    
    public const string CallbackResponse = "SUCCESS";

    public static TransStatus GetPayinTransStatus(string status)
        => TongsPayinStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
    
    public static TransStatus GetPayoutTransStatus(string status)
        => TongsPayoutStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
}

internal class Path
{
    public const string Deposit = "/gateway/bank";

    public const string Withdrawal = "/gateway/withdraw";
}
