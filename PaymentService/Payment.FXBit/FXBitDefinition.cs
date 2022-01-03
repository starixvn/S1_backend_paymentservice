using System;
using System.Collections.Generic;
using PaymentCore;

namespace Payment.FXBit
{
    internal class FXBitDefinition
    {
        public static Dictionary<string, TransStatus> DepositTransStatusMap =>
            new Dictionary<string, TransStatus>()
            {
                { "0", TransStatus.Pending },
                { "1", TransStatus.Pending },
                { "2", TransStatus.Success },
                { "3", TransStatus.Fail },
                { "4", TransStatus.Fail },
                { "5", TransStatus.Fail },
                { "6", TransStatus.Fail }
            };

        public static TransStatus GetDepositTransStatus(string status)
        {
            if (DepositTransStatusMap.TryGetValue(status, out var result))
            {
                return result;
            }

            return TransStatus.Pending;
        }

    }

    internal class ErrorCode
    {
        public const string Success = "0";

        public const string GetExchangeRateFailed = "9000";

        public const string GetReceptionTokenFailed = "9001";

        public const string GetWalletAddressFailed = "9002";
    }

    internal class MediaType
    {
        public const string Json = "application/json";
    }

    internal class Path
    {
        /// <summary>
        /// 取得匯率
        /// </summary>
        public const string GetExchangeRate = "/v1/api/getExchangeRate";

        /// <summary>
        /// 取得交易明細
        /// </summary>
        public const string GetTransactionDetail = "/v1/api/getTransactionDetail";

        /// <summary>
        /// 取得錢包地址
        /// </summary>
        public const string GetWalletAddress = "/v1/api/getWalletAddress";

        /// <summary>
        /// 創建交易
        /// </summary>
        public const string CreateTransaction = "/v1/api/getReceiptToken";

        /// <summary>
        /// H5 支付（創建交易並跳轉到 QR Code 頁面）
        /// </summary>
        public const string H5 = "/v2";

        /// <summary>
        /// 查詢交易狀態
        /// </summary>
        public const string QueryTransactionStatus = "/v1/api/pollPaymentStatus";
    }

    internal class CallbackResponse
    {
        public const string Success = "success";

        public const string Failed = "failed";
    }
}
