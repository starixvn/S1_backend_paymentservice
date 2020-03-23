using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using PaymentCore;
using PaymentService.Service.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentService.Service.Workers
{
    public class WithdrawalWorkerService : BackgroundService
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<WithdrawalWorkerService> logger;
        private readonly PaymentBLL paymentBLL;
        private readonly PayBLL payBLL;
        private Timer timer;
        public WithdrawalWorkerService(ILogger<WithdrawalWorkerService> logger, IHostEnvironment environment, PaymentBLL paymentBLL, PayBLL payBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.paymentBLL = paymentBLL;
            this.payBLL = payBLL;
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
                var withdrawalRequesList = paymentBLL.GetWithdrawalRequesPayEnquiry();
                foreach (var withdrawalRequest in withdrawalRequesList)
                {
                    try
                    {
                        var payHelper = payBLL.GetPayHelper(withdrawalRequest.PayName);
                        var WithReq = withdrawalRequest.ISetting;
                        if (!WithReq.ContainsKey("TransID"))
                        {
                            WithReq.Add("TransID", withdrawalRequest.TransId);
                        }
                        var enquiryWithdrawResult = payHelper.EnquiryForCredit(WithReq);

                        if (enquiryWithdrawResult.TransStatus == TransStatus.Success)
                        {
                            #region Transaction is Success
                            try
                            {
                                paymentBLL.WithdrawalSuccess(withdrawalRequest, enquiryWithdrawResult);
                            }
                            catch (MySqlException SqlEx)
                            {
                                /*Need Check Transaction Status*/
                                StringBuilder errorMessages = new StringBuilder();
                                //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));
                                errorMessages.AppendLine($"PayName: {withdrawalRequest.PayName}");
                                errorMessages.AppendLine($"Message: {SqlEx.Message}");
                                errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                                errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                                errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                                errorMessages.AppendLine($"Source: {SqlEx.Source}");
                                logger.LogError(errorMessages.ToString());
                            }
                            catch (Exception exForEachTransaction)
                            {
                                logger.LogError($"PayName: {withdrawalRequest.PayName}\r\n{exForEachTransaction.Message}");
                                continue;
                            }
                            #endregion
                        }
                        else if (enquiryWithdrawResult.TransStatus == TransStatus.Fail)
                        {
                            try
                            {
                                paymentBLL.WithdrawalFail(withdrawalRequest, enquiryWithdrawResult);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"PayName: {withdrawalRequest.PayName}\r\n{ex.Message}");
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
                logger.LogError(ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (environment.IsDevelopment() || environment.IsEnvironment("QA"))
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        ~WithdrawalWorkerService()
        {
            timer?.Dispose();
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("WithdrawalWorkerService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
