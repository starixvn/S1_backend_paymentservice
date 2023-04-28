using Dapper;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using System.Data;
using PaymentService.Model;
using PaymentService.Service.Library;

namespace PaymentService.Service.DAL
{
    public class WithdrawalService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly WalletService _walletService;
        private readonly Common _common;

        public WithdrawalService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _walletService = provider.GetRequiredService<WalletService>();
            _common = provider.GetRequiredService<Common>();
        }
        
        public WithdrawalRequest GetWithdrawalRequest(int withdrawalId)
        {
            return _reportDbClient.QueryFirst<WithdrawalRequest>("Service_WithdrawalRequestPay_Get", new { WithdrawalId = withdrawalId });
        }
        
        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry(string payIds)
        {
            return _reportDbClient.QueryUseCommandDefinition<WithdrawalRequest>("Service_WithdrawalRequestPayEnquiry_Get",
                new DynamicParameters(new { _PayIds = payIds }));
        }

        public int SetWithdrawalInfo(string transId, int siteId, IPay pay, string comment, string refId, string Operator, IEnumerable<int> forStatus, WithdrawalStatus status)
        {
            var withdrawalId = _common.GetId(transId);
            var result = 0;
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new
                {
                    _WithdrawalId = withdrawalId,
                    _SiteId = siteId,
                    _Comment = comment,
                    _ForStatus = sForStatus,
                    _Status = (int)status,
                    _WithdrawalInfo = (string)null,
                    _WithdrawalType = (int?)null,
                    _RefId = refId,
                    _Operator = Operator,
                    _ChargeFee = 0
                });
                p.Add("_CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("_UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("_Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Admin_WithdrawalInfo_Set_V2", p, trans);
                var amount = p.Get<decimal>("_Amount");
                if (result > 0)
                {
                    if (status == WithdrawalStatus.Approved)
                    {
                        result = _walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, -1 * amount, trans);
                    }

                    if (result > 0 && (status == WithdrawalStatus.Approved || status == WithdrawalStatus.Rejected))
                    {
                        result = _mainDbClient.Execute("Admin_WithdrawalInfo_FlowEndTime_Set",
                            new { _WithdrawalId = withdrawalId } ,trans);
                    }

                    SetWithdrawalLog(withdrawalId, siteId, comment, (int)status, Operator, trans);
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
                conn.Close();
                conn.Dispose();
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, "Payment Service");
            }

            return result;
        }

        /// <summary>
        ///  提款單手動 Approve
        /// </summary>
        /// <param name="transId"></param>
        /// <param name="siteId"></param>
        /// <param name="pay"></param>
        /// <param name="comment"></param>
        /// <param name="refId"></param>
        /// <param name="Operator"></param>
        /// <param name="forStatus"></param>
        /// <param name="status"></param>
        /// <param name="withdrawalRequest"></param>
        /// <returns></returns>
        public int SetWithdrawalInfoByManualApprove(string transId, int siteId, IPay pay, string comment, string refId, string Operator, IEnumerable<int> forStatus, WithdrawalStatus status, WithdrawalRequest withdrawalRequest)
        {
            var withdrawalId = _common.GetId(transId);
            var result = 0;
            try
            {             
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new
                {
                    _WithdrawalId = withdrawalId,
                    _SiteId = siteId,
                    _Comment = comment,
                    _ForStatus = sForStatus,
                    _Status = (int)status,
                    _WithdrawalInfo = (string)null,
                    _WithdrawalType = (int?)null,
                    _RefId = refId,
                    _Operator = Operator,
                    _ChargeFee = 0
                });
                p.Add("_CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("_UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("_Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = _mainDbClient.Execute("Admin_WithdrawalInfo_Set_V2", p, trans);
                var amount = p.Get<decimal>("_Amount");
                if (result > 0)
                {
                    result = _walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, -1 * amount, trans);

                    if (result > 0)
                    {
                        result = _mainDbClient.Execute("Admin_WithdrawalInfo_FlowEndTime_Set" ,
                            new { _WithdrawalId = withdrawalId }, trans);
                    }

                    // 如果是從 Reject 到 Approved 要再扣玩家一次錢，因為狀態變更成 Reject 的時候有把錢還給玩家
                    if (withdrawalRequest.Status == WithdrawalStatus.Rejected)
                    {
                        const int financialType = 1001;
                        _walletService.SetBalance(withdrawalRequest.CustId, withdrawalRequest.SiteId, withdrawalRequest.UserName, -1 * withdrawalRequest.Amount, financialType,
                            $"Manual Withdrawal Approved: {withdrawalRequest.Amount}", withdrawalRequest.TransId, out _, out _, trans);
                    }

                    SetWithdrawalLog(withdrawalId, siteId, comment, (int)status, Operator, trans);
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
                conn.Close();
                conn.Dispose();
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, "Payment Service");
            }

            return result;
        }

        /// <summary>
        /// 提款單手動 Reject
        /// </summary>
        /// <param name="transId"></param>
        /// <param name="siteId"></param>
        /// <param name="pay"></param>
        /// <param name="comment"></param>
        /// <param name="refId"></param>
        /// <param name="Operator"></param>
        /// <param name="forStatus"></param>
        /// <param name="status"></param>
        /// <param name="withdrawalRequest"></param>
        /// <returns></returns>
        public int SetWithdrawalInfoByManualReject(string transId, int siteId, IPay pay, string comment, string refId, string Operator, IEnumerable<int> forStatus, WithdrawalStatus status, WithdrawalRequest withdrawalRequest)
        {
            var withdrawalId = _common.GetId(transId);
            var result = 0;
            try
            {            
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new
                {
                    _WithdrawalId = withdrawalId,
                    _SiteId = siteId,
                    _Comment = comment,
                    _ForStatus = sForStatus,
                    _Status = (int)status,
                    _WithdrawalInfo = (string)null,
                    _WithdrawalType = (int?)null,
                    _RefId = refId,
                    _Operator = Operator,
                    _ChargeFee = 0
                });
                p.Add("_CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("_UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("_Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = _mainDbClient.Execute("Admin_WithdrawalInfo_Set_V2", p, trans);
                var amount = p.Get<decimal>("_Amount");
                if (result > 0)
                {
                    // 如果提款單原本的狀態是 Approved，然後變更成 Rejected，要把 PayDummy 的金額加回去
                    if (withdrawalRequest.Status == WithdrawalStatus.Approved)
                    {
                        result = _walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, amount, trans);
                    }

                    if (result > 0)
                    {
                        result = _mainDbClient.Execute("Admin_WithdrawalInfo_FlowEndTime_Set",
                            new { _WithdrawalId = withdrawalId }, trans);
                    }

                    // 提款單狀態變成 Reject 都要退玩家錢
                    const int financialType = 1002;
                    _walletService.SetBalance(withdrawalRequest.CustId, withdrawalRequest.SiteId, withdrawalRequest.UserName, withdrawalRequest.Amount, financialType,
                        $"Manual Withdrawal Rejected: {withdrawalRequest.Amount}", withdrawalRequest.TransId, out _, out _, trans);

                    SetWithdrawalLog(withdrawalId, siteId, comment, (int)status, Operator, trans);
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
                conn.Close();
                conn.Dispose();
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, "Payment Service");
            }

            return result;
        }

        public int SetWithdrawalLog(int withdrawalId, int siteId, string comment, int status, string @operator)
        {
            return _mainDbClient.Execute("WithdrawalLog_Ins", new
            {
                WithdrawalId = withdrawalId,
                SiteId = siteId,
                Comment = comment,
                Action = status,
                Operator = @operator
            });
        }

        private int SetWithdrawalLog(int withdrawalId, int siteId, string comment, int status, string @operator, IDbTransaction trans)
        {
            return _mainDbClient.Execute("WithdrawalLog_Ins" ,new
            {
                WithdrawalId = withdrawalId,
                SiteId = siteId,
                Comment = comment,
                Action = status,
                Operator = @operator
            }, trans);
        }

        public int UpdateWithdrawalInfo(string transId, string withdrawalInfo)
        {
            return _mainDbClient.Execute("Service_WithdrawalInfoWithdrawalInfo_Set",
                new
                {
                    _TransId = transId,
                    _WithdrawalInfo = withdrawalInfo
                });
        }

        public string GetWithdrawalInfo(string transId)
        {
            return _mainDbClient.QueryFirst<string>("Service_WithdrawalInfoWithdrawalInfo_Get",
                new
                {
                    _TransId = transId
                });
        }

        public async Task<bool> SetWithdrawalAccountCounter(int custId, string accountNo, string accountType, int counter)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId);
            p.Add("_AccountNo", accountNo);
            p.Add("_AccountType", (int)MapWithdrawalAccountType(accountType));
            p.Add("_Counter", counter);
            return await _mainDbClient.ExecuteAsync("Admin_WithdrawalAccountCounter_Set", p) > 0;
        }

        private WithdrawalAccountTypeEnum MapWithdrawalAccountType(string accountType)
        {
            if (string.IsNullOrEmpty(accountType)) return WithdrawalAccountTypeEnum.BankCard;

            return accountType.ToLower() switch
            {
                "ewallet" => WithdrawalAccountTypeEnum.EWallet,
                "crypto" => WithdrawalAccountTypeEnum.Crypto,
                _ => throw new NotImplementedException("withdrawal accountType not found")
            };
        }
    }
}
