using PaymentService.Service.DAL;
using PaymentService.Service.Model.enums;

namespace PaymentService.Service.BLL;

public class TaskSettingBLL
{
    private readonly TaskSettingService _TaskSettingService;

    public TaskSettingBLL(IServiceProvider provider)
    {
        _TaskSettingService = provider.GetService<TaskSettingService>();
    }

    public int Set(int custId, TaskSetting firstDepositTime, DateTimeOffset taskTime)
    {
        return _TaskSettingService.Set(custId, firstDepositTime, taskTime);
    }
}