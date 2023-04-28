using Newtonsoft.Json;
using PaymentCore.Extensions;
using PaymentCore.Interface;
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

        public PaymentBLL(IServiceProvider provider)
        {
            _depositService = provider.GetRequiredService<DepositService>();
            _withdrawalService = provider.GetRequiredService<WithdrawalService>();
            _common = provider.GetRequiredService<Common>();
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
                if (depositRequest.SiteId == 0 || depositRequest.CurrencyId == 0)
                {
                    var deposit = GetDepositInfo(depositRequest.TransId);
                    depositRequest.SiteId = deposit.SiteId;
                    depositRequest.CurrencyId = deposit.CurrencyId;
                }

                _depositService.SetDepositInfo(depositRequest.TransId,
                    depositRequest.SiteId,
                    (IPay)depositRequest.depositInfo,
                    iDepositResult.ActualAmount,
                    iDepositResult.Comment,
                    iDepositResult.RefThirdId,
                    iDepositResult.ThirdId,
                    depositRequest.PayName,
                    forStatus,
                    DepositStatus.Approved,
                    CommonExtension.CanRewardPoint(depositRequest.SiteId, depositRequest.CurrencyId));
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
                var result = _depositService.SetDepositInfo(
                    depositInfoData.TransId,
                    depositInfoData.SiteId,
                    (IPay)depositInfoData.depositInfo,
                    depositInfoData.ActualDeposit,
                    depositInfoData.Comment,
                    null,
                    depositInfoData.RefId,
                    depositInfoData.Operator,
                    forStatus,
                    DepositStatus.Approved,
                    CommonExtension.CanRewardPoint(depositInfoData.SiteId, depositInfoData.CurrencyId));

                return result;
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
            _depositService.SetDepositLog(depositId, siteId, comment, status, actualDeposit, @operator);
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
            SetWithdrawalAccountCounter(withdrawalRequest, WithdrawalStatus.Approved);
        }

        private void SetWithdrawalAccountCounter(WithdrawalRequest withdrawalRequest, WithdrawalStatus changeToStatus)
        {
            var originStatus = withdrawalRequest.Status;

            // 原本不是 Approved 要變成 Approved  +1，其餘狀況不用處理
            if (originStatus != WithdrawalStatus.Approved && changeToStatus == WithdrawalStatus.Approved)
            {
                var _ = _withdrawalService.SetWithdrawalAccountCounter(
                    custId: withdrawalRequest.CustId,
                    accountNo: withdrawalRequest.AccountNo,
                    accountType: withdrawalRequest.AccountType,
                    counter: 1).Result;
            }
        }

        /// <summary>
        /// 提款單手動 Approve
        /// </summary>
        /// <param name="withdrawalRequest"></param>
        /// <param name="comment"></param>
        /// <param name="refId"></param>
        /// <param name="operator"></param>
        public void WithdrawalManualApprove(WithdrawalRequest withdrawalRequest, string comment, string refId, string @operator)
        {
            var forStatus = new List<int>
            {
                (int)WithdrawalStatus.Draft,
                (int)WithdrawalStatus.Pending,
                (int)WithdrawalStatus.OnHold,
                (int)WithdrawalStatus.Verify,
                (int)WithdrawalStatus.Process,
                (int)WithdrawalStatus.Cancelled,
                (int)WithdrawalStatus.Rejected,
                (int)WithdrawalStatus.ToKYC,
                (int)WithdrawalStatus.Error
            };

            _withdrawalService.SetWithdrawalInfoByManualApprove(withdrawalRequest.TransId,
                withdrawalRequest.SiteId,
                withdrawalRequest,
                comment,
                refId,
                @operator,
                forStatus, WithdrawalStatus.Approved, withdrawalRequest);
        }


        /// <summary>
        /// 提款單手動 Reject
        /// </summary>
        /// <param name="withdrawalRequest"></param>
        /// <param name="comment"></param>
        /// <param name="refId"></param>
        /// <param name="operator"></param>
        public void WithdrawalManualReject(WithdrawalRequest withdrawalRequest, string comment, string refId, string @operator)
        {
            var forStatus = new List<int>
            {
                (int)WithdrawalStatus.Draft,
                (int)WithdrawalStatus.Pending,
                (int)WithdrawalStatus.OnHold,
                (int)WithdrawalStatus.Verify,
                (int)WithdrawalStatus.Process,
                (int)WithdrawalStatus.Cancelled,
                (int)WithdrawalStatus.Approved,
                (int)WithdrawalStatus.ToKYC,
                (int)WithdrawalStatus.Error
            };

            _withdrawalService.SetWithdrawalInfoByManualReject(withdrawalRequest.TransId,
                withdrawalRequest.SiteId,
                withdrawalRequest,
                comment,
                refId,
                @operator,
                forStatus, WithdrawalStatus.Rejected, withdrawalRequest);
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

        public IEnumerable<DepositAutoCancel> GetDepositAutoCancelList(int limit)
        {
            return _depositService.GetDepositAutoCancelList(limit);
        }

        public int DepositAutoCancel(int depositId, string Operator)
        {
            return _depositService.DepositAutoCancel(depositId, Operator);
        }
    }
}
