using System.ComponentModel;

namespace PaymentService.Service.Model.enums
{
    public enum ErrorCode : int
    {
        Success = 0,
        #region api response fail (1xxxx)
        Fail = 1000,
        NoAuthority = 1001,
        KEY_ERROR = 1002,
        NETWORK_ERROR = 1003,
        OTHER_ERROR = 1999,
        // Account 
        TransferFail = 1200,
        MoneyNotEnough = 1201,
        DuplicateTransID = 1202,
        AmountOutOfRange = 1203,
        TransferNotExist = 1204,// Check Transfer
        TransferPending = 1205,// Check Transfer
        TransferWaitManualCheck = 1206,// Check Transfer
        #endregion
        #region invalid parameter  (2xxxx)
        InvalidParameter = 2000,
        InvalidUserName = 2001,
        InvalidCurrency = 2002,
        InvalidLanguage = 2003,
        #endregion
        #region Payment Error  (3xxxx)
        BankNotFound = 3001,
        PayOutFail = 3002,
        ChannelNotFound = 3003,
        GetExchangeRateFailed = 3004,
        #endregion
        Not_allowed_country = 5001,
        Not_allowed_currency = 5002,
        #region System Error(9xxxx)
        ConnectionTimeOut = 9002,
        ServerIsBusy = 9003,
        ServerResponseError = 9004,
        ServerNotReady = 9005,
        //app setting data error(93xxx)
        SettingNotFound = 9301,
        //Sql
        SqlException = 9501,
        //Unhandle Error
        SystemError = 9000,
        #endregion
        UM = 10
    }
    public enum Direction
    {
        Add = 1,
        Deduct = 2
    }
    public enum CustActStatus
    {
        NonDepoist = 1,
        Depoisted = 2,
    }
    public enum CustStatus
    {
        Running = 1,
        Suspend = 2,
        Closed = 3,
    }
    public enum DepositType
    {
        OnlineBanking = 1,
        MobilePay = 2,
        LocalBank = 3,
        CryptoPay = 4,
        LoadCard = 5,
        QrPay = 6,
        BankAutoChannel = 7,
        CryptoWallet = 8
    }
    
    /// <summary>
    /// 
    /// </summary>
    public enum DepositStatus
    {
        Draft = 0,
        Pending = 1,
        Paid = 2,
        Approved = 3,
        Rejected = 4,
        Canceled = 5,
        Expired = 6,
        OnHold = 7,
        Error = 99
    }

    public enum PayStatusEnum
    {
        Enabled = 1,
        Disabled = 2
    }
    public enum WithdrawalStatus
    {
        Draft = 0,
        Pending = 1,
        OnHold = 2,
        Verify = 3,
        Process = 4,
        Cancelled = 5,
        Approved = 6,
        Rejected = 7,
        ToKYC = 8,
        Error = 99
    }
    
    public enum CryptoPaymentEnum
    {
        DirePay = 22,
        FXBit = 5
    }
    
    public enum ExchangeTypeEnum
    {
        /// <summary>
        /// 我方賣出(玩家提款)
        /// </summary>
        Sell = 0,
        /// <summary>
        /// 我方買入(玩家存款)
        /// </summary>
        Buy = 1
    }
    
    public enum WithdrawalLimitLogCreateFromEnum 
    {
        Default = 0,
        Deposit = 1
    }
    
    public enum WithdrawalLimitAdjustmentReasonEnum
    {
        [Description("None - (Default)")]
        None = 1,
        [Description("Retention Bonus")]
        RetentionBonus = 2,
        [Description("Manual Bonus(Special Bonus)")]
        ManualBonus = 3,
        [Description("Vip Upgrade Reward")]
        VipUpgradeReward = 4,
        [Description("Prize Drop")]
        PrizeDrop = 5,
        [Description("Tech Issue None Affected")]
        TechIssueNoneAffected = 6,
        Test = 7,
        [Description("Vendor Mistake")]
        VendorMistake = 8,
        Deposit = 9,
        Withdrawal = 10,
        Other = 11
    }

    public enum WithdrawalAccountTypeEnum
    {
        EWallet = 1,

        BankCard = 2,

        Crypto = 3
    }

    public enum WithdrawalInfoProcessWayEnum
    {
        /// <summary>
        /// 手動出款
        /// </summary>
        Manual = 0,

        /// <summary>
        /// 自動出款
        /// </summary>
        Auto = 1,

        /// <summary>
        /// 自動出款失敗後轉回手動出款
        /// </summary>
        AutoFailBackManual = 2
    }
    
    public enum WithdrawalType
    {
        OnlineBanking = 1,
        EWalletPayment = 3,
        CryptoPayment = 4
    }
}
