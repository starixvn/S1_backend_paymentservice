using PaymentService.Service.BLL;
using PaymentService.Service.Model;

namespace PaymentService.Service.Workers
{
    public class DepositAutoCancelWorkerService : BackgroundService
    {
        private readonly ILogger<DepositAutoCancelWorkerService> logger;
        private readonly PaymentBLL paymentBLL;
        private Timer timer;
        private readonly Random _random;

        public DepositAutoCancelWorkerService(ILogger<DepositAutoCancelWorkerService> logger, PaymentBLL paymentBLL)
        {
            this.logger = logger;
            this.paymentBLL = paymentBLL;
            _random = new Random();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            try
            {
                var depositAutoCancelList = paymentBLL.GetDepositAutoCancelList(_random.Next(950, 1000));
                foreach (var depositAutoCancel in depositAutoCancelList)
                {
                    try
                    {
                        paymentBLL.DepositAutoCancel(depositAutoCancel.DepositId, depositAutoCancel.SiteId, depositAutoCancel.CustId, "AutoCancel");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"DepositAutoCancel Fail\r\nTransId: {depositAutoCancel.TransId}\r\n{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(5);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("DepositAutoCancelWorkerService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
