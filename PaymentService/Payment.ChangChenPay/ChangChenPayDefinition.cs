using PaymentCore;

namespace Payment.ChangChenPay;

public class ChangChenPayDefinition
{
    private static Dictionary<string, TransStatus> PayinTransStatusMap =>
        new ()
        {
            { ChangChenPayinStatus.Completed, TransStatus.Success },
            { ChangChenPayinStatus.Pending, TransStatus.Pending },
            { ChangChenPayinStatus.Processing, TransStatus.Pending },
            { ChangChenPayinStatus.Success, TransStatus.Success },
            { ChangChenPayinStatus.Failed, TransStatus.Fail }
        };

    public static TransStatus GetPayinTransStatus(string status)
        => PayinTransStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
    
    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new ()
        {
            { ChangChenPayoutStatus.Completed, TransStatus.Success },
            { ChangChenPayoutStatus.Pending, TransStatus.Pending },
            { ChangChenPayoutStatus.Processing, TransStatus.Pending },
            { ChangChenPayoutStatus.Processing5, TransStatus.Pending },
            { ChangChenPayoutStatus.Failed, TransStatus.Fail }
        };
    
    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutTransStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
    
    internal class Path
    {
        /// <summary>
        /// 創建代收
        /// </summary>
        public const string CreateOrder = "/pay/create_order";

        /// <summary>
        /// 創建代付
        /// </summary>
        public const string CreatePayout = "/trans/create_order";
    }

    internal const string SuccessCode = "SUCCESS";
    internal const string CallbackResponse = "success";
}

internal class ChangChenPayinStatus
{
    public const string Pending = "0";

    public const string Processing = "1";

    public const string Completed = "2";

    public const string Success = "3";
    
    public const string Failed = "5";
}

internal class ChangChenPayoutStatus
{
    public const string Pending = "0";

    public const string Processing = "1";

    public const string Completed = "2";

    public const string Failed = "3";
    
    public const string Processing5 = "5";
}