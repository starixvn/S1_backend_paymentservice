using Newtonsoft.Json;

namespace Payment.BigPayz;

public class DepositBankResp
{
    [JsonProperty("transaction")]
    public string ThirdId { get; set; }
    
    [JsonProperty("redirect_to")]
    public string RedirectTo { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

internal class DepositQrResp
{
    [JsonProperty("invoice_number")]
    public string ThirdId { get; set; }
    
    [JsonProperty("redirect_to")]
    public string RedirectTo { get; set; }
    
    [JsonProperty("error_code")]
    public int ErrorCode { get; set; }

    [JsonProperty("error_message")]
    public string Message { get; set; }
}

internal class DepositInfo
{
    public string PayCurrencyId { get; set; }
    public string PayCode { get; set; }
}

internal class DepositCallbackInfo
{
    public string VerifyStr { get; set; }
    public string VerifyTarget { get; set; }
    public string ActualAmount { get; set; }
    public string Status { get; set; }
}

internal class CallbackResp
{
    public CallbackResp(int code, string msg)
    {
        ErrorCode = code;
        Message = msg;
    }
    
    [JsonProperty("error_code")]
    public int ErrorCode { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

internal class PayoutResp
{
    [JsonProperty("invoice_number")]
    public string ThirdId { get; set; }
    
    [JsonProperty("error_code")]
    public int ErrorCode { get; set; }

    [JsonProperty("error_message")]
    private string ErrorMessage1 { get; set; }
    
    [JsonProperty("message")]
    private string ErrorMessage2 { get; set; }

    public string ErrorMessage => string.IsNullOrEmpty(ErrorMessage1)
        ? ErrorMessage2 : ErrorMessage1;
}
