namespace PaymentCore.Models.Result;

public class DepositResult
{
    public DepositResult()
    {
    }

    public DepositResult(bool success, string errorCode, string message = "")
    {
        this.Success = success;
        this.ErrorCode = errorCode;
        this.Message = message;
    }
        
    public bool Success { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal SuggestAmount { get; set; }
    public string Url { get; set; }
    public Dictionary<string, string> FormData { get; set; }
    public string ThirdId { get; set; }
    public string Memo { get; set; }
    public string RawContent { get; set; }
    /// <summary>
    /// 需要更新 DepositInfo.DepositInfo 時使用此欄位
    /// </summary>
    public string DepositInfo { get; set; }
}