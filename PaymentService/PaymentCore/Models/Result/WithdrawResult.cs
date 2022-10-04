namespace PaymentCore.Models.Result;

public class WithdrawResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string WithdrawId { get; set; }
    public decimal Amount { get; set; }
    public string RawContent { get; set; }
    /// <summary>
    /// 需要更新 WithdrawalInfo.WithdrawalInfo 時使用此欄位
    /// </summary>
    public string WithdrawalInfo { get; set; }
}