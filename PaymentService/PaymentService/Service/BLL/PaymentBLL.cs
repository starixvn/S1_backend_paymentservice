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
            depoistService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.ThirdId, DepositStatus.Pending, DepositStatus.Approved);
        }

        public void DepositFail(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            depoistService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.ThirdId, DepositStatus.Pending, DepositStatus.Rejected);
        }

        public WithdrawalRequest GetWithdrawalRequest(string transId)
        {
            int withdrawalId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);

            return withdrawalService.GetWithdrawalRequest(withdrawalId);
        }

        public void WithdrawalSuccess(WithdrawalRequest withdrawalRequest, NotifyWithdrawResult notifyWithdrawResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);

            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, forStatus, WithdrawalStatus.Approved);
        }

        public void WithdrawalFail(WithdrawalRequest withdrawalRequest, NotifyWithdrawResult notifyWithdrawResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);

            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, forStatus, WithdrawalStatus.Verify);
        }
    }
}
