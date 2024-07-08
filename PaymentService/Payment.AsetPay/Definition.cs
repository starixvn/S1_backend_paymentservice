using PaymentCore;

namespace Payment.AsetPay;

public class Definition
{
    private static Dictionary<int, TransStatus> DepositStatusMap =>
        new()
        {
            { 1, TransStatus.Success },
            { -1, TransStatus.Fail }
        };

    public static TransStatus GetDepositTransStatus(int status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "S", TransStatus.Success },
            { "F", TransStatus.Fail }
        };

    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    public const string SuccessCode = "00";
}

internal class CallbackResponse
{
    public const string Failed = "failed";
    public const string Success = "success";
}

internal class Path
{
    public const string Deposit = "/pay/order.aspx";

    public const string Withdrawal = "/betdf/df.aspx";
}
