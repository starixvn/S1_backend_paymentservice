using PaymentCore;

namespace Payment.WPPay;

public class WPPayDefinition
{
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { WPPayTradeStatus.DepositSuccess, TransStatus.Success },
            { WPPayTradeStatus.DepositPending, TransStatus.Pending },
            { WPPayTradeStatus.DepositFailed, TransStatus.Fail },
            { WPPayTradeStatus.WithdrawalSuccess, TransStatus.Success },
            { WPPayTradeStatus.WithdrawalPending, TransStatus.Pending },
            { WPPayTradeStatus.WithdrawalFailed, TransStatus.Fail },
        };

    public static TransStatus GetTransStatus(string status)
    {
        if (TransStatusMap.TryGetValue(status, out var result))
            return result;
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

internal class WPPayTradeStatus
{
    /// <summary>
    /// 支付成功
    /// </summary>
    public const string DepositSuccess = "PAYMENT_SUCCESS";

    /// <summary>
    /// 等待支付
    /// </summary>
    public const string DepositPending = "WAITING_PAYMENT";

    /// <summary>
    /// 支付失败
    /// </summary>
    public const string DepositFailed = "PAYMENT_FAILURE";

    /// <summary>
    /// 代付成功
    /// </summary>
    public const string WithdrawalSuccess = "WITHDRAWAL_SUCCESS";

    /// <summary>
    /// 等待代付
    /// </summary>
    public const string WithdrawalPending = "WAITING_WITHDRAWAL";

    /// <summary>
    /// 代付失败
    /// </summary>
    public const string WithdrawalFailed = "WITHDRAWAL_FAILURE";
}