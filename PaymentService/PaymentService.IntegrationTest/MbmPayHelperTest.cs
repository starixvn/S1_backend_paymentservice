using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.MbmPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class MbmPayHelperTest
    {
        private SubPayHelper _target;
        private string _key;
        private IFlurlHttpHelper _flurlHttpHelper;
        private ILogger<PayHelper> _logger;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            _logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(_logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new SubPayHelper(serviceProvider);
            _key = "e997cb371b980a39260ae868bb3cd46f";
        }

        [Test]
        public void A01_Sign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "mchId", "108001" },
                { "signType", "md5" },
                { "mchOrderNo", "TV21693473076036" },
                { "amount", "10000" },
                { "channelType", "upi" },
                { "terminalType", "PC" },
                { "notifyUrl", "http://127.0.0.1:8080/notify" },
                { "payer", null },
                { "extra", "" },
                { "returnUrl", "https://www.google.com" },
                { "rand", _target.GetRandomString() }
            };

            var expectSignConcatSourceString =
                "amount=10000&channelType=upi&mchId=108001&mchOrderNo=TV21693473076036&notifyUrl=http://127.0.0.1:8080/notify&rand=3uz6b0m2cs865h20lh5ijr3q334wk0c8&returnUrl=https://www.google.com&signType=md5&terminalType=PC&key=e997cb371b980a39260ae868bb3cd46f";
            var expectedSignResult = "f5e4c3ee08cc65ec0ce17537aea03448";

            AssertSignShouldReturn(parameters, expectedSignResult, expectSignConcatSourceString);
        }

        [Test]
        public void A02_Sign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "mchId", "108001" },
                { "signType", "md5" },
                { "mchOrderNo", "TEST1668259858392" },
                { "amount", "10000" },
                { "channelType", "xxx" },
                { "notifyUrl", "http://xxxxx" },
                { "bankName", "bankName" },
                { "bankCode", "bankCode" },
                { "cardName", "cardName" },
                { "cardNo", "cardNo" },
                { "rand", "mj4z9sxzs7jmzrrqx01rs4vthii6ezqc" },
                { "sign", "829d29080af55b586408b7adce9917bd" }
            };

            var expectSignConcatSourceString =
                "amount=10000&bankCode=bankCode&bankName=bankName&cardName=cardName&cardNo=cardNo&channelType=xxx&mchId=108001&mchOrderNo=TEST1668259858392&notifyUrl=http://xxxxx&rand=mj4z9sxzs7jmzrrqx01rs4vthii6ezqc&signType=md5&key=e997cb371b980a39260ae868bb3cd46f";
            var expectedSignResult = "829d29080af55b586408b7adce9917bd";

            AssertSignShouldReturn(parameters, expectedSignResult, expectSignConcatSourceString);
        }

        [Test]
        public void B01_ValidateSign_Debit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "mchId", "108001" },
                { "signType", "md5" },
                { "sign", "e3f373362e4d1823871107203f534d69" },
                { "orderId", "AA22111820181290101" },
                { "mchOrderNo", "TEST1668245468575" },
                { "amountApply", "10000" },
                { "amount", "10000" },
                { "type", "channel" },
                { "payer", "test payer" },
                { "extra", "{}" },
                { "trx", "331256896452" },
                { "upi", "sdfsadf@upi" },
                { "rand", "c824cd23d6d549ba2a07d2a30014136d" },
                { "origin", "deposit" },
                { "status", "PAID" }
            };

            var expectSignConcatSourceString =
                "amount=10000&amountApply=10000&extra={}&mchId=108001&mchOrderNo=TEST1668245468575&orderId=AA22111820181290101&origin=deposit&payer=test payer&rand=c824cd23d6d549ba2a07d2a30014136d&signType=md5&status=PAID&trx=331256896452&type=channel&upi=sdfsadf@upi&key=e997cb371b980a39260ae868bb3cd46f";
            
            AssertValidateSignShouldReturn(parameters, expectSignConcatSourceString, true);
        }

        [Test]
        public void B02_ValidateSign_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "mchId", "108001" },
                { "signType", "md5" },
                { "sign", "c329c75050aff19ec142efc9c3d88b83" },
                { "orderId", "AA22111820181290101" },
                { "mchOrderNo", "TEST1668245468575" },
                { "amountApply", "10000" },
                { "amount", "10000" },
                { "type", "channel" },
                { "payer", "test payer" },
                { "extra", "{}" },
                { "trx", "331256896452" },
                { "upi", "sdfsadf@upi" },
                { "rand", "d9e3mr1nsm9vzf0fw6708hinny5bcuth" },
                { "origin", "payout" },
                { "status", "SUCCESS" }
            };

            var expectSignConcatSourceString =
                "amount=10000&amountApply=10000&extra={}&mchId=108001&mchOrderNo=TEST1668245468575&orderId=AA22111820181290101&origin=payout&payer=test payer&rand=d9e3mr1nsm9vzf0fw6708hinny5bcuth&signType=md5&status=SUCCESS&trx=331256896452&type=channel&upi=sdfsadf@upi&key=e997cb371b980a39260ae868bb3cd46f";
            
            AssertValidateSignShouldReturn(parameters, expectSignConcatSourceString, true);
        }

        private void AssertValidateSignShouldReturn(Dictionary<string,string> parameters, string expectSignConcatSourceString, bool expectedResult)
        {
            var actual = _target.ValidateSign(parameters, parameters["sign"], _key);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedResult);
        }

        private void AssertSignShouldReturn(Dictionary<string, string> parameters,
            string expectedSignResult,
            string expectSignConcatSourceString)
        {
            var actual = _target.Sign(parameters, _key);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }
    }

    internal class SubPayHelper : PayHelper
    {
        public SubPayHelper(IServiceProvider provider)
        : base(provider)
        {
        }
        
        internal override string GetRandomString()
        {
            return "3uz6b0m2cs865h20lh5ijr3q334wk0c8";
        }
    }
}