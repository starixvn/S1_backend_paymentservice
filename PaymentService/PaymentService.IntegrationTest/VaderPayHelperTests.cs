using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.VaderPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest;

public class VaderPayHelperTests
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
    public void DepositCallbackBankSignTest()
    {
        var testFormData = new Dictionary<string, string>
        {
            { "contract", "855" },
            { "apikey", "uniquekey" },
            { "transaction", "036-317-24-819" },
            { "status", "3" },
            { "status_message", "Approve" },
            { "decline_reason", "" },
            { "ItemID", "DD1414418" },
            { "ItemDescription", "DD1414418" },
            { "Amount", "320.00" },
            { "total_fees", "0.00" },
            { "Currency", "MYR" },
            { "ClientName", "643973" },
            { "telephone", "N/A" },
            { "ClientEmail", "vdptestnova88my" },
            { "bank_account", "-" },
            { "src_bank_account", "-" },
            { "bank_reference", "Manual--" },
            { "signature2", "362c984b0f2a323465c7f464d994bf3a330151a3ca973fdafc9a4d494991745e" },
            { "created_at", "11/12/2024 11:24:11 AM" },
            { "updated_at", "11/12/2024 11:34:31 AM" }
        };
        
        var testInputData = new Dictionary<string, string>
        {
            { "TransID", "DD1414418" },
            { "MYR_key", "0By11KhPMwbXVJPOvZ5nNz2MVviXa1wiGI/HwUWhr2k=" },
            { "DepositInfo", @"{""Mode"": 2, ""MSort"": 0, ""PayId"": 31, ""SSort"": 0, ""SysId"": 0, ""GroupId"": 1, ""PayCode"": ""bank"", ""PayName"": ""VaderPay"", ""SelBank"": false, ""BankCode"": null, ""BankLogo"": """", ""BankName"": """", ""ChannelId"": 869, ""GroupName"": ""Online Banking"", ""MaxAmount"": 10000.0, ""MinAmount"": 320.0, ""CurrencyId"": 2, ""PayAccount"": null, ""PointLevel"": 0, ""ChannelName"": ""VaderPay"", ""SysBankCode"": """", ""SysBankName"": """", ""ExtraDataType"": 0, ""PayCurrencyId"": ""MYR"", ""BankPayoutCode"": null, ""DisplayGroupName"": null, ""DisplayChannelName"": null}" }
        };

        const string expected = "orderStatus=3";
        
        var actual = _payHelper
            .CallBackDebit(testFormData, string.Empty, null!, testInputData)
            .Comment;

        Assert.AreEqual(expected, actual);
    }
    
    [Test]
    public void DepositCallbackEWalletSignTest()
    {
        const string jsonBody =
            @"{""MerchantCode"":""vdptestnova88my"",""InvoiceNo"":""079-MY05-317-24-260"",""Currency"":""MYR"",""Amount"":""30.00"",""PaymentDate"":""11/12/2024 11:26:47 AM"",""Signature"":""41f50e966d11aaf27ba90ea9e5c2ccbccd730bb70513844532c7ae2e8ec8dc01"",""RefId"":""DD1414417"",""Status"":3,""ReceiverBank"":""MB2U"",""ReceiverAccount"":""12345678"",""OriginalAmount"":30.0,""StatusMessage"":""Manual Approved"",""DeclineReason"":""""}";
        
        var testInputData = new Dictionary<string, string>
        {
            { "TransID", "DD1414417" },
            { "MYR_key", "0By11KhPMwbXVJPOvZ5nNz2MVviXa1wiGI/HwUWhr2k=" },
            { "DepositInfo", @"{""Mode"": ""2"", ""PayId"": ""31"", ""SysId"": ""0"", ""GroupId"": ""31"", ""PayCode"": ""qr-DuitNow"", ""PayName"": ""VaderPay"", ""ChannelId"": ""870"", ""GroupName"": ""DuitNowQR"", ""MaxAmount"": ""10000.0000"", ""MinAmount"": ""30.0000"", ""CurrencyId"": ""2"", ""PayAccount"": null, ""PointLevel"": ""0"", ""ChannelName"": ""VaderPay"", ""DepositType"": ""MobilePayment"", ""ExtraDataType"": ""0"", ""PayCurrencyId"": ""MYR"", ""DisplayGroupName"": ""DuitNowQR"", ""DisplayChannelName"": ""VaderPay""}" }
        };

        const string expected = "orderStatus=3";
        
        var actual = _payHelper
            .CallBackDebit(null!, jsonBody, null!, testInputData)
            .Comment;

        Assert.AreEqual(expected, actual);
    }
    /*
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
    }*/
}