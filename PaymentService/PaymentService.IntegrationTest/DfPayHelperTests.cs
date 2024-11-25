using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.DfPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class DfPayHelperTests
    {
        private IFlurlHttpHelper _flurlHttpHelper;
        private PayHelper _target;
        private string _token;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _token = "SecretKey";
        }

        [Test]
        public void A01_GetSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                {"dfusername", "merchantName"},
                {"money", "500"},
                {"orderno", "WD13579"},
                {"skcardno", "123123123" },
                {"skcardname ", "name"},
                {"skbankid", "300"},
                {"hdurli", "http://callback.url"}
            };

            var expectSignConcatSourceString = "merchantName500WD13579123123123name300http://callback.url{SecretKey}";
            var expectedSignResult = "1d8fbd4645006429ef7f84c0a71fb972";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetSign(testData, _token);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}