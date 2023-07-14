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

    //private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
    //    new()
    //    {
    //        { PattayaPayPayoutStatus.Pending, TransStatus.Pending },
    //        { PattayaPayPayoutStatus.Abnormal, TransStatus.Pending },
    //        { PattayaPayPayoutStatus.Processing, TransStatus.Pending },
    //        { PattayaPayPayoutStatus.Canceled, TransStatus.Fail },
    //        { PattayaPayPayoutStatus.Success, TransStatus.Success }
    //    };

    public static TransStatus GetDepositTransStatus(string status)
    {
        if (PayinTransStatusMap.TryGetValue(status, out var result))
        {
            return result;
        }

        return TransStatus.Pending;
    }

    //public static TransStatus GetPayoutTransStatus(string status)
    //{
    //    if (PayoutTransStatusMap.TryGetValue(status, out var result))
    //    {
    //        return result;
    //    }

    //    return TransStatus.Pending;
    //}
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
