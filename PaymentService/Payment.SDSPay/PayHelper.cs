using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.SDSPay;

public class PayHelper : PayHelperBase, IExchangeHelper
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    #region ExchangeRate

    public ExchangeResult GetExchange(Dictionary<string, string> input)
    {
        var baseApiUrl = input["host"];
        var type = input["ExchangeType"]; // 0: 提款, 1: 存款

        try
        {
            var req = new Dictionary<string, string>
            {
                {"merchantNo", "MerchantNo"},
                {"type", type.Equals("0") ? "Withdrawal" : "deposit"}
            };

            var rateInfo = GetExchangeRate(baseApiUrl, req);

            return new ExchangeResult("0", "Success", new CryptoExchangeInfo
            {
                Rate = rateInfo.Rate,
                ExchangeAmount = 0,
                Timestamp = 0
            });
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"GetExchange failed, Code:{ex.Code}, Message:{ex.Message}");

            return new ExchangeResult(ex.Code, ex.Message);
        }
    }

    #endregion

    #region Debit

    public override DepositResult Debit(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Credit

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #region Private Methods

    private ExchangeRate GetExchangeRate(string baseApiUrl, Dictionary<string, string> input)
    {
        var apiUrl = baseApiUrl + Path.ExchangeRate;
        var q = string.Join("&", input.Select(x => $"{x.Key}={x.Value}"));

        var response = _httpHelper
            .GetAsync<ExchangeRate>(apiUrl, q)
            .Result;

        if (!response.StatusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"GetExchangeRate failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}");
        }

        return response.Result;
    }

    #endregion
}