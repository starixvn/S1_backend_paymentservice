using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PaymentService.Service.Model.enums;
using PaymentService.Service.Model;

namespace PaymentService.Service.DAL
{
    public class DepoistService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly WithdrawalLimitService withdrawalLimitService;
        private int payTypePosition = 100;

        public DepoistService(IConfiguration configuration, WalletService walletService, WithdrawalLimitService withdrawalLimitService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.withdrawalLimitService = withdrawalLimitService;
        }
        public DepositRequest GetDepositRequest(int depositId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<DepositRequest>("DepositCallBack_Get", new { DepositId = depositId }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<DepositRequest> GetDepositRequesPayEnquiry()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<DepositRequest>("Service_DepositRequesPayEnquiry_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetDepositInfo(string transId, int siteId, IPay pay, decimal actualDeposit, string comment, string refThirdId, string thirdId, string Operator, IEnumerable<int> forStatus, DepositStatus status)
        {
            int depositId = 0;
            int type = payTypePosition + pay.PayId;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);

            int result = 0;
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                string sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new { DepositId = depositId, SiteId = siteId, ActualDeposit = actualDeposit, Comment = comment, ForStatus = sForStatus, Status = (int)status, RefThirdId = refThirdId, ThirdId = thirdId, Operator = Operator });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                result = conn.Execute("Service_DepositInfo_Set", p, trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    SetDepositLog(depositId, siteId, comment, (int)status, actualDeposit, Operator);

                    if (status == DepositStatus.Approved)
                    {
                        byte is1stDepoist = 0;
                        decimal afterAmount = 0;

                        int custId = p.Get<int?>("CustId") ?? 0;
                        string UserName = p.Get<string>("UserName");
                        result = walletService.SetBalance(custId, siteId, UserName, actualDeposit, type, $"Deposit Approved: {actualDeposit}", transId, out afterAmount, out is1stDepoist, conn, trans);

                        if (result > 0)
                        {
                            decimal withdrawalLimit = 0;
                            result = withdrawalLimitService.SetWithdrawalLimit(custId, UserName, siteId, actualDeposit, $"WithdrawalLimit: {actualDeposit}", transId, out withdrawalLimit, conn, trans);
                        }

                        if (is1stDepoist == 1 && result > 0)
                        {
                            result = SetDepositInfoFirstDeposit(depositId, siteId, conn, trans);
                        }
                        if (result > 0)
                        {
                            result = walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, actualDeposit, conn, trans);
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
            return conn.Execute("DepositInfoFirstDeposit_Set", new { DepositId = depositId, SiteId = siteId }, trans, commandType: CommandType.StoredProcedure);
        }
        private int SetDepositLog(int depositId, int siteId, string comment, int status, decimal actualDeposit, string Operator)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("DepositLog_Ins", new { DepositId = depositId, SiteId = siteId, Comment = comment, Action = status, ActualDeposit = actualDeposit, Operator }, commandType: CommandType.StoredProcedure);
        }

        public int SetThirdId(string transId, string thirdId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_DepositInfoThirdID_Set", new { TransId = transId, ThirdId = thirdId }, commandType: CommandType.StoredProcedure);
        }
    }
}
