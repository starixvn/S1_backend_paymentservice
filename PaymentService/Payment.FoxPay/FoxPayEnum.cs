namespace Payment.FoxPay;

public enum FoxPayStatus
{
    Fail = 0,
    Success = 1,
    ResponseError = 2
}

public enum FoxPayEWalletSysId
{
    Nagad = 8,
    Bkash = 9
}

public enum FoxPayPaymentId
{
    Nagad = 54,
    Bkash = 53
}