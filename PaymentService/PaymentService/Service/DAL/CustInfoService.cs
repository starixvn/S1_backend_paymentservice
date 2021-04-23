using Dapper;
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
    public class CustInfoService
    {
        private readonly IConfiguration configuration;
        public CustInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public int GetVipLevel(int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("VipLevel_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public Customer GetCustInfo(int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<Customer>("Customer_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public decimal GetBalance(int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
    }
}
