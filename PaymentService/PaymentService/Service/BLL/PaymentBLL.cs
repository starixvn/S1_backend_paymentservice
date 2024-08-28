using Newtonsoft.Json;

using PaymentCore.Interface;

using PaymentService.Exceptions;
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
        private readonly ConfigBLL _configBll;

        public PaymentBLL(IServiceProvider provider)
        {
            _depositService = provider.GetRequiredService<DepositService>();
            _withdrawalService = provider.GetRequiredService<WithdrawalService>();
            _common = provider.GetRequiredService<Common>();
            _logger = provider.GetRequiredService<ILogger<PaymentBLL>>();
            _configBll = provider.GetRequiredService<ConfigBLL>();
        }

        public DepositRequest GetDepositRequest(string transId)
        {
            var depositId = _common.GetId(transId);
            return _depositService.GetDepositRequest(depositId);
        }

        public DepositInfoModel GetDepositInfo(string transId)
        {
            var depositId = _common.GetId(transId);
            return _depositService.GetDepositInfo(depositId);
        }

        public void DepositSuccess(DepositRequest depositRequest, IDepositResult depositResult)
        {
            var forStatus = new[]
            {
                DepositStatus.Pending,
                DepositStatus.Rejected,
                DepositStatus.Canceled
            };

            if (depositRequest.SiteId == 0 || depositRequest.CurrencyId == 0)
            {
                var deposit = GetDepositInfo(depositRequest.TransId);
                depositRequest.SiteId = deposit.SiteId;
                depositRequest.CurrencyId = deposit.CurrencyId;
            }

            var canReward = CanRewardPoint(depositRequest.SiteId, depositRequest.CurrencyId);
            SetDepositInfo(depositRequest, depositResult, DepositStatus.Approved, forStatus, canReward);
        }

        public bool DepositManualSuccess(DepositInfoModel depositInfo)
        {
            var forStatus = new[]
            {
                DepositStatus.Draft,
                DepositStatus.Pending,
                DepositStatus.Rejected,
                DepositStatus.Canceled,
                DepositStatus.Expired,
                DepositStatus.OnHold,
                DepositStatus.Error
            };

            var di = GetPayInfoByDepositInfo(depositInfo.DepositInfo);
            var result = _depositService.SetDepositInfoV2(
                new SetDepositInfoParams
                {
                    TransId = depositInfo.TransId,
                    SiteId = depositInfo.SiteId,
                    ActualDeposit = depositInfo.ActualDeposit,
                    ThirdId = depositInfo.RefId,
                    PayId = di.PayId,
                    Operator = depositInfo.Operator,
                    ForStatus = forStatus,
                    Status = DepositStatus.Approved
                },
                CanRewardPoint(depositInfo.SiteId, depositInfo.CurrencyId));

            return result;
        }

        public void DepositFail(DepositRequest depositRequest, IDepositResult depositResult)
        {
            var forStatus = new[] { DepositStatus.Pending };
            SetDepositInfo(depositRequest, depositResult, DepositStatus.Rejected, forStatus);
        }

        public void SimpleDepositFail(string transId, int siteId, IPay payInfo, string comment)
        {
            _depositService.SetDepositInfoV2(new SetDepositInfoParams
            {
                TransId = transId,
                Comment = comment,
                SiteId = siteId,
                PayId = payInfo.PayId,
                Operator = payInfo.PayName,
                ForStatus = new[] { DepositStatus.Pending },
                Status = DepositStatus.Rejected
            });
        }

        public void DepositPending(DepositRequest depositRequest, IDepositResult depositResult)
        {
            var forStatus = new[] { DepositStatus.Pending };
            SetDepositInfo(depositRequest, depositResult, DepositStatus.Pending, forStatus);
        }

        public void SetDepositLog(string transId, int siteId, string comment, int status, decimal actualDeposit, string @operator)
        {
            var depositId = _common.GetId(transId);
            _depositService.SetDepositLog(depositId, siteId, comment, status, actualDeposit, @operator);
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry(IEnumerable<int> payIds)
        {
            var json = JsonConvert.SerializeObject(payIds);
            
            return _depositService.GetDepositRequesPayEnquiry(json);
        }

        public WithdrawalRequest GetWithdrawalRequest(string transId)
        {
            var withdrawalId = _common.GetId(transId);
            return _withdrawalService.GetWithdrawalRequest(withdrawalId);
        }

        public void WithdrawalSuccess(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int>
            {
                (int)WithdrawalStatus.Process,
                (int)WithdrawalStatus.Verify // 有可能送單timeout失敗退回後回調成功
            };
            _withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Approved);
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

            _ = _withdrawalService.SetWithdrawalInfoByManualApprove(withdrawalRequest.TransId,
                withdrawalRequest.SiteId,
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
        public void WithdrawalReject(WithdrawalRequest withdrawalRequest, string comment, string refId, string @operator, bool isManual)
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

            _ = _withdrawalService.SetWithdrawalInfoReject(withdrawalRequest.TransId,
                withdrawalRequest.SiteId,
                comment,
                refId,
                @operator,
                forStatus, WithdrawalStatus.Rejected, withdrawalRequest, isManual);
        }

        public void WithdrawalFail(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int> { (int)WithdrawalStatus.Process };
            _withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Verify);
            _withdrawalService.SetWithdrawalInfoProcessWay(withdrawalRequest.TransId, WithdrawalInfoProcessWayEnum.AutoFailBackManual);
        }

        public void WithdrawalPending(WithdrawalRequest withdrawalRequest, IWithdrawResult notifyWithdrawResult)
        {
            var forStatus = new List<int> { (int)WithdrawalStatus.Process };
            _withdrawalService.SetWithdrawalInfo(withdrawalRequest.TransId, withdrawalRequest.SiteId, notifyWithdrawResult.Message, notifyWithdrawResult.ThirdId, withdrawalRequest.PayName, forStatus, WithdrawalStatus.Process);
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

        #region private

        private static IPay GetPayInfoByDepositInfo(string depositInfo)
        {
            var pay = JsonConvert.DeserializeObject<TpPayInfo>(depositInfo);

            if (pay == null)
                throw new PaymentException("DepositInfo is empty.");

            return pay;
        }

        private void SetDepositInfo(DepositRequest depositRequest, IDepositResult depositResult, DepositStatus status,
            IEnumerable<DepositStatus> forStatus, bool canReward = false)
        {
            var di = GetPayInfoByDepositInfo(depositRequest.DepositInfo);
            var p = new SetDepositInfoParams
            {
                TransId = depositRequest.TransId,
                SiteId = depositRequest.SiteId,
                ActualDeposit = depositResult.ActualAmount,
                ThirdId = depositResult.ThirdId,
                PayId = di.PayId,
                Operator = di.PayName,
                ForStatus = forStatus,
                Status = status
            };
            var result = _depositService.SetDepositInfoV2(p, canReward);

            if (!result)
                _logger.LogError("Update DepositInfo failed. SetDepositInfoParams=" + JsonConvert.SerializeObject(p));
        }

        private bool CanRewardPoint(int siteId, int currencyId)
        {
            var canRewardCurrency = _configBll.GetConfigValue<int[]>("CanReward", siteId);

            return canRewardCurrency.Contains(currencyId);
        }

        #endregion
    }
}
