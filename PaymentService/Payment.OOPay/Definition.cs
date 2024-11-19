using PaymentCore;

namespace Payment.OOPay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositStatusMap =>
        new()
        {
            { "PAID", TransStatus.Success }
        };

    public static TransStatus GetDepositTransStatus(string status)
        => DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);

    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "PAID", TransStatus.Success },
            { "CANCELLED", TransStatus.Fail }
        };

    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);

    public const string SuccessCode = "0";

    private static Dictionary<string, string> DepositPathMap =>
        new()
        {
            { "VND", "/shou/new" },
            { "THB", "/shou/new" },
            { "INR", "/order/create" }
        };

    public static string GetDepositPath(string currencyId)
        => DepositPathMap.GetValueOrDefault(currencyId, "/shou/new");

    private static Dictionary<string, string> WithdrawalPathMap =>
        new()
        {
            { "VND", "/fu/new" },
            { "THB", "/fu/new" },
            { "INR", "/payout/create" }
        };

    public static string GetWithdrawalPath(string currencyId)
        => DepositPathMap.GetValueOrDefault(currencyId, "/fu/new");
}

internal class CallbackResponse
{
    public const string Success = "success";
}
