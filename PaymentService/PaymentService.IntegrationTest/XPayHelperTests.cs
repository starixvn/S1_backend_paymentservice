using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.XPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class XPayHelperTests
    {
        private IFlurlHttpHelper _flurlHttpHelper;
        private PayHelper _target;
        private string _secretKey;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _secretKey = "abc12345";
        }
        
        [Test]
        public void A01_GetPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "service_version", "1.0" },
                { "partner_code", "AB00001" },
                { "partner_orderid", "2019062100008883" },
                { "member_id", "member123" },
                { "currency", "USD" },
                { "amount", "5000" },
                { "account_name", "黃曉明" },
                { "bank_code", "ICBC" },
                { "notify_url", "https://www.test.com/callback" },
            };
            
            var expectSignConcatSourceString =
                "service_version=1.0&partner_code=AB00001&partner_orderid=2019062100008883&member_id=member123&currency=USD&amount=5000&account_name=黃曉明&bank_code=ICBC&notify_url=https://www.test.com/callback&key=abc12345";
            var expectedSignResult = "69E3F3F21B3C584923337930B1CE3E39C707DF51";

            AssertGetSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }
        
        [Test]
        public void B01_VerifySign_CallBackCredit()
        {
            var receiveModel = new Dictionary<string, string>
            {
                { "service_version", "1.0" },
                { "billno", "FO2019042515000002" },
                { "partner_orderid", "2019062100008883" },
                { "currency", "CNY" },
                { "amount", "5000" },
                { "account_name", "黄晓明" },
                { "account_number", "1234567890" },
                { "bank_code", "ICBC" },
                { "status", "000" },
                { "error_code", "" },
                { "error_description", "" }
            };
            
            var expectSignConcatSourceString = "service_version=1.0&billno=FO2019042515000002&partner_orderid=2019062100008883&currency=CNY&amount=5000&account_name=黄晓明&account_number=1234567890&bank_code=ICBC&status=000&key=abc12345";
            AssertVerifySign(receiveModel, expectSignConcatSourceString, "739B7D5EE6973AD835BD973C304C3EB301418A25");
        }
        
        private void AssertGetSign(Dictionary<string, string> testData, string expectSignConcatSourceString,
            string expectedSignResult)
        {
            var actual = _target.GetSign(testData, _secretKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
        
        private void AssertVerifySign(Dictionary<string, string> receiveModel, string expectSignConcatSourceString, string receivedSign)
        {
            var actual = _target.VerifySign(receiveModel, _secretKey, receivedSign);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(true);
        }
    }
}