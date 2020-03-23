using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using PaymentService.Service.Model;
using PaymentService.Service.Model.enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PaymentService.Service.DAL
{
    public class WithdrawalService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;

        public WithdrawalService(IConfiguration configuration, WalletService walletService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
        }
        public WithdrawalRequest GetWithdrawalRequest(int withdrawalId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<WithdrawalRequest>("WithdrawalRequesPay_Get", new { WithdrawalId = withdrawalId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<WithdrawalRequest>("Service_WithdrawalRequesPayEnquiry_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetWithdrawalInfo(string transId, int siteId, IPay pay, string comment, string refId, string Operator, IEnumerable<int> forStatus, WithdrawalStatus status)
        {
            int withdrawalId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out withdrawalId);
            int custId = 0;
            decimal amount = 0m;
            int result = 0;
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                string sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, ForStatus = sForStatus, Status = (int)status, WithdrawalInfo = (string)null, WithdrawalType = (int?)null, RefId = refId });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = conn.Execute("WithdrawalInfo_Set", p, trans, commandType: CommandType.StoredProcedure);
                custId = p.Get<int?>("CustId") ?? 0;
                string UserName = p.Get<string>("UserName");
                amount = p.Get<decimal>("Amount");
                if (result > 0)
                {
                    if (status == WithdrawalStatus.Approved)
                    {
                        result = walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, -1 * amount, conn, trans);
                    }

                    SetWithdrawalLog(withdrawalId, siteId, comment, (int)status, Operator);
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                SetWithdrawalLog(withdrawalId, siteId, ex.Message, (int)WithdrawalStatus.Error, "Payment Service");
            }

            return result;
        }


        private int SetWithdrawalLog(int withdrawalId, int siteId, string comment, int status, string Operator)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("WithdrawalLog_Ins", new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, Action = status, Operator }, commandType: CommandType.StoredProcedure);
        }
    }
}
