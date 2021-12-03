using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using PaymentCore;
using PaymentService.Service.BLL;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentService.Service.Workers
{
    public class DepositWorkerService : BackgroundService
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<DepositWorkerService> logger;
        private readonly PaymentBLL paymentBLL;
        private readonly PayBLL payBLL;
        private Timer timer;
        public DepositWorkerService(ILogger<DepositWorkerService> logger, IHostEnvironment environment, PaymentBLL paymentBLL, PayBLL payBLL)
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
                var depositRequesList = paymentBLL.GetDepositRequesPayEnquiry();
                foreach (var depositRequest in depositRequesList)
                {
                    try
                    {
                        var payHelper = payBLL.GetPayHelper(depositRequest.PayName);
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
                            DepReq.Add("ThirdID", depositRequest.ThirdId.ToString());
                        }

                        var enquiryDepositResult = payHelper.EnquiryForDebit(DepReq);
                        
                        if (enquiryDepositResult.TransStatus == TransStatus.Success)
                        {
                            #region Transaction is Success
                            try
                            {
                                paymentBLL.DepositSuccess(depositRequest, enquiryDepositResult);
                            }
                            catch (MySqlException SqlEx)
                            {
                                /*Need Check Transaction Status*/
                                StringBuilder errorMessages = new StringBuilder();
                                //errorMessages.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(dataParam));
                                errorMessages.AppendLine($"PayName: {depositRequest.PayName}");
                                errorMessages.AppendLine($"Message: {SqlEx.Message}");
                                errorMessages.AppendLine($"Error Number: {SqlEx.Number}");
                                errorMessages.AppendLine($"ErrorCode: {SqlEx.ErrorCode}");
                                errorMessages.AppendLine($"SqlState: {SqlEx.SqlState}");
                                errorMessages.AppendLine($"Source: {SqlEx.Source}");
                                logger.LogError(errorMessages.ToString());
                            }
                            catch (Exception exForEachTransaction)
                            {
                                logger.LogError($"PayName: {depositRequest.PayName}\r\n{exForEachTransaction.Message}");
                                continue;
                            }
                            #endregion
                        }
                        else if (enquiryDepositResult.TransStatus == TransStatus.Fail)
                        {
                            try
                            {
                                paymentBLL.DepositFail(depositRequest, enquiryDepositResult);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"PayName: {depositRequest.PayName}\r\n{ex.Message}");
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
        ~DepositWorkerService()
        {
            timer?.Dispose();
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("DepositWorkerService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
