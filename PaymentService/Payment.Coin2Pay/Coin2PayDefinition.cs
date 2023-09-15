namespace Payment.Coin2Pay;

internal class ErrorCode
{
    public const string Success = "0";
}

internal class Path
{
    /// <summary>
    /// 取得提款匯率
    /// </summary>
    public const string GetWithdrawalEstimate = "/v1/getWithdrawalEstimateValue.ashx";

    /// <summary>
    /// 取得錢包地址
    /// </summary>
    public const string GetAddress = "/v1/GetAddress.ashx";

    /// <summary>
    /// 提款
    /// </summary>
    public const string RequestWithdrawal = "/v1/RequestWithdraw.ashx";

    /// <summary>
    /// 餘額
    /// </summary>
    public const string CheckBalance = "/v1/CheckBalance.ashx";

    /// <summary>
    /// 查詢交易狀態
    /// </summary>
    public const string TransactionStatus = "/v1/TransactionStatus.ashx";
}

internal class CallbackResponse
{
    public const string Success = "success";

    public const string Failed = "failed";
}