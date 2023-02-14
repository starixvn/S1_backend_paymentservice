using PaymentCore;

namespace Payment.WPay;

internal class WPayDefinition
{
    private static Dictionary<string, TransStatus> PayinTransStatusMap =>
        new ()
        {
            { WPayPayinStatus.Pending, TransStatus.Pending },
            { WPayPayinStatus.ManualPaid, TransStatus.Success },
            { WPayPayinStatus.Paid, TransStatus.Success }
        };

    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new ()
        {
            { WPayPayoutStatus.Pending, TransStatus.Pending },
            { WPayPayoutStatus.Cancel, TransStatus.Fail },
            { WPayPayoutStatus.Reversed, TransStatus.Fail },
            { WPayPayoutStatus.Success, TransStatus.Success }
        };

    public static TransStatus GetDepositTransStatus(string status)
    {
        if (PayinTransStatusMap.TryGetValue(status, out var result))
        {
            return result;
        }

        return TransStatus.Pending;
    }
    
    public static TransStatus GetPayoutTransStatus(string status)
    {
        if (PayoutTransStatusMap.TryGetValue(status, out var result))
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
    public const string CreateOrder = "/order/create";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/order/payout_create";

    /// <summary>
    /// 查詢代收狀態
    /// </summary>
    public const string QueryOrderStatus = "/order/status";

    /// <summary>
    /// 查詢代付狀態
    /// </summary>
    public const string QueryPayoutStatus = "/order/payout_status";
}

internal class WPayPayinStatus
{
    /// <summary>
    /// 已付
    /// </summary>
    public const string Paid = "PAID";

    /// <summary>
    /// 已補單
    /// </summary>
    public const string ManualPaid = "MANUAL PAID";

    /// <summary>
    /// 待付款
    /// </summary>
    public const string Pending = "PENDING";
}

internal class WPayPayoutStatus
{
    /// <summary>
    /// 待處理
    /// </summary>
    public const string Pending = "PENDING";

    /// <summary>
    /// 交易取消
    /// </summary>
    public const string Cancel = "CANCELLED";

    /// <summary>
    /// 已沖正
    /// </summary>
    public const string Reversed = "REVERSED";

    /// <summary>
    /// 完成
    /// </summary>
    public const string Success = "PAID";
}

    
internal class CallbackResponse
{
    public const string Success = "success";

    public const string Failed = "failed";
}