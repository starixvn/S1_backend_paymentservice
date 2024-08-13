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

    public int Set(int custId, TaskSetting firstDepositTime,DateTimeOffset? taskTime)
    {
        var taskTimeValue = taskTime.HasValue ? taskTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var p = new DynamicParameters(new
        {
            _CustId = custId,
            _TaskId = (int)firstDepositTime,
            _TaskTime = taskTimeValue
        });

        return _mainDbClient.Execute("Service_CustTaskTime_Set", p);
    }
}