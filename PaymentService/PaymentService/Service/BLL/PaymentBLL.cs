using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Extensions;
using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.BLL
{
    public class PaymentBLL
    {
        private readonly DepositService _depositService;
        private readonly WithdrawalService _withdrawalService;
        private readonly Common _common;
        private readonly ILogger<PaymentBLL> _logger;

        public PaymentBLL(IServiceProvider provider)
        {
            _depositService = provider.GetRequiredService<DepositService>();
            _withdrawalService = provider.GetRequiredService<WithdrawalService>();
            _common = provider.GetRequiredService<Common>();
            _logger = provider.GetRequiredService<ILogger<PaymentBLL>>();
        }

        public DepositRequest GetDepositRequest(string transId)
        {
            var depositId = _common.GetId(transId);
            return _depositService.GetDepositRequest(depositId);
        }
        
        public DepositInfoData GetDepositInfo(string transId)
        {
            var depositId = _common.GetId(transId);
            return _depositService.GetDepositInfo(depositId);
        }

        public void DepositSuccess(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            var forStatus = new List<int>
            {
                (int)DepositStatus.Pending,
                (int)DepositStatus.Rejected
            };
            try
            {
                _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, iDepositResult.ActualAmount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Approved);
                if (CommonExtension.CanRewardPoint(depositRequest.SiteId, depositRequest.CurrencyId))
                {
                    _depositService.InsertDepositRewardPointQueue(depositRequest.TransId);
                }
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
            var forStatus = new List<int> { (int)DepositStatus.Pending };
            _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Rejected);
        }

        public void DepositPending(DepositRequest depositRequest, IDepositResult iDepositResult)
        {
            var forStatus = new List<int> { (int)DepositStatus.Pending };
            _depositService.SetDepositInfo(depositRequest.TransId, depositRequest.SiteId, (IPay)depositRequest.depositInfo, depositRequest.Amount, iDepositResult.Comment, iDepositResult.RefThirdId, iDepositResult.ThirdId, depositRequest.PayName, forStatus, DepositStatus.Pending);
        }
        
        public void SetDepositLog(string transId, int siteId, string comment, int status, decimal actualDeposit, string @operator)
        {
            var depositId = _common.GetId(transId);
            _depositService.SetDepositLog(depositId, siteId, comment,status, actualDeposit, @operator);
        }
        
        public void InsertDepositRewardPointQueue(string transId)
        {
            var resultCode = _depositService.InsertDepositRewardPointQueue(transId);

            if (resultCode != (int) ErrorCode.Success)
            {
                _logger.LogError($"InsertDepositRewardPointQueue Failed. ResultCode({resultCode}), TransId: {transId}");
            }
        }


        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            return _depositService.GetDepositRequesPayEnquiry();
        }

        public WithdrawalRequest GetWithdrawalRequest(string transId)
        {
            var withdrawalId = _common.GetId(transId);
            return _withdrawalService.GetWithdrawalRequest(withdrawalId);
        }

        public void WithdrawalSuccess(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int> { (int)WithdrawalStatus.Process };
            _withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Approved);
        }

        public void WithdrawalFail(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int> { (int)WithdrawalStatus.Process };
            _withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Verify);
        }

        public void WithdrawalPending(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int> { (int)WithdrawalStatus.Process };
            _withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, withdrawalRequest, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Process);
        }
        
        public void SetWithdrawalLog(string transId, int siteId, string comment, int status, string @operator)
        {
            var withdrawalId = _common.GetId(transId);
            _withdrawalService.SetWithdrawalLog(withdrawalId, siteId, comment, status, @operator);
        }

        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry(IEnumerable<int> payIds)
        {
            var json = JsonConvert.SerializeObject(payIds);

            return _withdrawalService.GetWithdrawalRequesPayEnquiry(json);
        }

        public void SetThirdId(string transId, string thirdID)
        {
            _depositService.SetThirdId(transId, thirdID);
        }
        public void UpdateDepositInfo(string transId, string depositInfo)
        {
            _depositService.UpdateDepositInfo(transId, depositInfo);
        }

        public void UpdateWithdrawalInfo(string transId, string withdrawalInfo)
        {
            _withdrawalService.UpdateWithdrawalInfo(transId, withdrawalInfo);
        }

        public string GetWithdrawalInfo(string transId)
        {
            return _withdrawalService.GetWithdrawalInfo(transId);
        }
    }
}
