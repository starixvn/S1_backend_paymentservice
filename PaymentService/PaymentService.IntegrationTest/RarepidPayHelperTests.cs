using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.RarepidPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

[TestFixture]
public class RarepidPayHelperTests
{
    private PayHelper _target;

    [SetUp]
    public void A00_SetUp()
    {   
        var serviceProvider = Substitute.For<IServiceProvider>();
        var flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
        serviceProvider.GetService<IFlurlHttpHelper>().Returns(flurlHttpHelper);
        var logger = Substitute.For<ILogger<PayHelper>>();
        serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);
        _target = new PayHelper(serviceProvider);
    }
    
    [Test]
    public void A01_VerifySign_CallBackDebit()
    {
        var depositReceive = new PaymentReceive
        {
            StatusCode = 0,
            Message = "NO_ERROR",
            Token = "e73abb15-3c89-11ef-8d67-020000e32cda",
            Amount = "3200.00",
            TxnId = "2627132",
            Status = "completed",
            Signature = "9ac0ded4c4a3953778953265512783ee40c7b5de5d0e9680d401a00be70a4142",
            Updated = "2023-12-31 23:59:59"
        };

        AssertVerifySignShouldBeTrue(depositReceive);
    }

    private void AssertVerifySignShouldBeTrue(PaymentReceive paymentReceive)
    {
        var actual = _target.VerifySign(paymentReceive, "36842c52711c46f5680ee85129f534a70b54df5beafee944d4dd7f7e8de20313");

        actual.Should().BeTrue();
        PayHelper.SignSourceString.Should().Be("26271323200.00completed36842c52711c46f5680ee85129f534a70b54df5beafee944d4dd7f7e8de20313");
    }
}