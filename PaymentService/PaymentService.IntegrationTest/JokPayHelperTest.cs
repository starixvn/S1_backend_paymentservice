using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.JokPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class JokPayHelperTest
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
                { "channleType", "0" },
                { "noticeUrl", "https://www.baidu.com/" },
                { "orderNo", "1574846511910" },
                { "bizAmt", "10" }
            };

            var expectSignConcatSourceString =
                "bizAmt=10&channleType=0&date=20191127172151&merchantNo=API2442810283706600&noticeUrl=https://www.baidu.com/&orderNo=1574846511910&signType=MD5&version=V22cf6782df6d7478f872954ef8ff45a16";
            var expectedSignResult = "c9937998ef046127f762a002a6e8be8a";

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
                { "noticeUrl", "http://abc.com" },
                { "openProvince", "" },
                { "openCity", "" },
                { "phone", "" }
            };

            var expectSignConcatSourceString =
                "accName=accountName&bankBranchName=Branch&bankCode=V_VIETCOMBANK&bizAmt=200000&cardNo=123456654321&channleType=1&date=20221104154422&merchantNo=API2442810283706600&noticeUrl=http://abc.com&orderNo=123456&signType=MD5&version=V22cf6782df6d7478f872954ef8ff45a16";
            var expectedSignResult = "e016197048e67d1a90a20426c665ec38";

            AssertSignShouldReturn(parameters, expectedSignResult, expectSignConcatSourceString);
        }

        [Test]
        public void B01_ValidateSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "orderNo", "123456" },
                { "orderAmt", "10000" },
                { "bizAmt", "10000" },
                { "status", "1" },
                { "notes", "" },
                { "remark", "" },
                { "version", "V2" },
                { "date", "20221101185530" },
                { "sign", "a34ab8c0f78cff021e94d3ea552acbc2" }
            };
            
            var expectedSignConcatSourceString =
                "bizAmt=10000&date=20221101185530&orderAmt=10000&orderNo=123456&status=1&version=V22cf6782df6d7478f872954ef8ff45a16";
            AssertValidateSignShouldReturn(parameters, true, expectedSignConcatSourceString);
        }

        [Test]
        public void B02_ValidateSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "orderNo", "123456" },
                { "bizAmt", "10000" },
                { "orderAmt", "10000" },
                { "status", "1" },
                { "notes", "" },
                { "remark", "" },
                { "version", "V2" },
                { "date", "20221101185530" },
                { "sign", "a34ab8c0f78cff021e94d3ea552acbc2" }
            };
            
            var expectedSignConcatSourceString =
                "bizAmt=10000&date=20221101185530&orderAmt=10000&orderNo=123456&status=1&version=V22cf6782df6d7478f872954ef8ff45a16";
            AssertValidateSignShouldReturn(parameters, true, expectedSignConcatSourceString);
        }

        private void AssertValidateSignShouldReturn(
            Dictionary<string, string> parameters,
            bool expectedResult,
            string expectedSignConcatSourceString)
        {
            var actual = _target.ValidateSign(parameters, parameters["sign"], _secretKey);
            actual.Should().Be(expectedResult);
            _target.SignConcatSourceString.Should().Be(expectedSignConcatSourceString);
        }

        private void AssertSignShouldReturn(Dictionary<string,string> parameters, string expectedSignResult, string expectSignConcatSourceString)
        {
            var actual = _target.Sign(parameters, _secretKey);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }
}