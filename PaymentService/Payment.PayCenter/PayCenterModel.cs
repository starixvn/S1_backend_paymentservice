using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.PayCenter
{
    internal enum Direction 
    {
        Deposit = 0,
        Withdraw = 1
    }

    internal class PayCenterRequest
    {
        public int Direction { get; set; }
        public string Player { get; set; }
        public string PlayerWalletNum { get; set; }
        public int Amount { get; set; }
        public string NotifyURL { get; set; }
        public string VendorTranId { get; set; }
    }
    public class Result
    {
        public string PaymentURL { get; set; }
        public int PaymentID { get; set; }
    }

    public class PayCenterResponse
    {
        public int ErrCode { get; set; }
        public string ErrText { get; set; }
        public Result Result { get; set; }
    }

}
