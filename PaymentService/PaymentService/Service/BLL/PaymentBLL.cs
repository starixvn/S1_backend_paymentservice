using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PaymentCore;
using PaymentService.Service.DAL;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.BLL
{
    public class PaymentBLL
    {
        private readonly DepositService _depositService;
        private readonly WithdrawalService withdrawalService;

        public PaymentBLL(DepositService depositService, WithdrawalService withdrawalService)
        {
            this._depositService = depositService;
            this.withdrawalService = withdrawalService;
        }

        public DepositRequest GetDepositRequest(string transId)
        {
            int depositId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);

            return _depositService.GetDepositRequest(depositId);
        }

        public void DepositSuccess(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);
            forStatus.Add((int)DepositStatus.Rejected);
            try
            {
                _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, iDepositResult.ActualAmount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Approved);
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
            _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Rejected);
        }

        public void DepositPending(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);
            _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Pending);
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            return _depositService.GetDepositRequesPayEnquiry();
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

        public void WithdrawalPending(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            List<int> forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);
            //withdrawalRequest.PayName
            
            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Process);
        }

        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry()
        {
            return withdrawalService.GetWithdrawalRequesPayEnquiry();
        }

        public void SetThirdId(string transId, string thirdID)
        {
            _depositService.SetThirdId(transId, thirdID);
        }
    }
}
