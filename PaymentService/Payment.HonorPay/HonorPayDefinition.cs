using PaymentCore;

namespace Payment.HonorPay;

internal class HonorPayDefinition
{
    public static Dictionary<string, TransStatus> DepositQueryStatusMap =>
        new()
        {
            { DepositQueryStatus.Pending, TransStatus.Pending },
            { DepositQueryStatus.Success, TransStatus.Success },
            { DepositQueryStatus.Expired, TransStatus.Fail }
        };

    public static Dictionary<string, TransStatus> WithdrawalQueryStatusMap =>
        new()
        {
            { WithdrawalQueryStatus.New, TransStatus.Pending },
            { WithdrawalQueryStatus.Pending, TransStatus.Pending },
            { WithdrawalQueryStatus.Success, TransStatus.Success },
            { WithdrawalQueryStatus.Cancel, TransStatus.Fail }
        };

    public static Dictionary<string, TransStatus> WithdrawalStatusMap =>
        new()
        {
            { WithdrawalStatus.Success, TransStatus.Success },
            { WithdrawalStatus.Cancel, TransStatus.Fail }
        };

    public static Dictionary<string, string> DepositErrorMsgMap =>
        new()
        {
            { "-100", "充值金额不符合规定" },
            { "-101", "充值金额不符合规定" },
            { "-102", "充值金额超过或低於限额" },
            { "-103", "接口参数有误" },
            { "-104", "通道尚未部署" },
            { "-105", "重复订单号" },
            { "-106", "签名验证错误" },
            { "-200", "交易逾时" },
            { "-201", "提交请求服务器 IP 不在白名单内" },
            { "-202", "请求数据验证失败，请检查 header 设置或更新 token" }
        };

    public static Dictionary<string, string> WithdrawalErrorMsgMap =>
        new()
        {
            { "-100", "Unauthorized" },
            { "-101", "接口参数有误" },
            { "-102", "申请金额不符合规定" },
            { "-103", "申请金额超过或低於上下限" },
            { "-104", "馀额不足" },
            { "-105", "查无此单号" },
            { "-106", "此单号已處理完成，无法取消" },
            { "-107", "此单号已取消" },
            { "-108", "此单号目前处理中，无法取消" },
            { "-109", "已有相同的订单号，无法申请代付" },
            { "-200", "請求失敗" }
        };

    public static List<string> NeedBankCodeChannels =>
        new()
        {
            PayType.BankQr, PayType.OnlineBanking
        };

    public const string CallbackResponse = "888888";
    public const string CallbackResponseFailed = "failed";

    public static TransStatus GetWithdrawalTransStatus(string status)
    {
        if (WithdrawalStatusMap.TryGetValue(status, out var result))
        {
            return result;
        }

        return TransStatus.Pending;
    }
}

internal class Path
{
    public const string Deposit = "/api/v2/pay_request";

    public const string DepositQuery = "/api/pay_status";

    public const string Withdrawal = "/api/withdraw/request";

    public const string WithdrawalQuery = "/api/withdraw/status";

    public const string BalanceQuery = "/api/withdraw/cash_balance";
}

internal class ErrorCode
{
    public const string Success = "0";
}

internal class DepositStatus
{
    /// <summary>
    /// 成功
    /// </summary>
    public const string Success = "success";
}

internal class WithdrawalStatus
{

    /// <summary>
    /// 完成
    /// </summary>
    public const string Success = "1";

    /// <summary>
    /// 已取消
    /// </summary>
    public const string Cancel = "2";
}

internal class DepositQueryStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    public const string Pending = "0";

    /// <summary>
    /// 成功
    /// </summary>
    public const string Success = "1";

    /// <summary>
    /// 逾時
    /// </summary>
    public const string Expired = "2";
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
    public const string Momo = "momopay";

    public const string BankQr = "bankcode";

    public const string OnlineBanking = "zhilian";
}

