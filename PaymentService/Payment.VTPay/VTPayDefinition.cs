using PaymentCore;
using static Payment.VTPay.VTPayModel;
namespace Payment.VTPay;

internal class VTPayDefinition
{
    internal class Path
    {
        public const string Deposit = "/fund_in/pg_session.api";

        public const string Withdrawal = "/fund_out/pg_session.api";
    }

    internal class ErrorCode
    {
        public const string Success = "1";
    }

    public class CallbackResponse
    {
        public Content Content { get; set; }
        public int ResultCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static TransStatus GetTransStatus(string status)
        => VTPayzStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;

    private class VTPayzStatus
    {
        public const string success = "success";
        public const string failed = "failed";
    }
    private static Dictionary<string, TransStatus> VTPayzStatusMap =>
        new()
        {
            { VTPayzStatus.success, TransStatus.Success },
            { VTPayzStatus.failed, TransStatus.Fail },
        };

}