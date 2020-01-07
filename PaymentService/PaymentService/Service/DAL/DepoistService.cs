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
        private int payTypePosition = 100;

        public DepoistService(IConfiguration configuration, WalletService walletService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
        }
        public DepositRequest GetDepositRequest(int depositId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<DepositRequest>("DepositCallBack_Get", new { DepositId = depositId }, commandType: CommandType.StoredProcedure);
        }
        public int SetDepositInfo(string transId, int siteId, IPay pay, decimal actualDeposit, string comment, string thirdId, string Operator, DepositStatus forStatus, DepositStatus status)
        {
            int depositId = 0;
            int type = payTypePosition + pay.PayId;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out depositId);

            int result = 0;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        var p = new DynamicParameters(new { DepositId = depositId, SiteId = siteId, ActualDeposit = actualDeposit, Comment = comment, ForStatus = (int)forStatus, Status = (int)status, ThirdId = thirdId });
                        p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                        result = conn.Execute("DepositInfo_Set", p, trans, commandType: CommandType.StoredProcedure);

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
                }
            }
            catch (Exception ex)
            {
                SetDepositLog(depositId, siteId, ex.Message, (int)DepositStatus.Error, actualDeposit, "Payment Service");
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
    }
}
