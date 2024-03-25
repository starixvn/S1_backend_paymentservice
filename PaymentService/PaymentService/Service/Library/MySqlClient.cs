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
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.QueryFirstOrDefault<T>(command);

                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public IEnumerable<T> Query<T>(string name, object parameters = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result.ToList();
        }

        public IEnumerable<T> QueryUseCommandDefinition<T>(string name, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();// 自己 Control connection open close，不要讓 ORM 自己做

                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string name, object parameters = null)
        {
            var result = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string name, object parameters, IDbTransaction transaction)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                result = transaction.Connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<int> ExecuteAsync(string name, object parameters = null)
        {
            var result = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public T ExecuteScalar<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.ExecuteScalar<T>(name, parameters,
                    commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
    }
}