using PaymentCore;

namespace Payment.OOPay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositStatusMap =>
        new()
        {
            { "PAID", TransStatus.Success }
        };

    public static TransStatus GetDepositTransStatus(string status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);

    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "PAID", TransStatus.Success },
            { "CANCELLED", TransStatus.Fail }
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
    public const string Deposit = "/shou/new";

    public const string Withdrawal = "/fu/new";
}
