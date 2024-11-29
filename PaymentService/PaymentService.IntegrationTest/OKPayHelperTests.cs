using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.OKPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

public class OKPayHelperTests
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
        var testBodyData = "{\"merchantCode\":\"SH262250\",\"orderNo\":\"S2024112910170201356\",\"merchantOrderNo\":\"DD1414752\",\"amount\":\"101000\",\"orderStatus\":\"SUCCESS\",\"payTime\":\"1732874844076\",\"sign\":\"6df1793f9bc8e56b146a7ff413bc74bc\",\"orderType\":\"IN\",\"adjustStatus\":\"SUCCESS\",\"amountActual\":\"101000\",\"remark\":null}";
        
        var testInputData = new Dictionary<string, string>
        {
            { "TransID", "DD1414752" },
            { "Key", "5MzERclSd4QM14KzIFBBohzAfkJaUeap" },
            { "AppId", "7854769432039520" },
            { "MerchantCode", "SH262250" }
        };

        const string expected = "orderStatus=SUCCESS";
        
        var actual = _payHelper
            .CallBackDebit(null!, testBodyData, null!, testInputData)
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