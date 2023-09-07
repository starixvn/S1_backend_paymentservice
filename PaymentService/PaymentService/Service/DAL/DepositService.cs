using Dapper;
using System.Data;
using Newtonsoft.Json;
using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model.enums;
using PaymentService.Service.Model;

namespace PaymentService.Service.DAL
{
    public class DepositService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly Common _common;
        private const int PayTypePosition = 100;
        private readonly ILogger<DepositService> _logger;

        public DepositService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
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

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            return _mainDbClient.QueryUseCommandDefinition<DepositRequest>("Service_DepositRequesPayEnquiry_Get", new DynamicParameters());
        }

        public int SetDepositInfo(string transId, int siteId, IPay pay, decimal actualDeposit, string comment, string refThirdId, string thirdId, string Operator, IEnumerable<int> forStatus, DepositStatus status, Action<string,string,object> publish, bool canReward = false)
        {
            var type = PayTypePosition + pay.PayId;
            var depositId = _common.GetId(transId);
            var logMsg = string.Empty;

            int result;
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                
                var sForStatus = JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new
                {
                    DepositId = depositId,
                    SiteId = siteId,
                    ActualDeposit = actualDeposit,
                    Comment = comment,
                    ForStatus = sForStatus,
                    Status = (int)status,
                    RefThirdId = refThirdId,
                    ThirdId = thirdId,
                    Operator
                });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_DepositInfo_Set_V3", p, trans);
                logMsg += $"transId:{transId} Execute Service_DepositInfo_Set_V3 after result:{result} Step1";

                if (result >= 0)
                {
                    SetDepositLog(depositId, siteId, comment, (int)status, actualDeposit, Operator);

                    if (status == DepositStatus.Approved)
                    {
                        decimal afterAmount = 0;

                        var custId = p.Get<int?>("CustId") ?? 0;
                        var userName = p.Get<string>("UserName");

                        logMsg += $"transId:{transId} _walletService.SetBalance parameter custId:{custId},siteId:{siteId},userName:{userName}," +
                                          $"actualDeposit:{actualDeposit},type:{type},Deposit Approved: {actualDeposit},afterAmount:{afterAmount} Step2-1";


                        result = _walletService.SetBalance(custId, siteId, userName, actualDeposit, type,
                            $"Deposit Approved: {actualDeposit}", transId, out afterAmount, out var is1stDepoist, trans);

                        logMsg += $"transId:{transId} Execute _walletService.SetBalance after result:{result} Step2-2";

                        if (result >= 0)
                        {
                            logMsg += $"transId:{transId} Execute _withdrawalLimitService.SetWithdrawalLimit parameter custId:{custId},userName:{userName},siteId:{siteId}" +
                                              $"actualDeposit:{actualDeposit},WithdrawalLimit: {actualDeposit},Step3-1";

                            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, actualDeposit, (int) WithdrawalLimitAdjustmentReasonEnum.Deposit ,
                                $"WithdrawalLimit: {actualDeposit}", transId, (int)WithdrawalLimitLogCreateFromEnum.Deposit ,Operator , out _, trans);

                            logMsg += $"transId:{transId} Execute _withdrawalLimitService.SetWithdrawalLimit after result:{result} Step3-2";
                        }

                        if (is1stDepoist == 1 && result > 0)
                        {
                            logMsg += $"transId:{transId} Execute SetDepositInfoFirstDeposit parameter depositId:{depositId},siteId:{siteId} Step4-1";

                            result = SetDepositInfoFirstDeposit(depositId, siteId, trans);

                            logMsg += $"transId:{transId} Execute SetDepositInfoFirstDeposit after result:{result} Step4-2";
                        }

                        /*if (result > 0)
                        {
                            logMsg += $"transId:{transId} Execute _walletService.SetPayDummy parameter siteId:{siteId},PayId:{pay.PayId},CurrencyId:{pay.CurrencyId}" +
                                              $"actualDeposit:{actualDeposit} Step5-1";

                            result = _walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, actualDeposit, trans);

                            logMsg += $"transId:{transId} Execute _walletService.SetPayDummy after result:{result} Step5-2";
                        }*/

                        if (result > 0 && canReward)
                        {
                            logMsg += $"transId:{transId} Execute InsertDepositRewardPointQueue parameter Step6-1";

                            var resultCode = InsertDepositRewardPointQueue(transId, trans);
                            if (resultCode != (int)ErrorCode.Success)
                            {
                                _logger.LogError($"InsertDepositRewardPointQueue Failed. ResultCode({resultCode}), TransId: {transId}");
                            }
                            else
                            {
                                result = 1;
                            }

                            logMsg += $"transId:{transId} Execute InsertDepositRewardPointQueue after result:{result} Step6-2";
                        }
                    }

                    if (result > 0)
                    {
                        trans.Commit();
                        result = p.Get<int?>("CustId") ?? result;
                        if (status == DepositStatus.Approved)
                            publish("MemberExchange", "deposit.payment", new { CustId = result });
                    }
                    else
                    {
                        _logger.LogInformation(logMsg); // 沒有正常Commit，將 Log 送到 ELK，以便查問題
                        trans.Rollback();
                    }
                }
                else
                {
                    _logger.LogInformation(logMsg); // 沒有正常Commit，將 Log 送到 ELK，以便查問題
                }
                
                conn.Close();
                conn.Dispose();
            }
            catch (Exception ex)
            {
                SetDepositLog(depositId, siteId, ex.Message, (int)DepositStatus.Error, actualDeposit, "Payment Service");
                throw ex;
            }

            return result;
        }

        public bool SetDepositInfoV2(SetDepositInfoParams input, Action<string, string, object> publish, bool canReward = false)
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

                            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, input.SiteId, input.ActualDeposit, (int) WithdrawalLimitAdjustmentReasonEnum.Deposit,
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
                            publish("MemberExchange", "deposit.payment", new { CustId = custId });
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
            return _mainDbClient.Execute("DepositInfoFirstDeposit_Set", new { DepositId = depositId, SiteId = siteId } ,trans);
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

            _ = _mainDbClient.Execute("Service_DepositRewardPointQueue_Ins", para ,trans);
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

        public IEnumerable<DepositAutoCancel> GetDepositAutoCancelList(int limit)
        {
            return _reportDbClient.QueryUseCommandDefinition<DepositAutoCancel>("Service_DepositAutoCancel_Get", new DynamicParameters(new { _Limit = limit }));
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
