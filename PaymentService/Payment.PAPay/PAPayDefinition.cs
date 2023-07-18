using PaymentCore;

namespace Payment.PAPay;

public class PAPayDefinition
{
    private static Dictionary<string, TransStatus> PayinTransStatusMap =>
        new()
        {
            { PAPayPayinStatus.Paid, TransStatus.Success },
            { PAPayPayinStatus.Pending, TransStatus.Pending },
            { PAPayPayinStatus.Failed, TransStatus.Fail }
        };

    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new()
        {
            { PAPayPayoutStatus.Pending, TransStatus.Pending },
            { PAPayPayoutStatus.Abnormal, TransStatus.Pending },
            { PAPayPayoutStatus.Processing, TransStatus.Pending },
            { PAPayPayoutStatus.Failed, TransStatus.Fail },
            { PAPayPayoutStatus.Success, TransStatus.Success }
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
    public const string CreatePay = "/lh_pay/pay";

    /// <summary>
    /// 代付接口
    /// </summary>
    public const string CreatePayout = "/lh_daifu/agent";

    /// <summary>
    /// 支付(交易)狀態查詢
    /// </summary>
    public const string QueryOrderStatus = "/lh_pay/order_query";

    /// <summary>
    /// 代付狀態查詢
    /// </summary>
    public const string QueryPayoutStatus = "/lh_daifu/agent_query";
}

internal class PAPayPayinStatus
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

internal class PAPayPayoutStatus
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
