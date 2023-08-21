using PaymentCore;

namespace Payment.OnePay;

public class OnePayDefinition
{
    private static Dictionary<int, TransStatus> TransStatusMap =>
        new()
        {
            { OnePayPayStatus.Success, TransStatus.Success },
            { OnePayPayStatus.Pending, TransStatus.Pending },
            { OnePayPayStatus.Failed, TransStatus.Fail }
        };

    public static TransStatus GetTransStatus(int status)
    {
        if (TransStatusMap.TryGetValue(status, out var result))
            return result;
        return TransStatus.Pending;
    }
}

internal class Path
{
    /// <summary>
    /// 推送入款单
    /// </summary>
    public const string CreatePay = "/order/receive";

    /// <summary>
    /// 推送出款单
    /// </summary>
    public const string CreatePayout = "/order/out";

    /// <summary>
    /// 订单查询
    /// </summary>
    public const string QueryOrderStatus = "/order/query";
}

internal class OnePayPayStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    public const int Pending = 1;

    /// <summary>
    /// 成功
    /// </summary>
    public const int Success = 2;

    /// <summary>
    /// 失败
    /// </summary>
    public const int Failed = 3;
}