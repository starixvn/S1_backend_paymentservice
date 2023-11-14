using PaymentService.Service.Model.enums;

namespace PaymentService.Exceptions
{
    public class PaymentException : Exception
    {
        public PaymentException()
        {
        }

        public PaymentException(string message)
            : base(message)
        {
        }

        public PaymentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public PaymentException(string message, ErrorCode code = ErrorCode.SystemError)
            : base(message)
        {
            Code = code;
        }

        public ErrorCode Code { get; set; } = ErrorCode.SystemError;
    }
}
