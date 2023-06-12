using PaymentCore;

namespace Payment.Coin2Pay;

internal class Coin2PayDefinition
{
    public static Dictionary<string, TransStatus> DepositTransStatusMap =>
        new ()
        {
            { Coin2PayDepositStatus.New, TransStatus.Pending },
            { Coin2PayDepositStatus.Confirming, TransStatus.Pending },
            { Coin2PayDepositStatus.Confirmed, TransStatus.Success },
            { Coin2PayDepositStatus.Failed, TransStatus.Fail },
            { Coin2PayDepositStatus.Insufficient, TransStatus.Success },
            { Coin2PayDepositStatus.Expired, TransStatus.Fail },
            { Coin2PayDepositStatus.IncorrectAmount, TransStatus.Success }
        };

    public static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new ()
        {
            { Coin2PayPayoutStatus.Pending, TransStatus.Pending },
            { Coin2PayPayoutStatus.Cancel, TransStatus.Fail },
            { Coin2PayPayoutStatus.Success, TransStatus.Success }
        };

    public static Dictionary<int, string> PayoutErrorMessageMap =>
        new Dictionary<int, string>()
        {
            { 0, "成功" },
            { 10001, "Header api_key 為空" },
            { 10002, "Header api_key 錯誤" },
            { 10003, "請求路徑錯誤" },
            { 10004, "請求類型錯誤" },
            { 20001, "幣種錯誤" },
            { 20002, "time_stamp 參數錯誤" },
            { 20003, "簽名錯誤" },
            { 20004, "payout id 錯誤（查單）" },
            { 20005, "地址錯誤" },
            { 20006, "金額錯誤" },
            { 20007, "備註太長（600 字元以內）" },
            { 20008, "req_payout_id 錯誤" },
            { 20009, "Payout IP 不在白名單範圍,請聯系管理員添加 IP 白名單" },
            { 20010, "禁止重復提交" },
            { 20011, "餘額不足" },
            { 20012, "申請支付的金額低於最少出金額" },
            { 30000, "請聯系管理員開啟 Payout Api 使用權限" }
        };

    public static TransStatus GetDepositTransStatus(string status)
    {
        if (DepositTransStatusMap.TryGetValue(status, out var result))
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

    public static string GetPayoutErrorMessage(int errorCode)
    {
        if (PayoutErrorMessageMap.TryGetValue(errorCode, out var result))
        {
            return result;
        }

        return string.Empty;
    }
}

internal class Coin2PayDepositStatus
{
    /// <summary>
    /// 尚未入帳
    /// </summary>
    public const string New = "0";

    /// <summary>
    /// 監控到有金額入帳
    /// </summary>
    public const string Confirming = "1";

    /// <summary>
    /// 訂單完成
    /// </summary>
    public const string Confirmed = "2";

    /// <summary>
    /// 交易錯誤
    /// </summary>
    public const string Failed = "3";

    /// <summary>
    /// 入帳金額不足
    /// </summary>
    public const string Insufficient = "4";

    /// <summary>
    /// 訂單已過期
    /// </summary>
    public const string Expired = "5";

    /// <summary>
    /// 入帳金額超出
    /// </summary>
    public const string IncorrectAmount = "6";

    /// <summary>
    /// 訂單過期後收到入金
    /// </summary>
    public const string PayInAfterExpired = "7";
}

internal class Coin2PayPayoutStatus
{
    /// <summary>
    /// 處理中
    /// </summary>
    public const string Pending = "0";

    /// <summary>
    /// 交易取消
    /// </summary>
    public const string Cancel = "1";

    /// <summary>
    /// 出幣完成
    /// </summary>
    public const string Success = "2";
}

internal class ErrorCode
{
    public const string Success = "0";

    public const string GetExchangeRateFailed = "9900";

    public const string GetReceptionTokenFailed = "9901";

    public const string GetWalletAddressFailed = "9902";

    public const string PayoutFailed = "9903";
}

internal class MediaType
{
    public const string Json = "application/json";
}

internal class Path
{
    /// <summary>
    /// 取得匯率
    /// </summary>
    public const string GetExchangeRate = "/v1/GetCoinPrice.ashx";
    
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