using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment._988Pay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class _988PayHelperTest
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

            _signKey = "key";
        }
        [Test]
        public void A01_GetPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "appId", "1714569990408773632" },
                { "nonce", "asdfghgf33" },
                { "mchOrderNo", "PO1321321" },
                { "amount", "40.00" },
                { "member", "张三/3DAE14184D8B" },
                { "notifyUrl", "http://localhost:8184/notify" },
                { "timestamp", "1697253582897" },
                { "extParam", "" },
                { "sign", "2218736B7B0CB1ED5C30F2C8DD28ACD0" }
            };

            var expectSignConcatSourceString = "amount=40.00&appId=1714569990408773632&mchOrderNo=PO1321321&member=张三/3DAE14184D8B&nonce=asdfghgf33&notifyUrl=http://localhost:8184/notify&timestamp=1697253582897&key=key";
            var expectedSignResult = "2218736B7B0CB1ED5C30F2C8DD28ACD0";

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