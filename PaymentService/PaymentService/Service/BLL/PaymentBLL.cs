using PaymentCore;
using PaymentService.Service.DAL;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PaymentService.Service.BLL
{
    public class PaymentBLL
    {
        private readonly DepoistService depoistService;
        private readonly WithdrawalService withdrawalService;

        public PaymentBLL(DepoistService depoistService, WithdrawalService withdrawalService)
        {
            this.depoistService = depoistService;
            this.withdrawalService = withdrawalService;
        }

        public DepositRequest GetDepositRequest(string transId)
        {
            int depositId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);

            return depoistService.GetDepositRequest(depositId);
        }

        public void DepositSuccess(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);
            forStatus.Add((int)DepositStatus.Rejected);
            try
            {
                depoistService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Approved);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DepositFail(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);
            depoistService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Rejected);
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            return depoistService.GetDepositRequesPayEnquiry();
        }

        public WithdrawalRequest GetWithdrawalRequest(string transId)
        {
            int withdrawalId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);

            return withdrawalService.GetWithdrawalRequest(withdrawalId);
        }

        public void WithdrawalSuccess(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);

            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Approved);
        }

        public void WithdrawalFail(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);
            //withdrawalRequest.PayName

            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Verify);
        }

        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry()
        {
            return withdrawalService.GetWithdrawalRequesPayEnquiry();
        }

        public void SetThirdId(string transId, string thirdID)
        {
            depoistService.SetThirdId(transId, thirdID);
        }

        public int DepositRejectd(string transId, decimal actualDeposit, string comment, string Operator)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);

            var depositRequest = GetDepositRequest(transId);
            return depoistService.SetDepositInfo(transId, depositRequest.SiteId, actualDeposit, comment, Operator, forStatus, DepositStatus.Rejected, null);
        }
    }
}
