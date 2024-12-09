using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.AeePay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class AeePayHelperTest
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

            _secretKey = "md5Key";
        }

        [Test]
        public void A01_GetPostSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantno", "merchantno" },
                { "morderno", "62000" },
                { "paycode", "BDTTP" },
                { "notifyurl", "depositReceiveUrl" },
                { "money", "100" },
                { "sendtime", "20170502144100" },
                { "buyerip", "127.0.0.1" }
            };

            // merchantno+"|"+morderno+"|"+ paycode +"|"+notifyurl+"|"+money+"|"+sendtime+"|"+buyerip+"|"+md5key
            var expectSignConcatSourceString =
                "merchantno|62000|BDTTP|depositReceiveUrl|100|20170502144100|127.0.0.1|md5Key";
            var expectedSignResult = "4e51d35789b4b06b51273cd36e0873f0";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }
        
        [Test]
        public void A02_GetPostSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "merchantno", "test" },
                { "morderno", "621435" },
                { "bankcode", "BDT_BKASH" },
                { "type", "0" },
                { "realname", "first last" },
                { "cardno", "ChinaAtmNumber" },
                { "money", "500" },
                { "sendtime", "2023100417080000" },
                { "buyerip", "127.0.0.1" }
            };

            // merchantno+"|"+morderno+"|"+bankcode+"|"+type+"|"+ realname +"|"+cardno + "|"+money +"|"+sendtime+ "|"+buyerip +"|"+md5key
            var expectSignConcatSourceString =
                "test|621435|BDT_BKASH|0|first last|ChinaAtmNumber|500|2023100417080000|127.0.0.1|md5Key";
            var expectedSignResult = "44f2aa8115aefcf389ea9d640c53f67e";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void B01_VerifySign_Debit()
        {
            var receiveFormData = new Dictionary<string, string>
            {
                { "orderno", "orderno" },
                { "merchantno", "merchantno" },
                { "morderno", "morderno" },
                { "paycode", "paycode" },
                { "tjmoney", "100" },
                { "money", "100.00" },
                { "status", "1" },
                { "sign", "f40084726d41880b3ae018a1669394cd" }
            };

            var callbackSignKeys = new List<string>
            {
                "merchantno",
                "orderno",
                "morderno",
                "paycode",
                "tjmoney",
                "money",
                "status"
            };

            // merchantno+“|”+orderno+“|”+morderno+ “|”+paycode+“|”+tjmoney+“|”+money+“|”+status + “|”+md5key
            var expectedSignConcatSourceString = "merchantno|orderno|morderno|paycode|100|100.00|1|md5Key";
            AssertVerifySign(receiveFormData, callbackSignKeys, expectedSignConcatSourceString);
        }
        
        [Test]
        public void B02_VerifySign_Credit()
        {
            var receiveFormData = new Dictionary<string, string>
            {
                { "orderno", "AeePayOrderNo" },
                { "merchantno", "test" },
                { "morderno", "621435" },
                { "bankcode", "BDT_BKASH" },
                { "cardno", "01234567890" },
                { "realname", "first last" },
                { "tjmoney", "1000" },
                { "money", "999" },
                { "status", "3" },
                { "sign", "c48819303a6fa28fb2c8b7cb700360b8" }
            };

            var callbackSignKeys= new List<string>
            {
                "merchantno",
                "orderno",
                "bankcode",
                "morderno",
                "cardno",
                "tjmoney",
                "money",
                "status"
            };

            // merchantno+“|”+orderno+“|”+bankcode+“|”+morderno +“|”+cardno+“|”+tjmoney+“|”+money+“|”+status+“|”+md5key
            var expectedSignConcatSourceString = "test|AeePayOrderNo|BDT_BKASH|621435|01234567890|1000|999|3|md5Key";
            AssertVerifySign(receiveFormData, callbackSignKeys, expectedSignConcatSourceString);
        }

        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString,
            string expectedSignResult)
        {
            var actual = _target.GetPostSign(testData, _secretKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
        
        private void AssertVerifySign(Dictionary<string, string> receiveFormData, List<string> callbackSignKeys, string expectedSignConcatSourceString)
        {
            var actual = _target.VerifySign(receiveFormData, callbackSignKeys, _secretKey);
            PayHelper.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
            actual.Should().BeTrue();
        }
    }
}