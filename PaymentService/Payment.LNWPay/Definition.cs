using PaymentCore;

namespace Payment.LNWPay;

internal class Definition
{
    private static Dictionary<string, TransStatus> StatusMap =>
        new()
        {
            { "pending", TransStatus.Pending },
            { "success", TransStatus.Success },
            { "unsuccess", TransStatus.Fail }
        };
    
    public static TransStatus GetTransStatus(string status)
        => StatusMap.GetValueOrDefault(status.ToLower(), TransStatus.Pending);
    
    public const string SuccessStatus = "success";
    public const string CallbackResponse = "success";
}

internal class Path
{
    public const string Auth = "/services/api/v1/service-request";
    public const string CreateDepositAuto = "/services/api/v1/account-request";
    public const string CreateDepositQr = "/services/api/v1/qrcode-request";
    public const string CreateDepositTrue = "/services/api/v1/wallet-request";
    public const string CreatePayout = "/services/api/v1/withdraw-request";
    public const string CreateDepositPayway = "/services/services/api/v2/payment-request";
}