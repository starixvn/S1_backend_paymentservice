using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Service.Model
{
    public class DepositRequest
    {
        public string TransId { set; get; }
        public int DepositType { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
        public string DepositInfo { set; get; }
        public decimal Amount { set; get; }
        public decimal ActualDeposit { set; get; }
        public DateTime CreateTime { set; get; }
        public IDepositInfo depositInfo
        {
            get
            {
                if (DepositType == 1)
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo1_1>(DepositInfo);
                }
                else if (DepositType == 2)
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<DepositInfo2_1>(DepositInfo);
                }

                return null;
            }
        }
        public int Status { set; get; }
    }
    public interface IDepositInfo
    {
        int Ver { get; set; }
    }
    public interface IPay
    {
        int PayId { get; set; }
        int ChannelId { get; set; }
        int CurrencyId { get; set; }
    }
    public class DepositInfo1_1 : IDepositInfo, IPay
    {
        public int Ver { get; set; }
        public int SysId { get; set; }
        public int CurrencyId { get; set; }
        public string SysBankCode { get; set; }
        public string SysBankName { get; set; }
        public string BankLogo { get; set; }
        public int PayId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int ChannelId { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
    }
    public class DepositInfo2_1 : IDepositInfo, IPay
    {
        public int Ver { get; set; }
        public int PayId { get; set; }
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
    }
    public class DepositInfo3_1 : IDepositInfo
    {
        public int Ver { get; set; }
        public int SysId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
    }
    public class Comments
    {
        public string Comment { get; set; }
        public DateTime CreateTime { get; set; }
        public int Action { get; set; }

        public string ActionString { get; set; }

        public decimal ActualDeposit { get; set; }
    }
}
