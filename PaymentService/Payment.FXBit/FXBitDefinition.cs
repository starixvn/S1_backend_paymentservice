using System;
using System.Collections.Generic;

namespace Payment.FXBit
{
    internal class FXBitDefinition
    {
        public static Dictionary<int, string> ToFXBitCurrencyMap =>
            new Dictionary<int, string>()
            {
                { 2, "MYR" },
                { 96, "USDT_ERC20" }
            };

        public static string GetFXBitCurrency(int currencyId)
        {
            if (ToFXBitCurrencyMap.TryGetValue(currencyId, out var result))
            {
                return result;
            }

            throw new ArgumentException($"CurrencyId: {currencyId} mapping to FXBitCurrency failed.");
        }
    }

    internal class ErrorCode
    {
        public const string Success = "0";

        public const string GetReceptionTokenFailed = "9001";

        public const string GetWalletAddressFailed = "9002";

        public const string GetExchangeRateFailed = "9000";
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

    
}
