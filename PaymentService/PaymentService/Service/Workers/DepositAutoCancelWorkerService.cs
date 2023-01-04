using PaymentService.Service.BLL;
using PaymentService.Service.Model;

namespace PaymentService.Service.Workers
{
    public class DepositAutoCancelWorkerService : BackgroundService
    {
        private readonly ILogger<DepositAutoCancelWorkerService> _logger;
        private readonly PaymentBLL _paymentBLL;
        private Timer _timer;
        private readonly Random _random;

        public DepositAutoCancelWorkerService(ILogger<DepositAutoCancelWorkerService> logger, PaymentBLL paymentBLL)
        {
            _logger = logger;
            _paymentBLL = paymentBLL;
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            try
            {
                _logger.LogInformation("Deposit Auto Cancel Task Start");
                var depositAutoCancelList = _paymentBLL.GetDepositAutoCancelList(_random.Next(950, 1000)).ToList();
                _logger.LogInformation($"GetDepositAutoCancelList Data Count：{depositAutoCancelList.Count}");
                foreach (var depositAutoCancel in depositAutoCancelList)
                {
                    try
                    {
                        _paymentBLL.DepositAutoCancel(depositAutoCancel.DepositId, "System");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"DepositAutoCancel Fail\r\nTransId: {depositAutoCancel.TransId}\r\n{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(5);
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
