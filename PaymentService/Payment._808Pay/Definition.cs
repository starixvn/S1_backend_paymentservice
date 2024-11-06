using PaymentCore;

namespace Payment._808Pay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositStatusMap =>
        new()
        {
            { "3", TransStatus.Success }
        };

    public static TransStatus GetDepositTransStatus(string status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);

    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "2", TransStatus.Success },
            { "5", TransStatus.Fail }
        };

    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);

    public const string SuccessCode = "0";
}

internal class CallbackResponse
{
    public const string Success = "success";
}

internal class Path
{
    public const string Deposit = "/placeOrder";

    public const string Withdrawal = "/withdrawOrder";
}
