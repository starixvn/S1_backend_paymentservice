using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Payment.CommsPay;
using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;
using PaymentService.Service.Model;
namespace PaymentService.IntegrationTest;

[TestFixture]
public class CommsPayHelperTests : PayHelperTestBase
{
    private const string CommsPaySetting = "{\"Host\": \"http://www.commsPay.com\", \"MerchantCode\": \"213213\", \"ApiKey\": \"3213212ewfewqfwqf\", \"PayName\": \"CommsPay\", \"SecretKey\": \"jdo383f1d2021ehd1dj2di32\"}";
    private const string DepositInfo = "{\"Mode\": \"2\", \"PayId\": \"93\", \"SysId\": \"0\", \"GroupId\": \"28\", \"PayCode\": \"CPAY08D\", \"PayName\": \"CommsPay\", \"ChannelId\": \"776\", \"GroupName\": \"UPI\", \"MaxAmount\": \"50000.0000\", \"MinAmount\": \"500.0000\", \"CurrencyId\": \"61\", \"PayAccount\": null, \"PointLevel\": \"0\", \"ChannelName\": \"CommsPay\", \"DepositType\": \"MobilePayment\", \"ExtraDataType\": \"0\", \"PayCurrencyId\": \"INR\", \"DisplayGroupName\": \"UPI\", \"DisplayChannelName\": \"CommsPay\"}";
    private const string WithdrawalInfo = "{\"PayId\": 93, \"SysId\": 244, \"IFSCCode\": \"ABCDE012345\", \"AccountNo\": \"9416030505\", \"ChannelId\": 777, \"AccountName\": \"Jatinder Mesi\"}";
    private string _actualUrl;
    private WithdrawalPostRaw _actualWData;
    private IFlurlHttpHelper _flurlHttpHelper;
    private PayHelper _target;
    [SetUp]
    public void A00_Setup()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<PayHelper>>();
        serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

        _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
        serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
        _target = new PayHelper(serviceProvider);
    }

    [Test]
    public void A01_Debit()
    {
        var transId = "TEST-DP-163158903432";
        var inputs = GenerateDepositInputs(transId);
        GivenCurrentTimeStamp("1631589012");

        var expectedKey =
            "BmMLzvFw2%2BX4sBwj5uuZMkYc2ELoHQUebvPzlWSXGaRTpUsV5cCidR30az5I%2FFpFmp1GsDgFDzAoXwb47rwKF%2BMkY6g642fSwXfl5%2Bux%2Fvjc12lCiYdWkF%2Bg9XtOcfXWaV9MX2j%2FFcWlyIYW2dQyPCwN8I89Yh1ifyyNCKxIcKn8JcKU53sxC1FSOStIbIUNfjNPsJA%2F1Q2OkwHf3prXIMcmYPFrilAa4YD2iLFDru0MH%2FlXekcdf%2FAe5UXdzhsXfTJOO54Z%2BpsGRFdEAeRoPAWVTiXPFShdrvPtRR0lflk0KeC4Sk4PVoIaDXe%2FdDpMlENTG%2BUVOXF5OPZpMkI%2FSKTraX27%2BIhZD82rKD8mzQ2agK2%2BEpMZGeHIyeerAwJDsKzXE8nU3Mj6ocKUh2h%2BGFG2A8tTbRpxkIdSeg7MNbZWt1B9oGp6%2FNGeYzYtLY9fYy2%2BLWjywxTGxApk7mmQXQ%3D%3D";
        var expected = new DepositResult
        {
            Success = true,
            ErrorCode = "0",
            Url = $"http://www.commsPay.com/213213/v2/dopayment?key={expectedKey}"
        };
        var expectedSignConcatSourceString =
            "merchant_code=213213&merchant_api_key=3213212ewfewqfwqf&transaction_code=TEST-DP-163158903432&transaction_timestamp=1631589012&transaction_amount=1000&payment_code=CPAY08D&user_id=651403&currency_code=INR&bank_code=&callback_url=http://www.callbackurl.com/Payment/DepositCallBack/TEST-DP-163158903432&return_url=ReturnUrl&random_bank_code=";
        AssertDebitShouldReturn(inputs, expected, expectedSignConcatSourceString);
    }

    [Test]
    public void B01_CallBackDebit_Success()
    {
        var bodyContentSource = "{\"key\": \"lkbOz0m36JPaxJB8LdxhSWAP10D63IINZ8%2Bq7IUfMvj6VdqKixYF6J5b3S4Qa6mfTw67xOdVJlVpihHKFZFO8CSDAPUm60dz3kCd5QFdpgEPmap7NtwleKp4eK%2FnEO7c9vUj8HILxHdWw216Wg3CDnj8rSZ0fEGyHnnMSv97NBasooKw7gMpu2OwJXYEeH9Qq0UQ07K%2BcawEpTDexbBX5jqc90oMJ9TCLjPAMqi48Qk%3D\",\"transaction_code\": \"DD1413179\",\"transaction_no\": \"202409271248\"}";
        var (formData, headers, bodyContent, payResource) = 
            GenerateCallBackDebitParameters(bodyContentSource);

        var expected = new NotifyDepositResult
        {
            ThirdId = "202409271248",
            DepositId = "DD1413179",
            Comment = "2",
            TransStatus = TransStatus.Success,
            ActualAmount = 1629m,
            Response = "SUCCESS"
        };
        AssertCallBackDebitShouldReturn(formData, bodyContent, headers, payResource, expected);
    }

    [Test]
    public void B02_CallBackDebit_FailDecrypt()
    {
        var bodyContentSource = "{\"key\": \"xxx\",\"transaction_code\": \"DD1413179\",\"transaction_no\": \"202409271248\"}";
        var (formData, headers, bodyContent, payResource) =
            GenerateCallBackDebitParameters(bodyContentSource);

        var expected = new NotifyDepositResult
        {
            DepositId = "DD1413179",
            TransStatus = TransStatus.Pending,
            Response = "Decrypt failed"
        };
        AssertCallBackDebitShouldReturn(formData, bodyContent, headers, payResource, expected);
    }

    [Test]
    public void C01_Credit_Success()
    {
        var transId = "WD138065";
        var inputs = GenerateWithdrawalInputs(transId);
        var withdrawalResponse = GenerateWithdrawalResponse(true, "request withdrawal successful");
        GivenHttpHelperPostJsonReturnsWithdrawalResponse(withdrawalResponse);
        GivenCurrentTimeStamp("1631589012");

        var expected = new WithdrawResult
        {
            Success = true,
            Message = "request withdrawal successful"
        };
        AssertCreditSuccessShouldReturn(inputs, expected);

        var expectedData = new WithdrawalPost
        {
            MerchantCode = "213213",
            TransactionCode = transId,
            TransactionTimestamp = "1631589012",
            PayoutCode = "CPAY05W",
            TransactionAmount = 1000d,
            UserId = "651403",
            CurrencyCode = "INR",
            BankAccountNumber = "58585858",
            IfscCode = "ABCDE012345",
            BankCode = "UTIB",
            BankName = "Axis Bank",
            AccountName = "Jatinder Mesi",
            CallbackUrl = $"http://www.callbackurl.com/Payment/WithdrawalCallBack/{transId}"
        };
        AssertHttpHelperPostJsonForWithdrawalShouldReceive(
            "u%2BPuGc5mMkuvq3%2FuqDXE6LC9HQSphZT%2FjDpFo34hJ5pHFHR5C4E6HHzdJPIDbKLcln7AFjP9WvyKF1bnoO19mJKc8bTMojSRle%2BUAVPchVBIybrQEZ6yTvGPoC8blup7q%2Bk%2BLyKdQCe3K9DmbYjP26vMiGXK%2B%2Biet7IO1StbhVVip5kk0MsAsXHq%2FDKplFrmbH%2Fg9nwUKjFp1PNkWb1cfd3uklxtS1slYIuWT2dJrE9zulrtwYgcbM7GjJuIqVyqFix6p918dGO4x0FI9zN2uk1nQ5F8pK8qp9luLsfxEiUsyyiTROEvcyqsrCcbnuYjI5rBpY78BlbqpowL9tySIvfGz9hri%2B3qBjneelopAM5aGlBr7PIXolBkeZDEIT8T7TvrqI%2FIUVO602ZPunayqsa%2BeUUR74QZXBFWzd7V18g3EaW54mwmhcujVSwS%2BwBBade2C8Viefj8MzArc4Lt%2BA%2B9j4iQZlb%2FQc5vEp3W9st1ZMrj4%2Bp3hCoR8RxNjZC4k0KWP%2F%2B8rBaDEfXN79JsBQ%3D%3D",
            "http://www.commsPay.com/api/v1/payout/213213",
            JsonConvert.SerializeObject(expectedData));
    }

    [Test]
    public void C02_Credit_Fail()
    {
        var transId = "WD138065";
        var inputs = GenerateWithdrawalInputs(transId);
        var withdrawalResponse = GenerateWithdrawalResponse(false, "Invalid Transaction Code ( not unique )!, error code 111");
        GivenHttpHelperPostJsonReturnsWithdrawalResponse(withdrawalResponse);
        GivenCurrentTimeStamp("1631589012");

        var expected = new WithdrawResult
        {
            Success = false,
            Message = "CommsPay create payout failed: Success=False, Message=Invalid Transaction Code ( not unique )!, error code 111"
        };
        AssertCreditSuccessShouldReturn(inputs, expected);

        var expectedData = new WithdrawalPost
        {
            MerchantCode = "213213",
            TransactionCode = transId,
            TransactionTimestamp = "1631589012",
            PayoutCode = "CPAY05W",
            TransactionAmount = 1000d,
            UserId = "651403",
            CurrencyCode = "INR",
            BankAccountNumber = "58585858",
            IfscCode = "ABCDE012345",
            BankCode = "UTIB",
            BankName = "Axis Bank",
            AccountName = "Jatinder Mesi",
            CallbackUrl = $"http://www.callbackurl.com/Payment/WithdrawalCallBack/{transId}"
        };
        AssertHttpHelperPostJsonForWithdrawalShouldReceive(
            "u%2BPuGc5mMkuvq3%2FuqDXE6LC9HQSphZT%2FjDpFo34hJ5pHFHR5C4E6HHzdJPIDbKLcln7AFjP9WvyKF1bnoO19mJKc8bTMojSRle%2BUAVPchVBIybrQEZ6yTvGPoC8blup7q%2Bk%2BLyKdQCe3K9DmbYjP26vMiGXK%2B%2Biet7IO1StbhVVip5kk0MsAsXHq%2FDKplFrmbH%2Fg9nwUKjFp1PNkWb1cfd3uklxtS1slYIuWT2dJrE9zulrtwYgcbM7GjJuIqVyqFix6p918dGO4x0FI9zN2uk1nQ5F8pK8qp9luLsfxEiUsyyiTROEvcyqsrCcbnuYjI5rBpY78BlbqpowL9tySIvfGz9hri%2B3qBjneelopAM5aGlBr7PIXolBkeZDEIT8T7TvrqI%2FIUVO602ZPunayqsa%2BeUUR74QZXBFWzd7V18g3EaW54mwmhcujVSwS%2BwBBade2C8Viefj8MzArc4Lt%2BA%2B9j4iQZlb%2FQc5vEp3W9st1ZMrj4%2Bp3hCoR8RxNjZC4k0KWP%2F%2B8rBaDEfXN79JsBQ%3D%3D",
            "http://www.commsPay.com/api/v1/payout/213213",
            JsonConvert.SerializeObject(expectedData));
    }

    [Test]
    public void D01_CallBackCredit_Success()
    {
        var bodyContentSource =
            "{\"transaction_no\":\"20241008125328\",\"transaction_code\":\"WD138065\",\"key\":\"KCwkB9kJ89V4%2BJL4hdYSV7vYe6db77xOCnYl0lLhfJ11aVw6oL4EKKXSV%2FtA11AoFPWtboodZVfx6W1vabBmxkfVjbYXKwGbTC%2Bo6c%2FoDAvKZSll5xOQ3xrPchfvKSzRrWQTVi2CmqJ866JdYp4znK%2BAMjvoOIefTciTMpnDxQCa1Vj0QwmckpfKm6SFbBjBqLbXi0FzwfYTTxryfx1FdTWv2Kv9m7VRcWfayEZk09IpoFxJOJNxGgFGYbZXbemaxF7vAV2X6WNZHZhUmc7q6Q%3D%3D\"}";
        var (formData, headers, bodyContent, payResource) = GenerateCallBackCreditParameters(bodyContentSource);

        var expected = new NotifyWithdrawResult
        {
            WithdrawId = "WD138065",
            ThirdId = "20241008125328",
            TransStatus = TransStatus.Success,
            Response = "SUCCESS",
            Message = "2"
        };
        AssertCallBackCreditShouldReturn(formData, bodyContent, headers, payResource, expected);
    }

    [Test]
    public void D02_CallBackCredit_FailDecrypt()
    {
        var bodyContentSource =
            "{\"transaction_no\":\"20241008125328\",\"transaction_code\":\"WD138065\",\"key\":\"xxx\"}";
        var (formData, headers, bodyContent, payResource) = GenerateCallBackCreditParameters(bodyContentSource);

        var expected = new NotifyWithdrawResult
        {
            WithdrawId = "WD138065",
            TransStatus = TransStatus.Pending,
            Response = "Decrypt failed"
        };
        AssertCallBackCreditShouldReturn(formData, bodyContent, headers, payResource, expected);
    }

    private void AssertCallBackCreditShouldReturn(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource, NotifyWithdrawResult expected)
    {
        var actual = _target.CallBackCredit(formData, bodyContent, headers, payResource);
        actual.Should().BeEquivalentTo(expected);
    }

    private void AssertCallBackDebitShouldReturn(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource, NotifyDepositResult expected)
    {
        var actual = _target.CallBackDebit(formData, bodyContent, headers, payResource);
        actual.Should().BeEquivalentTo(expected);
    }

    private void AssertCreditSuccessShouldReturn(Dictionary<string, string> inputs, WithdrawResult expected)
    {
        var actual = _target.Credit(inputs);
        actual.Should().BeEquivalentTo(expected);
    }

    private void AssertDebitShouldReturn(
        Dictionary<string, string> inputs,
        DepositResult expected,
        string expectedSignConcatSourceString)
    {
        var actual = _target.Debit(inputs);
        _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
        actual.Should().BeEquivalentTo(expected);
    }

    private void AssertHttpHelperPostJsonForWithdrawalShouldReceive(
        string expectedKey,
        string expectedUrl,
        string expectedSignConcatSourceString)
    {
        var expected = new WithdrawalPostRaw { Key = expectedKey };
        _actualWData.Should().BeEquivalentTo(expected);
        _actualUrl.Should().Be(expectedUrl);
        _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
    }

    private BankInfoPay GenerateBankInfo()
    {
        var bankInfoPay = new BankInfoPay
        {
            PayId = 93,
            PayCurrencyId = "INR",
            BankCode = "UTIB",
            BankName = "Axis Bank",
            SysId = 244,
            PayCode = "CPAY05W"
        };
        return bankInfoPay;
    }

    private (Dictionary<string, string> formData,
                            Dictionary<string, string> headers,
        string bodyContent,
        Dictionary<string, string> payResource)
        GenerateCallBackCreditParameters(string bodyContent)
    {
        var formData = GenerateFormData();
        var headers = GenerateHeaders();
        var withdrawalRequest = new WithdrawalRequest
        {
            TransId = "WD138065",
            WithdrawalInfo = WithdrawalInfo,
            WithdrawalType = WithdrawalType.OnlineBanking,
            CurrencyId = 61
        };
        var payResource = GenerateCreditCallBackParametersFromPayResource(CommsPaySetting, withdrawalRequest);

        return (formData, headers, bodyContent, payResource);
    }
    private (Dictionary<string, string> formData,
        Dictionary<string, string> headers,
        string bodyContent,
        Dictionary<string, string> payResource)
        GenerateCallBackDebitParameters(string bodyContent)
    {
        var formData = GenerateFormData();
        var headers = GenerateHeaders();
        var depositRequest = new DepositRequest
        {
            TransId = "DD1413179",
            DepositInfo = DepositInfo,
            DepositType = DepositType.MobilePayment,
            Amount = 1629,
            CurrencyId = 61
        };
        var payResource = GenerateCallBackParametersFromPayResource(CommsPaySetting, depositRequest);

        return (formData, headers, bodyContent, payResource);
    }

    private Dictionary<string, string> GenerateDepositInputs(string transId)
    {
        var inputs = new Dictionary<string, string>();

        inputs.AddRange(GenerateInputsFromOriginalRequest(transId));

        inputs.AddRange(GenerateInputsFromDepositInfo(DepositInfo));
        inputs.AddRange(GenerateDepositInputsForMobilePay(DepositInfo, new BankInfoPay()));

        inputs.AddRange(GenerateInputsFromPayInfo(93, "INR", "CPAY08D"));
        inputs.AddRange(GenerateDepositInputsFromCustInfo());
        inputs.AddRange(GenerateInputsFromPaySetting(CommsPaySetting));

        return inputs;
    }

    private Dictionary<string, string> GenerateFormData()
    {
        return new Dictionary<string, string>();
    }

    private Dictionary<string, string> GenerateHeaders()
    {
        return new Dictionary<string, string>();
    }

    private Dictionary<string, string> GenerateWithdrawalInputs(string transId)
    {
        var inputs = new Dictionary<string, string>();
        inputs.AddRange(GenerateWithdrawalInputsFromOriginalRequest(transId));
        inputs.AddRange(GenerateInputsFromWithdrawalInfoForOnlineBanking(WithdrawalType.OnlineBanking, WithdrawalInfo));
        inputs.AddRange(GenerateWithdrawalInputsFromCustInfo());
        inputs.AddRange(GenerateWithdrawalInputsFromPayInfo(GenerateBankInfo()));
        inputs.AddRange(GenerateInputsFromWithdrawalTypeForOnlineBanking(WithdrawalInfo, GenerateBankInfo()));
        inputs.AddRange(GenerateInputsFromPaySetting(CommsPaySetting));
        return inputs;
    }

    private WithdrawalResponse GenerateWithdrawalResponse(bool success, string message)
    {
        return new WithdrawalResponse
        {
            Success = success,
            Message = message
        };
    }

    private void GivenCurrentTimeStamp(string timeStamp)
    {
        _target.CurrentTimeStamp = timeStamp;
    }

    private void GivenHttpHelperPostJsonReturnsWithdrawalResponse(WithdrawalResponse withdrawalResponse)
    {
        _flurlHttpHelper
            .PostJson<WithdrawalPostRaw, WithdrawalResponse>(
                Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                Arg.Do<WithdrawalPostRaw>(actualData => _actualWData = actualData))
            .Returns((withdrawalResponse, HttpStatusCode.OK));
    }
}