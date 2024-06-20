using PaymentCore;

namespace Payment.PapayaPay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositStatusMap =>
        new()
        {
            { "00_Initiated", TransStatus.Pending },
            { "01_Pending", TransStatus.Pending },
            { "02_Paid", TransStatus.Success },
            { "03_Rejected", TransStatus.Fail },
            { "04_Refunded", TransStatus.Fail },
            { "05_Canceled", TransStatus.Fail }
        };

    public static TransStatus GetDepositTransStatus(string status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "INITIATED", TransStatus.Pending },
            { "PROCESSING", TransStatus.Pending },
            { "PAID", TransStatus.Success },
            { "REJECTED", TransStatus.Fail },
            { "ERROR", TransStatus.Fail }
        };

    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    public const string SuccessCode = "200";
}

internal class CallbackResponse
{
    public const string Success = "success";
}

internal class Path
{
    public const string Deposit = "/api/v2/create-qr-payment";

    public const string Withdrawal = "/api/v2/create-fundout";
}
