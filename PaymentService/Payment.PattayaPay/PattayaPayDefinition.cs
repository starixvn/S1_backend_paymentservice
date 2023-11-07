using PaymentCore;

namespace Payment.PattayaPay;

internal class PattayaPayDefinition
{
    private static Dictionary<string, TransStatus> PayinTransStatusMap =>
        new()
        {
            { PattayaPayPayinStatus.Pending, TransStatus.Pending },
            { PattayaPayPayinStatus.Canceled, TransStatus.Fail },
            { PattayaPayPayinStatus.Timeout, TransStatus.Fail },
            { PattayaPayPayinStatus.Paid, TransStatus.Success }
        };

    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new()
        {
            { PattayaPayPayoutStatus.Pending, TransStatus.Pending },
            { PattayaPayPayoutStatus.Abnormal, TransStatus.Pending },
            { PattayaPayPayoutStatus.Processing, TransStatus.Pending },
            { PattayaPayPayoutStatus.Canceled, TransStatus.Fail },
            { PattayaPayPayoutStatus.Success, TransStatus.Success }
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
    public const string CreatePay = "/pay";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/transfer/apply";

    /// <summary>
    /// 查詢代收狀態
    /// </summary>
    public const string QueryOrderStatus = "/trade/query";

    /// <summary>
    /// 查詢代付狀態
    /// </summary>
    public const string QueryPayoutStatus = "/transfer/query";
}

internal class PattayaPayPayinStatus
{
    /// <summary>
    /// 待付款
    /// </summary>
    public const string Pending = "0";

    /// <summary>
    /// 已支付
    /// </summary>
    public const string Paid = "1";

    /// <summary>
    /// 已超時
    /// </summary>
    public const string Timeout = "2";

    /// <summary>
    /// 已撤銷
    /// </summary>
    public const string Canceled = "4";
}

internal class PattayaPayPayoutStatus
{
    /// <summary>
    /// 待處理
    /// </summary>
    public const string Pending = "0";

    /// <summary>
    /// 結算完成
    /// </summary>
    public const string Success = "1";

    /// <summary>
    /// 結算中
    /// </summary>
    public const string Processing = "2";

    /// <summary>
    /// 結算異常
    /// </summary>
    public const string Abnormal = "3";

    /// <summary>
    /// 交易取消
    /// </summary>
    public const string Canceled = "4";
}


internal class CallbackResponse
{
    public const string Success = "success";

    public const string Failed = "failed";
}