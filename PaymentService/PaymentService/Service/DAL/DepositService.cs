using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using PaymentService.Extensions;
using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model.enums;
using PaymentService.Service.Model;

namespace PaymentService.Service.DAL
{
    public class DepositService
    {
        private readonly string _connectionString;
        private readonly MySqlClient _mainDbClient;
        private readonly WalletService _walletService;
        private readonly WithdrawalLimitService _withdrawalLimitService;
        private readonly Common _common;
        private const int PayTypePosition = 100;

        public DepositService(IServiceProvider provider)
        {
            _connectionString = ServiceCollectionExtension.ConnectionString;
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _walletService = provider.GetRequiredService<WalletService>();
            _withdrawalLimitService = provider.GetRequiredService<WithdrawalLimitService>();
            _common = provider.GetRequiredService<Common>();
        }
        public DepositRequest GetDepositRequest(int depositId)
        {
            return _mainDbClient.QueryFirst<DepositRequest>("DepositCallBack_Get", new { DepositId = depositId });
        }
        
        public DepositInfoData GetDepositInfo(int depositId)
        {
            return _mainDbClient.QueryFirst<DepositInfoData>("Service_DepositInfo_Get", new { DepositId = depositId });
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            return _mainDbClient.Query<DepositRequest>("Service_DepositRequesPayEnquiry_Get");
        }

        public int SetDepositInfo(string transId, int siteId, IPay pay, decimal actualDeposit, string comment, string refThirdId, string thirdId, string Operator, IEnumerable<int> forStatus, DepositStatus status)
        {
            var type = PayTypePosition + pay.PayId;
            var depositId = _common.GetId(transId);

            int result;
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var trans = conn.BeginTransaction();
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
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
                result = _mainDbClient.Execute("Service_DepositInfo_Set_V2", p, conn, trans);

                if (result > 0)
                {
                    SetDepositLog(depositId, siteId, comment, (int)status, actualDeposit, Operator);

                    if (status == DepositStatus.Approved)
                    {
                        decimal afterAmount = 0;

                        var custId = p.Get<int?>("CustId") ?? 0;
                        var userName = p.Get<string>("UserName");
                        result = _walletService.SetBalance(custId, siteId, userName, actualDeposit, type,
                            $"Deposit Approved: {actualDeposit}", transId, out afterAmount, out var is1stDepoist, conn,
                            trans);

                        if (result > 0)
                        {
                            result = _withdrawalLimitService.SetWithdrawalLimit(custId, userName, siteId, actualDeposit,
                                $"WithdrawalLimit: {actualDeposit}", transId, out _, conn, trans);
                        }

                        if (is1stDepoist == 1 && result > 0)
                        {
                            result = SetDepositInfoFirstDeposit(depositId, siteId, conn, trans);
                        }
                        if (result > 0)
                        {
                            result = _walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, actualDeposit, conn, trans);
                        }
                    }

                    if (result > 0)
                    {
                        trans.Commit();
                        result = p.Get<int?>("CustId") ?? result;
                    }
                }
            }
            catch (Exception ex)
            {
                SetDepositLog(depositId, siteId, ex.Message, (int)DepositStatus.Error, actualDeposit, "Payment Service");
                throw ex;
            }

            return result;
        }
        private int SetDepositInfoFirstDeposit(int depositId, int siteId, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("DepositInfoFirstDeposit_Set", new { DepositId = depositId, SiteId = siteId },
                conn, trans);
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
    }
}
