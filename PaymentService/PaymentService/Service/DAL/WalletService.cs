using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Model;
using PaymentService.Service.Library;

namespace PaymentService.Service.DAL
{
    public class WalletService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public WalletService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo, string refTransId, out decimal afterAmount, out byte is1StDeposit, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount = amount, Type = type, Memo = memo, RefTransId = refTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Wallet_Set", p, conn, trans);
            is1StDeposit = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;
            return afterAmount >= 0 ? 1 : 0;
        }
        public decimal GetBalance(int custId)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }

        public int SetPayDummy(int siteId, int payId, int currencyId, decimal amount, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("PayDummy_Set", new { SiteId = siteId, PayId = payId, CurrencyId = currencyId, Amount = amount }, conn, trans);
        }
        public int SetBankOffDummy(int siteId, int sysId, string accountNo, int currencyId, decimal amount, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("BankOffDummy_Set", new { SiteId = siteId, SysId = sysId, AccountNo = accountNo, CurrencyId = currencyId, Amount = amount }, conn, trans);
        }
    }
}
