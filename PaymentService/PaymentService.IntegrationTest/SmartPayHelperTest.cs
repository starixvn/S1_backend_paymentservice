using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.SmartPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class SmartPayHelperTest
    {
        private string _secretKey;
        private IFlurlHttpHelper _flurlHttpHelper;
        private PayHelper _target;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _secretKey = "2cf6782df6d7478f872954ef8ff45a16";
        }

        [Test]
        public void A01_Sign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "version", "V2" },
                { "signType", "MD5" },
                { "merchantNo", "API2442810283706600" },
                { "date", "20191127172151" },
                { "channleType", "1" },
                { "noticeUrl", "http://abc.com/DepositReceive/SmartPay/risk" },
                { "orderNo", "2617951" },
                { "bizAmt", "200" },
                { "bankCode", "V_BIDV" },
                { "returnUrl", "http://abc.com/Proxy/Result/SmartPay/vn" }
            };

            var expectSignConcatSourceString =
                "bankCode=V_BIDV&bizAmt=200&channleType=1&date=20191127172151&merchantNo=API2442810283706600&noticeUrl=http://abc.com/DepositReceive/SmartPay/risk&orderNo=2617951&returnUrl=http://abc.com/Proxy/Result/SmartPay/vn&signType=MD5&version=V22cf6782df6d7478f872954ef8ff45a16";
            var expectedSignResult = "c57f4f0439efe9051c726660ff06a128";

            AssertSignShouldReturn(parameters, expectedSignResult, expectSignConcatSourceString);
        }

        [Test]
        public void A02_Sign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "version", "V2" },
                { "signType", "MD5" },
                { "merchantNo", "API2442810283706600" },
                { "date", "20221104154422" },
                { "channleType", "1" },
                { "orderNo", "123456" },
                { "bizAmt", "200000" },
                { "accName", "accountName" },
                { "bankCode", "V_VIETCOMBANK" },
                { "bankBranchName", "Branch" },
                { "cardNo", "123456654321" },
                { "noticeUrl", "http://abc.com/WithdrawalReceive/SmartPay" }
            };

            var expectSignConcatSourceString =
                "accName=accountName&bankBranchName=Branch&bankCode=V_VIETCOMBANK&bizAmt=200000&cardNo=123456654321&channleType=1&date=20221104154422&merchantNo=API2442810283706600&noticeUrl=http://abc.com/WithdrawalReceive/SmartPay&orderNo=123456&signType=MD5&version=V22cf6782df6d7478f872954ef8ff45a16";
            var expectedSignResult = "1803b2c9332e48bc52508dbb7834b1c9";

            AssertSignShouldReturn(parameters, expectedSignResult, expectSignConcatSourceString);
        }

        [Test]
        [TestCase("1", "V_BIDV", "1", "V_BIDV")]
        [TestCase("2-V_MOMOPAY", null, "2", "V_MOMOPAY")]
        [TestCase("3-V_BIDV", null, "3", "V_BIDV")]
        [TestCase("4", null, "4", null)]
        public void B01_GetPayCodeAndBankCode(string payCode, string bankCode, string expectedPayCode, string expectedBankCode)
        {
            var inputs = GivenInputs(payCode, bankCode);

            AssertGetPayCodeAndBankCodeShouldReturn(expectedPayCode, expectedBankCode, inputs);
        }

        [Test]
        public void C01_ValidateSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "orderNo", "123456" },
                { "orderAmt", "10000" },
                { "bizAmt", "10000" },
                { "status", "1" },
                { "remark", "" },
                { "version", "V2" },
                { "date", "20221101185530" },
                { "notes", "" },
                { "sign", "a34ab8c0f78cff021e94d3ea552acbc2" }
            };

            var expectSignConcatSourceString =
                "bizAmt=10000&date=20221101185530&orderAmt=10000&orderNo=123456&status=1&version=V22cf6782df6d7478f872954ef8ff45a16";

            AssertValidateSignShouldReturn(parameters, expectSignConcatSourceString, true);
        }

        [Test]
        public void C02_ValidateSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "orderNo", "123456" },
                { "orderAmt", "10000" },
                { "bizAmt", "10000" },
                { "status", "1" },
                { "remark", "" },
                { "version", "V2" },
                { "date", "20221101185530" },
                { "notes", "" },
                { "sign", "a34ab8c0f78cff021e94d3ea552acbc2" }
            };

            var expectSignConcatSourceString =
                "bizAmt=10000&date=20221101185530&orderAmt=10000&orderNo=123456&status=1&version=V22cf6782df6d7478f872954ef8ff45a16";

            AssertValidateSignShouldReturn(parameters, expectSignConcatSourceString, true);
        }

        private void AssertValidateSignShouldReturn(Dictionary<string, string> parameters, string expectSignConcatSourceString, bool expectedResult)
        {
            var actual = _target.ValidateSign(parameters, parameters["sign"], _secretKey);
            actual.Should().Be(expectedResult);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
        }

        private void AssertGetPayCodeAndBankCodeShouldReturn(string expectedPayCode, string expectedBankCode, Dictionary<string, string> inputs)
        {
            var (actualPayCode, actualBankCode) = _target.GetPayCodeAndBankCode(inputs);
            actualPayCode.Should().Be(expectedPayCode);
            actualBankCode.Should().Be(expectedBankCode);
        }

        private static Dictionary<string, string> GivenInputs(string payCode, string bankCode)
        {
            return new Dictionary<string, string>
            {
                { "PayCode", payCode },
                { "BankCode", bankCode }
            };
        }

        private void AssertSignShouldReturn(
            Dictionary<string, string> parameters,
            string expectedSignResult,
            string expectSignConcatSourceString)
        {
            var actual = _target.Sign(parameters, _secretKey);
            actual.Should().Be(expectedSignResult);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
        }
    }
}