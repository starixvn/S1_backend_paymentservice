using System.Data;
using System.Diagnostics;

using Dapper;

using MySql.Data.MySqlClient;

using PaymentService.Model;

using Serilog.Context;

namespace PaymentService.Service.Library
{
    public sealed class MySqlClient
    {
        public DataBase DataBase { get; set; }

        private readonly string _connectionString;
        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
        }

        public MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public T QueryFirst<T>(string name, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                var result = connection.QueryFirstOrDefault<T>(command);

                connection.Close();
                connection.Dispose();

                LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }

        public IEnumerable<T> Query<T>(string name, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                var result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();

                LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }

        public IEnumerable<T> QueryUseCommandDefinition<T>(string name, object parameters = null, int? commandTimeout = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open)
                    connection.Open();// 自己 Control connection open close，不要讓 ORM 自己做

                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                var result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
                
                LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }

        public int Execute(string name, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var result = connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();

                LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }

        public int Execute(string name, object parameters, IDbTransaction transaction)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = transaction.Connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);

                LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }

        public async Task<int> ExecuteAsync(string name, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                var result = await connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();

                LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }

        public T ExecuteScalar<T>(string name, object parameters)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var result = connection.ExecuteScalar<T>(name, parameters,
                    commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();

                LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
                _logger.LogDebug($"{name} {sw.Elapsed:c}");
                
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
        }
    }
}