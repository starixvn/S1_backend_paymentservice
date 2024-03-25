using Newtonsoft.Json;
using PaymentService.Service.BLL;
using Serilog.Context;

namespace PaymentService.Service.Workers
{
    public class DepositAutoCancelWorkerService : BackgroundService
    {
        private readonly ILogger<DepositAutoCancelWorkerService> _logger;
        private readonly PaymentBLL _paymentBll;
        private Timer _timer;
        private readonly Random _random;

        public DepositAutoCancelWorkerService(ILogger<DepositAutoCancelWorkerService> logger, PaymentBLL paymentBll)
        {
            _logger = logger;
            _paymentBll = paymentBll;
            _random = new Random();
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
                _logger.LogInformation("Deposit Auto Cancel Task Start");
                
                var r = _random.Next(950, 1000);
                var depositAutoCancelList = _paymentBll.GetDepositAutoCancelList(r);

                var list = depositAutoCancelList.ToList();
                
                _logger.LogInformation($"GetDepositAutoCancelList Data Count：{list.Count}");
                foreach (var deposit in list)
                {
                    try
                    {
                        _paymentBll.DepositAutoCancel(deposit.DepositId, "System");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"DepositAutoCancel Fail\r\nDepositId: {deposit.DepositId}\r\n{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DepositAutoCancelWorkerService: Process Stopped");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
