using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.JDPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

public class JDPayHelperTests
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
        var testFormData = new Dictionary<string, string>
        {
            { "merNo", "10214" },
            { "ordernum", "DD1414716" },
            { "money", "99.83" },
            { "amount", "100.00" },
            { "status", "1" },
            { "msg", "：ขาย-" },
            { "sign", "88476475ac3ce6a04aac52d39880dda3" }
        };
        
        var testInputData = new Dictionary<string, string>
        {
            { "TransID", "DD1414716" },
            { "Key", "sIvJwb2YhDnIXeHkc7lYE31j2dfrob0Z" }
        };

        var expected = $"status={testFormData["status"]}, msg={testFormData["msg"]}";
        
        var actual = _payHelper
            .CallBackDebit(testFormData, string.Empty, null!, testInputData)
            .Comment;

        Assert.AreEqual(expected, actual);
    }
    
    [Test]
    public void PayoutCallbackSignTest()
    {
        var testFormData = new Dictionary<string, string>
        {
            { "merNo", "10214" },
            { "ordernum", "WD138361" },
            { "amount", "100.00" },
            { "status", "1" },
            { "sign", "23cfe5a434a2ea0c939a0be675e85fd6" }
        };
        
        var testInputData = new Dictionary<string, string>
        {
            { "TransId", "WD138361" },
            { "Key", "sIvJwb2YhDnIXeHkc7lYE31j2dfrob0Z" }
        };

        var expected = $"status={testFormData["status"]}";
        
        var actual = _payHelper
            .CallBackCredit(testFormData, string.Empty, null!, testInputData)
            .Message;

        Assert.AreEqual(expected, actual);
    }
}