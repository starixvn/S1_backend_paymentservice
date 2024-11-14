using PaymentCore;

namespace Payment.BigPayz;

public class Definition
{
    public const int SuccessCode = 0;
    
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { "1", TransStatus.Success },
            { "2", TransStatus.Fail },
            { "3", TransStatus.Success }
        };

    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
}