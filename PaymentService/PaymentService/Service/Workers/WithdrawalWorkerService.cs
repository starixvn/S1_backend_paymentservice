using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using PaymentCore;
using PaymentService.Service.BLL;

namespace PaymentService.Service.Workers
{
    public class WithdrawalWorkerService : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<WithdrawalWorkerService> _logger;
        private readonly PaymentBLL _paymentBll;
        private readonly PayBLL _payBll;
        private readonly IEnumerable<int> _payIds;
        private Timer _timer;
        public WithdrawalWorkerService(IServiceProvider provider)
        {
            this._environment = provider.GetRequiredService<IHostEnvironment>();
            this._logger = provider.GetRequiredService<ILogger<WithdrawalWorkerService>>();
            this._paymentBll = provider.GetRequiredService<PaymentBLL>();
            this._payBll = provider.GetRequiredService<PayBLL>();
            this._payIds = new List<int> { 3, 5, 15, 25, 26 };
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
                var withdrawalRequesList = _paymentBll.GetWithdrawalRequesPayEnquiry(_payIds);
                foreach (var withdrawalRequest in withdrawalRequesList)
                {
                    try
                    {
                        var payHelper = _payBll.GetPayHelper(withdrawalRequest.PayName);
                        var WithReq = withdrawalRequest.ISetting;
                        if (!WithReq.ContainsKey("TransID"))
                        {
                            WithReq.Add("TransID", withdrawalRequest.TransId);
                        }

                        if (!WithReq.ContainsKey("Amount"))
                        {
                            WithReq.Add("Amount", withdrawalRequest.Amount.ToString());
                        }

                        if (!WithReq.ContainsKey("CurrencyId"))
                        {
                            WithReq.Add("CurrencyId", withdrawalRequest.CurrencyId.ToString());
                        }

                        var enquiryWithdrawResult = payHelper.EnquiryForCredit(WithReq);

                        if (enquiryWithdrawResult.TransStatus == TransStatus.Success)
                        {
                            #region Transaction is Success
                            try
                            {
                                _paymentBll.WithdrawalSuccess(withdrawalRequest, enquiryWithdrawResult);
                            }
                            catch (MySqlException SqlEx)
                            {
                                /*Need Check Transaction Status*/
                                var errorMessages = new StringBuilder();
                                //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));
                                errorMessages.AppendLine($"PayName: {withdrawalRequest.PayName}");
                                errorMessages.AppendLine($"Message: {SqlEx.Message}");
                                errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                                errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                                errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                                errorMessages.AppendLine($"Source: {SqlEx.Source}");
                                _logger.LogError(errorMessages.ToString());
                            }
                            catch (Exception exForEachTransaction)
                            {
                                _logger.LogError($"PayName: {withdrawalRequest.PayName}\r\n{exForEachTransaction.Message}");
                                continue;
                            }
                            #endregion
                        }
                        else if (enquiryWithdrawResult.TransStatus == TransStatus.Fail)
                        {
                            try
                            {
                                _paymentBll.WithdrawalFail(withdrawalRequest, enquiryWithdrawResult);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"PayName: {withdrawalRequest.PayName}\r\n{ex.Message}");
                                continue;
                            }
                        }
                    }
                    catch(Exception ex1)
                    {
                        //logger.LogError($"PayName: {withdrawalRequest.PayName}\r\n{ex1.ToString()}");
                        continue;
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
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
}
