using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using PaymentService.Service.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Service.DAL
{
    public class PayService
    {
        private readonly IConfiguration configuration;
        public PayService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BankInfoPay>("Common_BankInfoPay_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BankInfoPay> GetOnlineBankListForPayout()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BankInfoPay>("Common_BankInfoPayForPayout_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<PayCurrency>("PayCurrency_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<PayLanguage> GetPayLanguage()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<PayLanguage>("PayLanguage_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Pay> GetPaymentList(int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Pay>("PaySetting_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<MobilePayInfo> GetMobilePayInfo(int siteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<MobilePayInfo>("Member_MobilePayV2_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
    }
}
