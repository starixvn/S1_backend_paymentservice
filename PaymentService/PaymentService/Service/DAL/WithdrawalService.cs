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
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Model;
using PaymentService.Service.Library;

namespace PaymentService.Service.DAL
{
    public class WithdrawalService
    {
        private readonly string _connectionString;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly WalletService _walletService;
        
        public WithdrawalService(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _walletService = provider.GetRequiredService<WalletService>();
        }
        public WithdrawalRequest GetWithdrawalRequest(int withdrawalId)
        {
            return _reportDbClient.QueryFirst<WithdrawalRequest>("WithdrawalRequesPay_Get", new { WithdrawalId = withdrawalId });
        }
        public IEnumerable<WithdrawalRequest> GetWithdrawalRequesPayEnquiry()
        {
            return _reportDbClient.Query<WithdrawalRequest>("Service_WithdrawalRequesPayEnquiry_Get");
        }

        public int SetWithdrawalInfo(string transId, int siteId, IPay pay, string comment, string refId, string Operator, IEnumerable<int> forStatus, WithdrawalStatus status)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var withdrawalId);
            var result = 0;
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var trans = conn.BeginTransaction();
                var sForStatus = Newtonsoft.Json.JsonConvert.SerializeObject(forStatus);
                var p = new DynamicParameters(new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, ForStatus = sForStatus, Status = (int)status, WithdrawalInfo = (string)null, WithdrawalType = (int?)null, RefId = refId, Operator = Operator });
                p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("UserName", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("Amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Admin_WithdrawalInfo_Set", p, conn, trans);
                var amount = p.Get<decimal>("Amount");
                if (result > 0)
                {
                    if (status == WithdrawalStatus.Approved)
                    {
                        result = _walletService.SetPayDummy(siteId, pay.PayId, pay.CurrencyId, -1 * amount, conn, trans);
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
            return _mainDbClient.Execute("WithdrawalLog_Ins", new { WithdrawalId = withdrawalId, SiteId = siteId, Comment = comment, Action = status, Operator });
        }
    }
}
