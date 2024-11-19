using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.NewXfuoo;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class NewXfuooHelperTests
    {
        private IFlurlHttpHelper _flurlHttpHelper;
        private PayHelper _target;
        private string _signKey;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _signKey = "wjwckpuuzeegquoeoi";
        }

        [Test]
        public void A01_GetPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantNo", "merch001" },
                { "outTradeNo", "trade001" },
                { "type", "UERC" },
                { "amount", "1000" },
                { "notifyUrl", "http://www.google.com" },
                { "lang", "zh" },
                { "extra", "" }
            };

            var expectSignConcatSourceString = "amount=1000&lang=zh&merchantNo=merch001&notifyUrl=http://www.google.com&outTradeNo=trade001&type=UERC&signKey=wjwckpuuzeegquoeoi";
            var expectedSignResult = "35A853ED36CE44B9069B34C363F67DEB";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetPostSign(testData, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}