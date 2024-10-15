using PaymentCore;

namespace Payment.ZiuPay;

internal class Definition
{
    public const int SuccessCode = 0;
    
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { "approve", TransStatus.Success },
            { "reject", TransStatus.Fail }
        };
    
    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);

}

internal class Path
{
    public const string Deposit = "/transaction/deposit/request";

    public const string Withdrawal = "/transaction/withdraw/request";
}