using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
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
            var depositId = GetDepositId(transId);
            return _depositService.GetDepositRequest(depositId);
        }
        
        public DepositInfoData GetDepositInfo(string transId)
        {
            var depositId = GetDepositId(transId);
            return _depositService.GetDepositInfo(depositId);
        }

        private int GetDepositId(string transId)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var depositId);
            return depositId;
        }

        public void DepositSuccess(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            var forStatus = new List<int>();
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
        
        public int DepositManualSuccess(DepositInfoData depositInfoData)
        {
            var forStatus = new List<int>
            {
                (int)DepositStatus.Draft,
                (int)DepositStatus.Pending,
                (int)DepositStatus.Rejected,
                (int)DepositStatus.Canceled,
                (int)DepositStatus.Expired,
                (int)DepositStatus.OnHold,
                (int)DepositStatus.Error
            };
            
            try
            {
                return _depositService.SetDepositInfo(
                    depositInfoData.TransId, 
                    depositInfoData.SiteId,
                    (IPay)depositInfoData.depositInfo, 
                    depositInfoData.ActualDeposit, 
                    depositInfoData.Comment, 
                    null,
                    depositInfoData.RefId, 
                    depositInfoData.Operator, 
                    forStatus, 
                    DepositStatus.Approved);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DepositFail(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            var forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);
            _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Rejected);
        }

        public void DepositPending(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            var forStatus = new List<int>();
            forStatus.Add((int)DepositStatus.Pending);
            _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Pending);
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            return _depositService.GetDepositRequesPayEnquiry();
        }

        public WithdrawalRequest GetWithdrawalRequest(string transId)
        {
            var withdrawalId = 0;
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);

            return withdrawalService.GetWithdrawalRequest(withdrawalId);
        }

        public void WithdrawalSuccess(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);

            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Approved);
        }

        public void WithdrawalFail(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);
            //withdrawalRequest.PayName
            
            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Verify);
        }

        public void WithdrawalPending(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int>();
            forStatus.Add((int)WithdrawalStatus.Process);
            //withdrawalRequest.PayName
            
            withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Process);
        }

        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry(IEnumerable<int> payIds)
        {
            var json = JsonConvert.SerializeObject(payIds);

            return withdrawalService.GetWithdrawalRequesPayEnquiry(json);
        }

        public void SetThirdId(string transId, string thirdID)
        {
            _depositService.SetThirdId(transId, thirdID);
        }
        public void UpdateDepositInfo(string transId, string depositInfo)
        {
            _depositService.UpdateDepositInfo(transId, depositInfo);
        }
    }
}
