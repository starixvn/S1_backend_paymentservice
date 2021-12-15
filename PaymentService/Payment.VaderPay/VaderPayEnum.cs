using System.ComponentModel;

namespace Payment.VaderPay
{
    public enum DepositStatus
    {
        Initiated = 0,
        Accepted = 1,
        Rejected = -1,
        Error = -2,
        Pending = -3
    }
    
    public enum DepositCallbackStatus
    {
        Success = 1,
        Rejected = 2,
        Approved = 3
    }

    public enum WithdrawalStatus
    {
        Success = 0,
        
        [Description("Duplicate Reference Id")]
        Duplicate =101,
        
        [Description("Amount more than maximum transaction amount")]
        OverMaxAmount = 204,
        
        [Description("Beneficiary RefId is Exists")]
        TransactionExists = 302
    }

    public enum WithdrawalCallbackStatus
    {
        Pending = 0,
        Success = 1,
        Reject = 2,
        ManualApprove = 3
    }

    public enum WithdrawalResponseStatus
    {
        OperationSuccess = 0,
        InvalidTransaction = 1,
        DuplicateTransaction = 2,
        InvalidParameter = 3,
        OperationFailed = 1001,
        SystemMaintenance = 1002
    }
}