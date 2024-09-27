using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.EBPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

public class EBPayHelperTests
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
    public void SignTest()
    {
        const string expected = "5529e28cd989577fc22d8b9617479126";
        var testData = new Dictionary<string, string>()
        {
            { "merchantNo", "10005" },
            { "merchantOrderId", "95622411315163325655" },
            { "userName", "knox" },
            { "deviceType", "1" }
        };
        
        var actual = _payHelper.GetSign(testData, "2rk9b1trxwfzohku");

        Assert.AreEqual(expected, actual);
    }
    
    [Test]
    public void DepositCallbackSignTest()
    {
        var testFormData = new Dictionary<string, string>
        {
            { "orderNo", "ICBC2220516124055745b46700" },
            { "merchantOrderId", "819273559624282180" },
            { "merchantNo", "666666" },
            { "orderStatus", "1" },
            { "payTypeId", "912" },
            { "orderAmount", "2.12" },
            { "paidAmount", "0.2" },
            { "completeTime", "yyyy-MM-dd HH:mm:ss" },
            { "sign", "e8510a9c3343c405ad0e727a2b35efb0" }
        };
        
        var testInputData = new Dictionary<string, string>
        {
            { "Key", "2rk9b1trxwfzohku" }
        };

        var expected = $"orderStatus={testFormData["orderStatus"]}, completeTime={testFormData["completeTime"]}";
        
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
            { "orderNo", "ICBC2220516124055745b46700" },
            { "merchantOrderId", "819273559624282180" },
            { "merchantNo", "666666" },
            { "orderStatus", "1" },
            { "payTypeId", "912" },
            { "orderAmount", "288.12" },
            { "paidAmount", "1.2" },
            { "completeTime", "yyyy-MM-dd HH:mm:ss" },
            { "sign", "e9e19805e7c462d93859f1ad59492e1d" }
        };
        
        var testInputData = new Dictionary<string, string>
        {
            { "Key", "2rk9b1trxwfzohku" }
        };

        var expected = $"orderStatus={testFormData["orderStatus"]}, completeTime={testFormData["completeTime"]}";
        
        var actual = _payHelper
            .CallBackCredit(testFormData, string.Empty, null!, testInputData)
            .Message;

        Assert.AreEqual(expected, actual);
    }
}