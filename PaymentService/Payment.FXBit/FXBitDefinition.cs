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
                { FxBitStatus.New, TransStatus.Pending },
                { FxBitStatus.Confirming, TransStatus.Pending },
                { FxBitStatus.Confirmed, TransStatus.Success },
                { FxBitStatus.Failed, TransStatus.Fail },
                { FxBitStatus.Insufficient, TransStatus.Success },
                { FxBitStatus.Expired, TransStatus.Fail },
                { FxBitStatus.IncorrectAmount, TransStatus.Success }
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

    internal class FxBitStatus
    {
        /// <summary>
        /// 尚未入帳
        /// </summary>
        public const string New = "0";

        /// <summary>
        /// 監控到有金額入帳
        /// </summary>
        public const string Confirming = "1";

        /// <summary>
        /// 訂單完成
        /// </summary>
        public const string Confirmed = "2";

        /// <summary>
        /// 交易錯誤
        /// </summary>
        public const string Failed = "3";

        /// <summary>
        /// 入帳金額不足
        /// </summary>
        public const string Insufficient = "4";

        /// <summary>
        /// 訂單已過期
        /// </summary>
        public const string Expired = "5";

        /// <summary>
        /// 入帳金額超出
        /// </summary>
        public const string IncorrectAmount = "6";

        /// <summary>
        /// 訂單過期後收到入金
        /// </summary>
        public const string PayInAfterExpired = "7";
    }

    internal class ErrorCode
    {
        public const string Success = "0";

        public const string GetExchangeRateFailed = "9900";

        public const string GetReceptionTokenFailed = "9901";

        public const string GetWalletAddressFailed = "9902";
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
