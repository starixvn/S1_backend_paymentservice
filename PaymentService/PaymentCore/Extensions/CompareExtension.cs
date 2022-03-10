using System.Net;

namespace PaymentCore.Extensions
{
    public static class CompareExtension
    {
        public static bool IsSuccessStatus(this HttpStatusCode httpStatus)
            => (int)httpStatus >= 200 && (int)httpStatus < 300;
    }
}
