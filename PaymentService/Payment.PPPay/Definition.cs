using PaymentCore;

namespace Payment.PPPay;

public class Definition
{
    private static Dictionary<int, TransStatus> DepositStatusMap =>
        new()
        {
            { 1, TransStatus.Pending },
            { 2, TransStatus.Success },
            { 3, TransStatus.Pending },
            { 4, TransStatus.Fail },
            { 5, TransStatus.Fail },
            { 6, TransStatus.Success }
        };

    public static TransStatus GetDepositTransStatus(int status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    private static Dictionary<int, TransStatus> PayoutStatusMap =>
        new()
        {
            { 1, TransStatus.Pending },
            { 2, TransStatus.Pending },
            { 3, TransStatus.Success },
            { 4, TransStatus.Fail }
        };

    public static TransStatus GetPayoutTransStatus(int status)
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
    public const string Deposit = "/deposit/order";

    public const string Withdrawal = "/withdraw/order";
}
