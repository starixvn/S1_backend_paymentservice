using System.ComponentModel.DataAnnotations;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.Model
{
    public class DepositManualRequest
    {
        [Required]
        public string TransId { get; set; }
        [Required]
        [Range(0.0, double.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
        public decimal Amount { get; set; }
        public string ThirdId { get; set; }
        public string Operator { get; set; }
    }

    public class DepositManualResponse
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public decimal Amount { get; set; }
    }

    public class DepositInfoModel
    {
        public string DepositId { get; set; }
        public string TransId { set; get; }
        public DepositType DepositType { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public int CurrencyId { set; get; }
        public string DepositInfo { set; get; }
        public decimal Amount { set; get; }
        public decimal ActualDeposit { set; get; }
        public int Status { set; get; }
        public string Comment { set; get; }
        public string RefId { set; get; }
        public string Operator { set; get; }
    }

    public class SetDepositInfoParams
    {
        public string TransId { set; get; }
        public int SiteId { set; get; }
        public int PayId { set; get; }
        public decimal ActualDeposit { set; get; }
        public string Comment { set; get; }
        public string ThirdId { set; get; }
        public string Operator { set; get; }
        public IEnumerable<DepositStatus> ForStatus { set; get; }
        public DepositStatus Status { set; get; }
    }
}