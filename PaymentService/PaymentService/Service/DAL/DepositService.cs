using System.Data;
using Dapper;
using Newtonsoft.Json;
using PaymentCore.Model;
using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.DAL
{
    public class DepositService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly CustInfoService _custInfoService;
        private readonly Common _common;
        private const int PayTypePosition = 100;
        private readonly ILogger<DepositService> _logger;

        public DepositService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _common = provider.GetRequiredService<Common>();
            _logger = provider.GetRequiredService<ILogger<DepositService>>();
        }

        public DepositRequest GetDepositRequest(int depositId)
        {
            return _mainDbClient.QueryFirst<DepositRequest>("DepositCallBack_Get", new { DepositId = depositId });
        }

        public DepositInfoModel GetDepositInfo(int depositId)
        {
            return _mainDbClient.QueryFirst<DepositInfoModel>("Service_DepositInfo_Get", new { DepositId = depositId });
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry(string payIds)
        {
            return _mainDbClient.QueryUseCommandDefinition<DepositRequest>("Service_DepositRequestPayEnquiry_Get",
                new DynamicParameters(new { _PayIds = payIds }));
        }
        
        public bool SetDepositInfoV2(SetDepositInfoParams input, bool canReward = false)
        {
            var type = PayTypePosition + input.PayId;
            var depositId = _common.GetId(input.TransId);
            var logMsg = string.Empty;
            var custId = 0;
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    DepositId = depositId,
                    input.SiteId,
                    input.ActualDeposit,
                    input.Comment,
                    ForStatus = JsonConvert.SerializeObject(input.ForStatus),
                    Status = (int)input.Status,
                    RefThirdId = input.ThirdId, // 看起來原本RefThirdId根本沒用
                    input.ThirdId,
                    input.Operator
                });

                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);

                var result = _mainDbClient.Execute("Service_DepositInfo_Set_V3", p, trans);
                logMsg += $"transId:{input.TransId} Execute Service_DepositInfo_Set_V3 after result:{result} Step1";

                if (result >= 0)
                {
                    SetDepositLog(depositId, input.SiteId, input.Comment, (int)input.Status, input.ActualDeposit, input.Operator);

                    if (input.Status == DepositStatus.Approved)
                    {
                        decimal afterAmount = 0;

                        custId = p.Get<int?>("CustId") ?? 0;
                        var userName = p.Get<string>("UserName");

                        logMsg += Environment.NewLine +
                            $"transId:{input.TransId} _walletService.SetBalance parameter custId:{custId}, siteId:{input.SiteId}, userName:{userName}, " +
                            $"actualDeposit:{input.ActualDeposit}, type:{type} Step2-1";

                        result = _walletService.SetBalance(custId, input.SiteId, userName, input.ActualDeposit, type,
                            $"Deposit Approved: {input.ActualDeposit}", input.TransId, out afterAmount, out var is1stDepoist, trans);

                        logMsg += Environment.NewLine +
                            $"transId:{input.TransId} Execute _walletService.SetBalance after result:{result}, afterAmount:{afterAmount} Step2-2";

                        if (result >= 0)
                        {
                            logMsg += Environment.NewLine +
                                $"transId:{input.TransId} Execute _withdrawalLimitService.SetWithdrawalLimit parameter custId:{custId}, userName:{userName}, siteId:{input.SiteId}" +
                                $"actualDeposit:{input.ActualDeposit}, WithdrawalLimit:{input.ActualDeposit} Step3-1";

                            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, input.SiteId, input.ActualDeposit, (int)WithdrawalLimitAdjustmentReasonEnum.Deposit,
                                $"WithdrawalLimit: {input.ActualDeposit}", input.TransId, (int)WithdrawalLimitLogCreateFromEnum.Deposit, input.Operator, out _, trans);

                            logMsg += Environment.NewLine +
                                $"transId:{input.TransId} Execute _withdrawalLimitService.SetWithdrawalLimit after result:{result} Step3-2";
                        }

                        if (is1stDepoist == 1 && result > 0)
                        {
                            logMsg += Environment.NewLine +
                                $"transId:{input.TransId} Execute SetDepositInfoFirstDeposit parameter depositId:{depositId}, siteId:{input.SiteId} Step4-1";

                            result = SetDepositInfoFirstDeposit(depositId, input.SiteId, trans);

                            logMsg += Environment.NewLine +
                                $"transId:{input.TransId} Execute SetDepositInfoFirstDeposit after result:{result} Step4-2";
                        }

                        if (result > 0 && canReward)
                        {
                            logMsg += Environment.NewLine +
                                $"transId:{input.TransId} Execute InsertDepositRewardPointQueue parameter Step5-1";

                            var resultCode = InsertDepositRewardPointQueue(input.TransId, trans);
                            if (resultCode != (int)ErrorCode.Success)
                            {
                                _logger.LogError($"InsertDepositRewardPointQueue Failed. ResultCode({resultCode}), TransId: {input.TransId}");
                            }
                            else
                            {
                                result = 1;
                            }

                            logMsg += Environment.NewLine +
                                $"transId:{input.TransId} Execute InsertDepositRewardPointQueue after result:{result} Step5-2";
                        }
                    }

                    if (result > 0)
                    {
                        trans.Commit();

                        if (input.Status == DepositStatus.Approved)
                        {
                            var custVerificationReward = _custInfoService.GetCustVerificationReward(custId);
                            if (custVerificationReward is null or {EmailVerified: true, EmailReward: 0} or {PhoneVerified: true, PhoneReward: 0})
                            {
                                _custInfoService.InsertQueueJob(custId, QueueJob.Verification, string.Empty);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation(logMsg);
                        trans.Rollback();
                    }
                }
                else
                {
                    _logger.LogInformation(logMsg);
                }

                conn.Close();
                conn.Dispose();
            }
            catch (Exception ex)
            {
                SetDepositLog(depositId, input.SiteId, ex.Message, (int)DepositStatus.Error, input.ActualDeposit, "Payment Service");
                _logger.LogError(ex, $"exception: {ex.Message}, logMsg: {logMsg}");

                return false;
            }

            return true;
        }
        
        private int SetDepositInfoFirstDeposit(int depositId, int siteId, IDbTransaction trans)
        {
            return _mainDbClient.Execute("DepositInfoFirstDeposit_Set", new { DepositId = depositId, SiteId = siteId }, trans);
        }

        public int SetDepositLog(int depositId, int siteId, string comment, int status, decimal actualDeposit, string @operator)
        {
            return _mainDbClient.Execute("DepositLog_Ins",
                new
                {
                    DepositId = depositId,
                    SiteId = siteId,
                    Comment = comment,
                    Action = status,
                    ActualDeposit = actualDeposit,
                    Operator = @operator
                });
        }

        private int InsertDepositRewardPointQueue(string transId, IDbTransaction trans)
        {
            var para = new DynamicParameters();
            para.Add("_TransId", transId);
            para.Add("_Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            _ = _mainDbClient.Execute("Service_DepositRewardPointQueue_Ins", para, trans);
            var resultCode = para.Get<int?>("_Result") ?? -9;

            return resultCode;
        }

        public int SetThirdId(string transId, string thirdId)
        {
            return _mainDbClient.Execute("Service_DepositInfoThirdID_Set", new { TransId = transId, ThirdId = thirdId });
        }

        public int UpdateDepositInfo(string transId, string depositInfo)
        {
            return _mainDbClient.Execute("Service_DepositInfoDepositInfo_Set",
                new
                {
                    _TransId = transId,
                    _DepositInfo = depositInfo
                });
        }

        public IEnumerable<int> GetDepositAutoCancelList(int limit)
        {
            return _reportDbClient.QueryUseCommandDefinition<int>("Service_DepositAutoCancel_Get",
                new DynamicParameters(new { _Limit = limit }));
        }

        public int DepositAutoCancel(int depositId, string Operator)
        {
            return _mainDbClient.Execute("Service_DepositAutoCancel_Set",
                new
                {
                    _DepositId = depositId,
                    _Operator = Operator
                });
        }
    }
}
