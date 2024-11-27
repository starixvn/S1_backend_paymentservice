using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.NewXyPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class NewXyPayHelperTests
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

            _signKey = "im_signkey";
        }

        [Test]
        public void A01_GetPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                {"storeCode", "81233"},
                {"storeOrderNo", "682406faa629"},
                {"payType", "bankcard"},
                {"totalAmt", "1000.00" },
                {"backUrl", "https://domain.com/index.html"},
                {"notifyUrl", "https://domain.com/notifyResult"},
                {"playerName", "张三"}
            };

            var expectSignConcatSourceString = "backUrl=https://domain.com/index.html&notifyUrl=https://domain.com/notifyResult&payType=bankcard&playerName=张三&storeCode=81233&storeOrderNo=682406faa629&totalAmt=1000.00&key=im_signkey";
            var expectedSignResult = "0CC33AC7C1CAD939BE23ED3C8864DF18";

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