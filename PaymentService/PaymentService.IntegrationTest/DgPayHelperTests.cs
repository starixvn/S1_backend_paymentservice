using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.DgPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class DgPayHelperTests
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

            _secretKey = "SecretKey";
        }

        [Test]
        public void A01_GetSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                {"orderId", "DD123456"},
                {"providerId", "5"},
                {"providerType", "10"},
                {"currency", "MYR" },
                {"amount ", "1000"},
                {"reqDateTime", "2024-11-11 16:17:00"},
                {"opCode", "dgpay_742"}
            };

            var expectSignConcatSourceString = "DD123456510MYR10002024-11-11 16:17:00dgpay_742SecretKey";
            var expectedSignResult = "68DF8C6EF4472A5EA6C6D7D068546FF5";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetSign(testData, _secretKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}