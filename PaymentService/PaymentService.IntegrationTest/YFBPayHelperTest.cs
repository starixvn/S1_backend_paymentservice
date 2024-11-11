using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.YFBPay;
using PaymentCore.Interface;

// ReSharper disable InconsistentNaming
namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class YFBPayHelperTest
    {
        private PayHelper _target;
        private IFlurlHttpHelper _flurlHttpHelper;
        private string _key;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _key = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        [Test]
        public void A01_GetSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "amount", "100.50" },
                { "datetime", "2024-07-18 14:30:00" },
                { "discount", "10.00" },
                { "extra", "" },
                { "merchantNo", "123456" },
                { "notifyUrl", "https://yourdomain.com/notify" },
                { "orderNo", "123456" },
                { "time", "1681681688" },
                { "userNo", "12345" }
            };

            var expectSignConcatSourceString = "amount=100.50&datetime=2024-07-18 14:30:00&discount=10.00&extra=&merchantNo=123456&notifyUrl=https://yourdomain.com/notify&orderNo=123456&time=1681681688&userNo=12345ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var expectedSignResult = "1363BBB168F9D17800FEEA5AE608B89F";

            AssertGetSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void A02_GetSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "amount", "1000" },
                { "bankAccount", "123456" },
                { "bankName", "abcde" },
                { "datetime", "2023-01-01 23:59:59" },
                { "extra", string.Empty },
                { "merchantNo", "123456" },
                { "mobile", "12345" },
                { "name", "123" },
                { "notifyUrl", "https://yourdomain.com/notify" },
                { "orderNo", "20231010123456789012" },
                { "reverseUrl", string.Empty },
                { "time", "1681681688" }
            };

            var expectSignConcatSourceString = "amount=1000&bankAccount=123456&bankName=abcde&datetime=2023-01-01 23:59:59&extra=&merchantNo=123456&mobile=12345&name=123&notifyUrl=https://yourdomain.com/notify&orderNo=20231010123456789012&reverseUrl=&time=1681681688ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var expectedSignResult = "0230C67DD61FE04696F4A5F65186B345";

            AssertGetSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void B01_ValidateSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "amount", "2500.00" },
                { "discount", "100.00" },
                { "extra", "" },
                { "lucky", "50.00" },
                { "orderNo", "12345" },
                { "paid", "2500.00" },
                { "status", "PAID" },
                { "tradeNo", "YFB20240719-001" },
                { "userNo", "123456" }
            };

            var sign = "E3FA36BEC7C9DB5CE47000AE9C40BE3B";
            var expectedSignConcatSourceString = "amount=2500.00&discount=100.00&extra=&lucky=50.00&orderNo=12345&paid=2500.00&status=PAID&tradeNo=YFB20240719-001&userNo=123456ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            AssertValidateSignShouldReturn(parameters, sign, true, expectedSignConcatSourceString);
        }

        [Test]
        public void B02_ValidateSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "amount", "1000" },
                { "bankAccount", "123456" },
                { "bankName", "abcde" },
                { "extra", "" },
                { "fee", "3.12" },
                { "mobile", "12345" },
                { "name", "name" },
                { "orderNo", "12345" },
                { "status", "PAID" },
                { "tradeNo", "YFB20240719-001" }
            };

            var sign = "8DDDC9DD764D9D4C4B364C66B4E3C3CD";
            var expectedSignConcatSourceString = "amount=1000&bankAccount=123456&bankName=abcde&extra=&fee=3.12&mobile=12345&name=name&orderNo=12345&status=PAID&tradeNo=YFB20240719-001ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            AssertValidateSignShouldReturn(parameters, sign, true, expectedSignConcatSourceString);
        }

        private void AssertValidateSignShouldReturn(Dictionary<string, string> parameters, string sign, bool expectedResult, string expectedSignConcatSourceString)
        {
            var actual = _target.ValidateSign(parameters, sign, _key);
            _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
            actual.Should().Be(expectedResult);

        }

        private void AssertGetSign(Dictionary<string, string> parameters, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetSign(parameters, _key);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}