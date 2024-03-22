namespace Payment.LNWPay;

internal class Definition
{
    public const string SuccessStatus = "success";
}

internal class Path
{
    public const string Auth = "/api/v1/service-request";
    
    public const string CreateDepositAuto = "/api/v1/account-request";
    public const string CreateDepositQr = "/api/v1/qrcode-request";
    public const string CreateDepositTrue = "/api/v1/wallet-request";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/merchant/withdraw_orders";
}