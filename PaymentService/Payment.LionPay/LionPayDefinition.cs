using PaymentCore;

namespace Payment.LionPay;

internal class LionPayDefinition
{
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new ()
        {
            { LionPayOrderStatus.Completed, TransStatus.Success },
            { LionPayOrderStatus.Pending, TransStatus.Pending },
            { LionPayOrderStatus.Processing, TransStatus.Pending },
            { LionPayOrderStatus.Error, TransStatus.Pending },
            { LionPayOrderStatus.Timeout, TransStatus.Pending },
            { LionPayOrderStatus.Canceled, TransStatus.Fail },
            { LionPayOrderStatus.Rejected, TransStatus.Fail }
        };


    public static TransStatus GetTransStatus(string status)
    {
        if (TransStatusMap.TryGetValue(status, out var result))
        {
            return result;
        }

        return TransStatus.Pending;
    }
}

internal class Path
{
    /// <summary>
    /// 創建代收
    /// </summary>
    public const string CreateOrder = "/deposit";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/withdraw";
}

internal class LionPayErrorCode
{
    public const string Success = "SUCCESS";

    public const string Failed = "FAIL";
}

internal class LionPayOrderStatus
{
    public const string Processing = "PROCESSING";

    public const string Completed = "COMPLETED";

    public const string Pending = "PENDING";

    public const string Error = "ERROR";

    public const string Timeout = "TIMEOUT";

    public const string Canceled = "CANCELED";

    public const string Rejected = "REJECTED";
}

    
internal class CallbackResponse
{
    public const string Success = "SUCCESS";

    public const string Failed = "FAILED";
}