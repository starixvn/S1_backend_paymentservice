using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.QuickPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

public class QuickPayHelperTests
{
    private PayHelper _payHelper;

    [SetUp]
    public void Setup()
    {     
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        serviceProvider.GetService(typeof(ILogger<PayHelper>))
            .Returns(Substitute.For<ILogger<PayHelper>>());
        
        serviceProvider.GetService(typeof(IFlurlHttpHelper))
            .Returns(Substitute.For<IFlurlHttpHelper>());

        _payHelper = new PayHelper(serviceProvider);
    }

    [Test]
    public void DepositCallbackSignTest()
    {
        var testBody =
            @"{""status"":""MANUAL PAID"",""tradeNo"":""XOJa70813"",""orderNo"":""DP9952481"",""userNo"":1226117,""userName"":""Ananta+Firdaus"",""channelNo"":2,""amount"":199000.00,""discount"":0.00,""lucky"":0.00,""paid"":199000.00,""extra"":"""",""sign"":""35515D08765816C22EE3D1DFAC5F80AE""}";
        
        var testInputData = new Dictionary<string, string>
        {
            { "apiKey", "d36a9739a4244789889781abe68acec1" }
        };
        
        var actual = _payHelper
            .CallBackDebit(null!, testBody, null!, testInputData)
            .Response;

        Assert.AreEqual("success", actual);
    }
}