using PaymentCore;

namespace Payment.XPay;

public class Definition
{
    internal static string GenerateCallbackResponse(string billNo)
    {
        return $"<xml><billno>{billNo}</billno><status>OK</status></xml>";
    }

    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            // Deposit and Withdrawal Success
            { "000", TransStatus.Success },
            
            // Deposit and Withdrawal Fail
            { "111", TransStatus.Fail },
            
            // Deposit and Withdrawal Pending
            { "001", TransStatus.Pending },
            
            // Withdrawal Reject
            { "112", TransStatus.Fail }
        };


    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
}

internal class Path
{
    public const string CreateOrder = "/fundtransfer.php";

    public const string CreatePayout = "/withdrawal.php";
}