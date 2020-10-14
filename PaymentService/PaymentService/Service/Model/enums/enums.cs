using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        MobilePayment = 2,
        LocalBank = 3,
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
        Error = 99
    }

    public enum AdminUserStatus
    {
        Running = 1,
        Suspend = 2,
        Closed = 3,
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
        Error = 99
    }
}
