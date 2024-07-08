using PaymentCore;

namespace Payment.OneDayPay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositTransStatusMap =>
        new()
        {
            { "6", TransStatus.Success },
            { "7", TransStatus.Fail }
        };
    public static TransStatus GetDepositTransStatus(string status)
        => DepositTransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new()
        {
            { "4", TransStatus.Success },
            { "5", TransStatus.Fail }
        };
    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutTransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
}


internal class Path
{
    public const string Deposit = "/api/Deposit/SubmitDeposit";

    public const string Withdrawal = "/api/Withdrawal/SubmitWithdrawal";
}


internal class CallbackResponse
{
    public const string Success = "success";
}