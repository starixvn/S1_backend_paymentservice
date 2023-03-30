using PaymentCore;

namespace Payment.DelhiPay;

internal class DelhiPayDefinition
{
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new ()
        {
            { DelhiPayOrderStatus.Completed, TransStatus.Success },
            { DelhiPayOrderStatus.Pending, TransStatus.Pending },
            { DelhiPayOrderStatus.Processing, TransStatus.Pending },
            { DelhiPayOrderStatus.Error, TransStatus.Pending },
            { DelhiPayOrderStatus.Timeout, TransStatus.Pending },
            { DelhiPayOrderStatus.Canceled, TransStatus.Fail },
            { DelhiPayOrderStatus.Rejected, TransStatus.Fail }
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

internal class DelhiPayErrorCode
{
    public const string Success = "SUCCESS";

    public const string Failed = "FAIL";
}

internal class DelhiPayOrderStatus
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