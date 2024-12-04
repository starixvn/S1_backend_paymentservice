using System.Text;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using PaymentCore;
using PaymentService.Service.BLL;
using Serilog.Context;

namespace PaymentService.Service.Workers;

public class WithdrawalWorkerService : BackgroundService
{
    private readonly string _environmentName;
    private readonly ILogger<WithdrawalWorkerService> _logger;
    private readonly PaymentBLL _paymentBll;
    private readonly PayBLL _payBll;
    private readonly IEnumerable<int> _payIds;
    private Timer _timer;
    public WithdrawalWorkerService(IServiceProvider provider, IConfiguration configuration)
    {
        _environmentName = configuration.GetValue<string>("EnvironmentName");
        _logger = provider.GetRequiredService<ILogger<WithdrawalWorkerService>>();
        _paymentBll = provider.GetRequiredService<PaymentBLL>();
        _payBll = provider.GetRequiredService<PayBLL>();
        _payIds = new List<int> { 3, 5, 15, 25, 74 };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }
    private void DoWork(object state)
    {
        LogContext.PushProperty("WorkId", Guid.NewGuid());
        try
        {
            var withdrawalList = _paymentBll.GetWithdrawalRequesPayEnquiry(_payIds);

            if (withdrawalList.Any())
            {
                _logger.LogInformation("Start Withdrawal Worker" +
                                       $", TransId List: {string.Join(',', withdrawalList.Select(x => x.TransId))}");
            }

            foreach (var withdrawalRequest in withdrawalList)
            {
                try
                {
                    var payHelper = _payBll.GetPayHelper(withdrawalRequest.PayName);
                    var withReq = withdrawalRequest.ISetting;
                    if (!withReq.ContainsKey("TransID"))
                    {
                        withReq.Add("TransID", withdrawalRequest.TransId);
                    }

                    if (!withReq.ContainsKey("Amount"))
                    {
                        withReq.Add("Amount", withdrawalRequest.Amount.ToString());
                    }

                    if (!withReq.ContainsKey("CurrencyId"))
                    {
                        withReq.Add("CurrencyId", withdrawalRequest.CurrencyId.ToString());
                    }

                    var enquiryWithdrawResult = payHelper.EnquiryForCredit(withReq);

                    if (enquiryWithdrawResult.TransStatus == TransStatus.Success)
                    {
                        #region Transaction is Success
                        try
                        {
                            _paymentBll.WithdrawalSuccess(withdrawalRequest, enquiryWithdrawResult);
                            _logger.LogInformation($"Update {withdrawalRequest.TransId} to Success" +
                                                   $", Query Result: {JsonConvert.SerializeObject(enquiryWithdrawResult)}");
                        }
                        catch (MySqlException sqlEx)
                        {
                            /*Need Check Transaction Status*/
                            var errorMessages = new StringBuilder();
                            errorMessages.AppendLine($"PayName: {withdrawalRequest.PayName}");
                            errorMessages.AppendLine($"Message: {sqlEx.Message}");
                            errorMessages.AppendLine($"Error Number: {sqlEx.Number}");
                            errorMessages.AppendLine($"ErrorCode: {sqlEx.ErrorCode}");
                            errorMessages.AppendLine($"SqlState: {sqlEx.SqlState}");
                            errorMessages.AppendLine($"Source: {sqlEx.Source}");

                            _logger.LogError(errorMessages.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Update {withdrawalRequest.TransId} to Success Failed. " +
                                             $"Exception: {ex.Message}\r\n{ex.StackTrace}");
                        }
                        #endregion
                    }
                    else if (enquiryWithdrawResult.TransStatus == TransStatus.Fail)
                    {
                        try
                        {
                            _paymentBll.WithdrawalFail(withdrawalRequest, enquiryWithdrawResult);
                            _logger.LogInformation($"Update {withdrawalRequest.TransId} to Fail" +
                                $", Query Result: {JsonConvert.SerializeObject(enquiryWithdrawResult)}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Update {withdrawalRequest.TransId} to Fail Failed. " +
                                             $"Exception: {ex.Message}\r\n{ex.StackTrace}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Query {withdrawalRequest.TransId} Failed. " +
                                     $"Exception: {ex.Message}\r\n{ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Withdrawal Worker Exception: {ex.Message}\r\n{ex.StackTrace}");
        }
        finally
        {
            var dueTimeSpan = TimeSpan.FromSeconds(30);

            if (!_environmentName.Equals("PROD"))
            {
                dueTimeSpan = TimeSpan.FromSeconds(5);
            }

            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }
    ~WithdrawalWorkerService()
    {
        _timer?.Dispose();
    }
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WithdrawalWorkerService: Process Stopped");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}
