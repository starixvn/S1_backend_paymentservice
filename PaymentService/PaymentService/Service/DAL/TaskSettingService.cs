using Dapper;
using PaymentService.Model;
using PaymentService.Service.Library;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.DAL;

public class TaskSettingService
{
    private readonly MySqlClient _mainDbClient;

    public TaskSettingService(IServiceProvider serviceProvider)
    {
        _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public int Set(int custId, TaskSetting taskSetting, DateTimeOffset taskTime, string transId)
    {
        var p = new DynamicParameters(new
        {
            _CustId = custId,
            _TaskId = (int)taskSetting,
            _TaskTime = taskTime.UtcDateTime,
            _RefId = transId
        });

        return _mainDbClient.Execute("Service_CustTaskTime_Set_V2", p);
    }
}