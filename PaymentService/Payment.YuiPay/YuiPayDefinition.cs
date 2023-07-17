using PaymentCore;

namespace Payment.YuiPay;

internal class YuiPayDefinition
{
    public const string Version = "V2";
    public const string SingType = "MD5";
    
    public static Dictionary<string, TransStatus> YuiPayStatusMap =>
        new ()
        {
            { YuiPayStatus.Pending, TransStatus.Pending },
            { YuiPayStatus.Success, TransStatus.Success },
            { YuiPayStatus.Failed, TransStatus.Fail }
        };
    
    public const string CallbackResponse = "SUCCESS";
    
    public static TransStatus GetTransStatus(string status)
        => YuiPayStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;
}

internal class Path
{
    public const string Deposit = "/api/pay/V2";

    public const string Withdrawal = "/api/defray/V2";
}

internal class ErrorCode
{
    public const string Success = "0";
}

internal class YuiPayStatus
{
    public const string Success = "1";
    public const string Failed = "2";
    public const string Pending = "0";
}

internal class WithdrawalQueryStatus
{
    /// <summary>
    /// 申請中
    /// </summary>
    public const string New = "0";
    
    /// <summary>
    /// 處理中
    /// </summary>
    public const string Pending = "1";

    /// <summary>
    /// 完成
    /// </summary>
    public const string Success = "2";

    /// <summary>
    /// 已取消
    /// </summary>
    public const string Cancel = "3";
}

internal class PayType
{
    public const string OnlineBanking = "1";
}
