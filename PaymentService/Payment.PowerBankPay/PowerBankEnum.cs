namespace Payment.PowerBankPay;

public class PowerBankEnum
{
    public enum ResultCode
    {
        Success = 10,
        BalanceNotEnough = 11,
        AlreadyExists = 12,
        NotFound = 13,
        Approved = 100,
        Pending = 101,
        Rejected = 102,
        UnknownError = 1000,
        SignatureNotMatch = 1001,
    }
}