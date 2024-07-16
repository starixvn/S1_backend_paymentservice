using PaymentCore;

namespace Payment.S88Pay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositStatusMap =>
        new()
        {
            { "1", TransStatus.Pending },
            { "2", TransStatus.Success },
            { "3", TransStatus.Fail },
            { "4", TransStatus.Pending }
        };

    public static TransStatus GetDepositTransStatus(string status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "1", TransStatus.Pending },
            { "2", TransStatus.Success },
            { "3", TransStatus.Fail }
        };

    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);
}

internal class CallbackResponse
{
    public const string Success = "success";
}

internal class Path
{
    public const string Deposit = "/v2/dopayment";

    public const string Withdrawal = "/api/v1/payout";
}
