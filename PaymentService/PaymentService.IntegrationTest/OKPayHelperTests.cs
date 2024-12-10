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
        const string testBodyData = "{\"merchantCode\":\"SH262250\",\"orderNo\":\"S2024112910170201356\",\"merchantOrderNo\":\"DD1414752\",\"amount\":\"101000\",\"orderStatus\":\"SUCCESS\",\"payTime\":\"1732874844076\",\"sign\":\"6df1793f9bc8e56b146a7ff413bc74bc\",\"orderType\":\"IN\",\"adjustStatus\":\"SUCCESS\",\"amountActual\":\"101000\",\"remark\":null}";
        
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
        const string testBodyData = "{\"merchantCode\":\"SH262250\",\"orderNo\":\"C2024112912182641509\",\"merchantOrderNo\":\"WD138366\",\"amount\":\"11000\",\"orderStatus\":\"FAIL\",\"payTime\":\"1732879665751\",\"sign\":\"675cb5b6942227fe8cf7ac7aa1f947d5\",\"orderType\":\"OUT\",\"adjustStatus\":null,\"amountActual\":null,\"remark\":null}";
                
        
        var testInputData = new Dictionary<string, string>
        {
            { "TransId", "WD138366" },
            { "Key", "5MzERclSd4QM14KzIFBBohzAfkJaUeap" },
            { "AppId", "7854769432039520" },
            { "MerchantCode", "SH262250" }
        };

        const string expected = "orderStatus=FAIL";
        
        var actual = _payHelper
            .CallBackCredit(null!, testBodyData, null!, testInputData)
            .Message;

        Assert.AreEqual(expected, actual);
    }
}