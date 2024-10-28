using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.GameWallet;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

[TestFixture]
public class GameWalletPayHelperTest
{
    private const string Key = "secretkey";
    private PayHelper _target;
    [SetUp]
    public void _00_Setup()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();

        serviceProvider.GetService(typeof(ILogger<PayHelper>))
            .Returns(Substitute.For<ILogger<PayHelper>>());

        serviceProvider.GetService(typeof(IFlurlHttpHelper))
            .Returns(Substitute.For<IFlurlHttpHelper>());

        _target = new PayHelper(serviceProvider);
    }

    [Test]
    public void A01_GetPostSign_ForDebit()
    {
        var data = GenerateDepositPost();

        var expected = "54424af6011fb0c4b2792b6d0fdac7c0";
        var expectedSignConcatSourceString =
            "Mer123Deposit123user12312345VND20241026180500secretkey127.0.0.1";
        AssertGetPostSignShouldReturn(data, expected, expectedSignConcatSourceString);
    }

    [Test]
    public void B01_ValidateSign_ForDebit()
    {
        var data = GenerateDepositReceive();

        var expectedSignConcatSourceString = "ABC123Deposit123171335100.0020006secretkey";
        AssertValidateSignShouldReturn(data, true, expectedSignConcatSourceString);
    }

    [Test]
    public void C01_GetCreditPostSign_ForCredit()
    {
        var data = GenerateCreditPost();

        var expected = "bb452068f20d3247edd4cb32578064e8";
        var expectedSignConcatSourceString =
            "V000016068661713072000.00THB2012-05-21 20:04:00PMsecretkey";
        AssertGetCreditPostSignShouldReturn(data, expected, expectedSignConcatSourceString);
    }

    [Test]
    public void D01_ValidateSign_ForCredit()
    {
        var data = GenerateWithdrawalReceive();

        var expectedSignConcatSourceString = "1bvc123621521123456599THB000secretkey";
        AssertVerifySignShouldReturn(data, true, expectedSignConcatSourceString);
    }

    private void AssertGetCreditPostSignShouldReturn(
        WithdrawalPost data,
        string expected,
        string expectedSignConcatSourceString)
    {
        var actual = _target.GetCreditPostSign(data, Key);
        _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
        actual.Should().Be(expected);
    }

    private void AssertGetPostSignShouldReturn(
        DepositPost data,
        string expected,
        string expectedSignConcatSourceString)
    {
        var actual = _target.GetPostSign(data, Key);
        _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
        actual.Should().Be(expected);
    }

    private void AssertValidateSignShouldReturn(
        DepositCallbackRequest data,
        bool expected,
        string expectedSignConcatSourceString)
    {
        var actual = _target.ValidateSign(data, Key);
        _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
        actual.Should().Be(expected);
    }

    private void AssertVerifySignShouldReturn(
        WithdrawalCallBackRequest data,
        bool expected,
        string expectedSignConcatSourceString)
    {
        var actual = _target.VerifySign(data, Key);
        _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
        actual.Should().Be(expected);
    }

    private WithdrawalPost GenerateCreditPost()
    {
        return new WithdrawalPost
        {
            accountName = "a123",
            accountNumber = "",
            amount = "2000.00",
            bank = "SCB",
            callbackUri = "http://localhost:58969/GameWalletWithdrawal_Receive.aspx",
            currency = "THB",
            customer = "171307",
            datetime = "2012-05-21 20:04:00PM",
            merchant = "V00001",
            note = "",
            reference = "606866"
        };
    }

    private DepositPost GenerateDepositPost()
    {
        return new DepositPost
        {
            Amount = "12345",
            Bank = "bankABC",
            CallbackUri = "http://123.com",
            Currency = "VND",
            Customer = "user123",
            Datetime = "20241026180500",
            FailUrl = "https://{Domain}/Home/2.0/Deposit/?status=fail",
            Language = "lang-th",
            Merchant = "Mer123",
            Note = "nothing",
            Reference = "Deposit123",
            SuccessUrl = "https://{Domain}/Home/2.0/Deposit/?status=success",
            ClientIP = "127.0.0.1"
        };
    }

    private DepositCallbackRequest GenerateDepositReceive()
    {
        return new DepositCallbackRequest
        {
            actualAmount = "100.00",
            amount = "100.00", // 4
            bank = "MOMO",
            currency = "20", // 5
            customer = "171335", // 3
            datetime = "20241025233600",
            key = "2f4e91430e5a2f633c96e506e20688a8",
            merchant = "ABC123", // 1
            reference = "Deposit123", // 2
            refId = "",
            statementDate = "20241025233600",
            statusCode = "006", // 6
            statusMessage = "ok",
            transactionId = "20241025233600"
        };
    }

    private WithdrawalCallBackRequest GenerateWithdrawalReceive()
    {
        return new WithdrawalCallBackRequest
        {
            Merchant = "1bvc123",
            Reference = "621521",
            Customer = "123456",
            Amount = "599",
            Currency = "THB",
            StatusCode = "000", // 000:Success 001:Failed 002:Processing
            TransactionId = "20241027210124",
            key = "edfee1e36d34f26fadb8187d780f3823"
        };
    }
}