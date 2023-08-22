using PaymentCore;

namespace Payment.WPPay;

public class WPPayDefinition
{
    private static Dictionary<string, TransStatus> PayinTransStatusMap =>
        new()
        {
            { WPPayPayinStatus.Paid, TransStatus.Success },
            { WPPayPayinStatus.Pending, TransStatus.Pending },
            { WPPayPayinStatus.Failed, TransStatus.Fail }
        };

    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new()
        {
            { WPPayPayoutStatus.Pending, TransStatus.Pending },
            { WPPayPayoutStatus.Abnormal, TransStatus.Pending },
            { WPPayPayoutStatus.Processing, TransStatus.Pending },
            { WPPayPayoutStatus.Failed, TransStatus.Fail },
            { WPPayPayoutStatus.Success, TransStatus.Success }
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
    /// 交易接口
    /// </summary>
    public const string CreatePay = "/deposit/apply";

    /// <summary>
    /// 代付接口
    /// </summary>
    public const string CreatePayout = "/withdrawal/apply";

    /// <summary>
    /// 支付(交易)狀態查詢
    /// </summary>
    public const string QueryOrderStatus = "/deposit/query";

    /// <summary>
    /// 代付狀態查詢
    /// </summary>
    public const string QueryPayoutStatus = "/withdrawal/query";
}

internal class WPPayPayinStatus
{
    /// <summary>
    /// 支付成功
    /// </summary>
    public const string Paid = "1";

    /// <summary>
    /// 待支付
    /// </summary>
    public const string Pending = "2";

    /// <summary>
    /// 支付失败
    /// </summary>
    public const string Failed = "3";
}

internal class WPPayPayoutStatus
{
    /// <summary>
    /// 代付成功
    /// </summary>
    public const string Success = "1";

    /// <summary>
    /// 代付中
    /// </summary>
    public const string Processing = "2";

    /// <summary>
    /// 代付失败
    /// </summary>
    public const string Failed = "3";

    /// <summary>
    /// 待确认
    /// </summary>
    public const string Pending = "4";

    /// <summary>
    /// 异常状态
    /// </summary>
    public const string Abnormal = "5";
}
