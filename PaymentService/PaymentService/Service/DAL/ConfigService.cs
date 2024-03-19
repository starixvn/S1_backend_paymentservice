using PaymentService.Model;
using PaymentService.Service.Library;

namespace PaymentService.Service.DAL;

public class ConfigService
{
    private readonly MySqlClient _mainDbClient;

    public ConfigService(IServiceProvider provider)
    {
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public IEnumerable<T> Query<T>(string sql, object parameters = null)
    {
        return _mainDbClient.Query<T>(sql, parameters);
    }
}