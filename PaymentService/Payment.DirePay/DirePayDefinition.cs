using System;
using System.Collections.Generic;
using System.Text;

namespace Payment.DirePay
{
    internal class DirePayDefinition
    {

    }

    internal class ErrorCode
    {
        public const string Success = "0";

        public const string GetReceptionTokenFailed = "9001";

        public const string GetWalletAddressFailed = "9002";

        public const string GetExchangeRateFailed = "9000";
    }

    internal class Path
    {
        public const string Login = "/api/login";
        
        public const string ExchangeRate = "/api/callBack";
    }
}
