using PaymentCore;

namespace Payment.BetcatPay;

public class Definition
{
    /// <summary>
    /// 0=生成訂單，1=支付中，2=支付未通知，3=支付已通知，-1=交易失敗，-2=交易過期，-3=交易退還，-4=交易異常
    /// </summary>
    private static Dictionary<int, TransStatus> StatusMap =>
        new()
        {
            { 0, TransStatus.Pending },
            { 1, TransStatus.Pending },
            { 2, TransStatus.Success },
            { 3, TransStatus.Success },
            { -1, TransStatus.Fail },
            { -2, TransStatus.Fail },
            { -3, TransStatus.Fail },
            { -4, TransStatus.Fail }
        };

    public static TransStatus GetTransStatus(int status)
        => StatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
    
    public const string SuccessCode = "0";
}

internal class CallbackResponse
{
    public const string Success = "success";
}

internal class Path
{
    public const string Deposit = "/api/v1/payment/order/create";

    public const string Withdrawal = "/api/v1/payout/order/create";
}
