using System.Text;

using MySql.Data.MySqlClient;

using PaymentCore;

using PaymentService.Service.BLL;
using Serilog.Context;

namespace PaymentService.Service.Workers
{
    public class DepositWorkerService : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<DepositWorkerService> _logger;
        private readonly PaymentBLL _paymentBll;
        private readonly PayBLL _payBll;
        private Timer _timer;
        private readonly IEnumerable<int> _payIds;
        public DepositWorkerService(ILogger<DepositWorkerService> logger, IHostEnvironment environment, PaymentBLL paymentBll, PayBLL payBll)
        {
            _environment = environment;
            _logger = logger;
            _paymentBll = paymentBll;
            _payBll = payBll;
            _payIds = new List<int> { 3, 4, 5, 32, 63 };
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
                var depositRequesList = _paymentBll.GetDepositRequesPayEnquiry(_payIds);
                foreach (var depositRequest in depositRequesList)
                {
                    try
                    {
                        var payHelper = _payBll.GetPayHelper(depositRequest.PayName);
                        var DepReq = depositRequest.ISetting;
                        if (!DepReq.ContainsKey("TransID"))
                        {
                            DepReq.Add("TransID", depositRequest.TransId);
                        }

                        if (!DepReq.ContainsKey("Amount"))
                        {
                            DepReq.Add("Amount", depositRequest.Amount.ToString());
                        }

                        if (!DepReq.ContainsKey("CurrencyId"))
                        {
                            DepReq.Add("CurrencyId", depositRequest.CurrencyId.ToString());
                        }

                        if (!DepReq.ContainsKey("ThirdId"))
                        {
                            DepReq.Add("ThirdID", depositRequest.ThirdId);
                        }

                        var enquiryDepositResult = payHelper.EnquiryForDebit(DepReq);

                        if (enquiryDepositResult.TransStatus == TransStatus.Success)
                        {
                            #region Transaction is Success
                            try
                            {
                                _paymentBll.DepositSuccess(depositRequest, enquiryDepositResult);
                            }
                            catch (MySqlException SqlEx)
                            {
                                /*Need Check Transaction Status*/
                                var errorMessages = new StringBuilder();
                                //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));
                                errorMessages.AppendLine($"PayName: {depositRequest.PayName}");
                                errorMessages.AppendLine($"Message: {SqlEx.Message}");
                                errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                                errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                                errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                                errorMessages.AppendLine($"Source: {SqlEx.Source}");
                                _logger.LogError(SqlEx, errorMessages.ToString());
                            }
                            catch (Exception exForEachTransaction)
                            {
                                _logger.LogError(exForEachTransaction, $"PayName: {depositRequest.PayName}\r\n{exForEachTransaction.Message}");
                                continue;
                            }
                            #endregion
                        }
                        else if (enquiryDepositResult.TransStatus == TransStatus.Fail)
                        {
                            try
                            {
                                _paymentBll.DepositFail(depositRequest, enquiryDepositResult);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"PayName: {depositRequest.PayName}\r\n{ex.Message}");
                                continue;
                            }
                        }
                    }
                    catch (NotImplementedException niex)
                    {
                        continue;
                    }
                    catch (Exception ex1)
                    {
                        //logger.LogError($"PayName: {depositRequest.PayName}\r\n{ex1.ToString()}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromSeconds(30);
                if (_environment.IsDevelopment() || _environment.IsEnvironment("QA"))
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        ~DepositWorkerService()
        {
            _timer?.Dispose();
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DepositWorkerService: Process Stopped");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
