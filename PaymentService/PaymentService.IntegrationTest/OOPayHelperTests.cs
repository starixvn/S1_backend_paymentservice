using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.OOPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class OoPayHelperTests
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

            _signKey = "testkey";
        }

        [Test]
        public void A01_GetPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantNo", "merchantNo" },
                { "orderNo", "123456" },
                { "userNo", "" },
                { "userName", "" },
                { "channelNo", "2" },
                { "amount", "100.00" },
                { "realName", "" },
                { "bankCode", "bankCode" },
                { "extra", "" },
                { "notifyUrl", "https://api.test/notify" },
                { "time","1654671701" },
                { "appSecret", "appSecret" }
            };

            var expectSignConcatSourceString = "amount=100.00&appSecret=appSecret&bankCode=bankCode&channelNo=2&extra=&merchantNo=merchantNo&notifyUrl=https://api.test/notify&orderNo=123456&realName=&time=1654671701&userName=&userNo=";
            var expectedSignResult = "358ACD973E533304F49C6A4750215DDC";
  
            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void A01_GetPostSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantNo", "merchantNo" },
                { "orderNo", "123456" },
                { "amount", "100" },
                { "name", "Burimas" },
                { "bankName", "BBL" },
                { "bankAccount", "2440008866" },
                { "notifyUrl", "https://api.test/notify" },
                { "extra", "" },
                { "time", "1654671701" },
                { "appSecret", "appSecret" }
            };

            var expectSignConcatSourceString = "amount=100&appSecret=appSecret&bankAccount=2440008866&bankName=BBL&extra=&merchantNo=merchantNo&name=Burimas&notifyUrl=https://api.test/notify&orderNo=123456&time=1654671701";
            var expectedSignResult = "2997802432EA8754382E57699FBAF1F6";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void B01_VerifySign_Debit()
        {
            var receiveSign = "E1662B215B65C68B68FEE27AA03A07BC";
            var receiveModel = new Dictionary<string, string>
            {
                { "status", "PAID" },
                { "tradeNo", "tradeNo" },
                { "orderNo", "123456" },
                { "userNo", "" },
                { "userName", "" },
                { "channelNo", "2" },
                { "amount", "100" },
                { "lucky", "100" },
                { "paid", "100" },
                { "extra", "" }
            };

            var expectedSignConcatSourceString =
                "amount=100&channelNo=2&extra=&lucky=100&orderNo=123456&paid=100&status=PAID&tradeNo=tradeNo&userName=&userNo=";
            var actual = _target.VerifySign(receiveSign, receiveModel, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
            actual.Should().BeTrue();
        }

        [Test]
        public void B02_VerifySign_Credit()
        {
            var receiveSign = "E38E102159E75D5446C29A9AFE173236";
            var receiveModel = new Dictionary<string, string>
            {
                { "status", "PAID" },
                { "tradeNo", "tradeNo" },
                { "orderNo", "123456" },
                { "amount", "100" },
                { "name", "Burimas" },
                { "bankName", "BBL" },
                { "bankAccount", "2440008866" },
                { "fee", "0.1" },
                { "extra", "" }
            };

            var expectedSignConcatSourceString =
                "amount=100&bankAccount=2440008866&bankName=BBL&extra=&fee=0.1&name=Burimas&orderNo=123456&status=PAID&tradeNo=tradeNo";
            var actual = _target.VerifySign(receiveSign, receiveModel, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
            actual.Should().BeTrue();
        }

        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetPostSign(testData, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}