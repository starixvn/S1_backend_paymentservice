using PaymentService.Service.Model.enums;

namespace PaymentService.Service.Model
{
    public interface ISelLang
    {
        string GetErrMsg(int code);
    }

    public class ApiResult
    {
        public ApiResult() : this(enums.ErrorCode.SystemError)
        {
        }

        public ApiResult(ErrorCode errorCode, string message = "")
            : this((int)errorCode, string.IsNullOrEmpty(message) ? errorCode.ToString() : message, string.Empty,
                string.Empty)
        {
        }

        protected ApiResult(int errorCode, string message, string rawErrorCode = "", string rawMessage = "")
        {
            ErrorCode = errorCode;
            Message = message;
            RawErrorCode = rawErrorCode;
            RawMessage = rawMessage;
        }

        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string RawErrorCode { get; set; }
        public string RawMessage { get; set; }

    }
    public class ApiResult<TData> : ApiResult
    {
        public ApiResult() : this(enums.ErrorCode.SystemError)
        {
        }

        public ApiResult(TData data)
            : this((int)enums.ErrorCode.Success, string.Empty, data)
        {
        }

        public ApiResult(ErrorCode errorCode, string message = "")
            : this((int)errorCode, string.IsNullOrEmpty(message) ? errorCode.ToString() : message, default)
        {
        }

        public ApiResult(int errorCode, string message, TData data)
            : base(errorCode, message)
        {
            Data = data;
        }

        public TData Data { get; set; }
    }
}
