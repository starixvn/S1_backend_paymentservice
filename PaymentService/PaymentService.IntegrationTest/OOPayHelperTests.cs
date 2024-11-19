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
        public void A02_GetPostSign_Credit()
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
        public void A03_GetInrPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantNo", "123456" },
                { "orderNo", "123456" },
                { "userNo", "1001" },
                { "userName", "1001" },
                { "channelNo", "4" },
                { "amount", "1000.00" },
                { "discount", "0.00" },
                { "payeeName", "1001" },
                { "datetime", "2021-10-10 23:59:59" },
                { "extra", "" },
                { "notifyUrl", "xxx" },
                { "returnUrl", "xxx" },
                { "time","1681681688" },
                { "appSecret", "appSecret" }
            };

            var expectSignConcatSourceString = "amount=1000.00&datetime=2021-10-10 23:59:59&discount=0.00&extra=&merchantNo=123456&notifyUrl=xxx&orderNo=123456&time=1681681688&userNo=1001";
            var expectedSignResult = "2F0EBB8B4DCE7F3E0A3430D3434C9C9A";

            AssertGetInrDepositPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void A04_GetInrPostSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantNo", "123456" },
                { "orderNo", "20231010123456789012" },
                { "amount", "1000" },
                { "name", "123" },
                { "bankName", "abcde" },
                { "bankAccount", "123456" },
                { "bankBranch", "IFSC00000" },
                { "mobile", "12345" },
                { "datetime", "2023-01-01 23:59:59" },
                { "notifyUrl", "" },
                { "reverseUrl", "" },
                { "extra", "" },
                { "time", "1570716647" },
                { "appSecret", "appSecret" }
            };

            var expectSignConcatSourceString = "amount=1000&bankAccount=123456&bankName=abcde&datetime=2023-01-01 23:59:59&extra=&merchantNo=123456&mobile=12345&name=123&notifyUrl=&orderNo=20231010123456789012&reverseUrl=&time=1570716647";
            var expectedSignResult = "A5E447B694FD52838F7DA81299AD3836";

            AssertGetInrWithdrawalPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
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

        private void AssertGetInrDepositPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetInrDepositPostSign(testData, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
        private void AssertGetInrWithdrawalPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetInrWithdrawalPostSign(testData, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }

        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetPostSign(testData, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}