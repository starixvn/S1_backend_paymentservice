using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.PomPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class PomPayHelperTest
    {
        private PayHelper _target;
        private IFlurlHttpHelper _flurlHttpHelper;
        private string _clientId;
        private string _clientSecret;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _clientId = "UPLTRX";
            _clientSecret = "e3323b4dac596077fbc7198df06c52bf20990eb3";
        }

        [Test]
        public void A01_Hash_Debit()
        {   
            var parameters = new Dictionary<string, string>
            {
                { "transactionId", "1234321" },
                { "custName", "johndoe" },
                { "custSecondaryName", "alexsec" },
                { "custBank", "scb" },
                { "custMobile", "0123456789" },
                { "custEmail", "john@gmail.com" },
                { "amount", "50.00" },
                { "returnUrl", "https://www.merchant.com/return" },
                { "callbackUrl", "https://www.merchant.com/transaction/post" },
                { "bankAcc", "123456" }
            };

            var expectSignConcatSourceString =
                "UPLTRX1234321johndoealexsecscb0123456789john@gmail.com50.00https://www.merchant.com/returnhttps://www.merchant.com/transaction/post123456e3323b4dac596077fbc7198df06c52bf20990eb3";
            var expectedHashResult = "0c0fd00628b3fa7870e9c185bd9e2cc40bcc29a2f5d5b333c26fdc5f537435c9";

            AssertHashShouldReturn("Debit", parameters, expectSignConcatSourceString, expectedHashResult);
        }

        [Test]
        public void A02_Hash_Credit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "transactionId", "1234321" },
                { "custName", "AlexRobert" },
                { "custBank", "mbb" },
                { "custBankAcc", "8001002293" },
                { "custMobile", "095988888" },
                { "custEmail", "test1@email.com" },
                { "amount", "500.00" },
                { "callbackUrl", "https://www.merchant.com/callback" }
            };

            var expectSignConcatSourceString =
                "UPLTRX1234321AlexRobertmbb8001002293095988888test1@email.com500.00https://www.merchant.com/callbacke3323b4dac596077fbc7198df06c52bf20990eb3";
            var expectedHashResult = "d1fb3e9ee32ae209d8f0c5a11ef8aa58c10e7a46e1cd5a81da53e2572d3c28ee";

            AssertHashShouldReturn("Credit", parameters, expectSignConcatSourceString, expectedHashResult);
        }

        [Test]
        public void B01_ValidateHash_CallBackDebit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "referenceId", "1234321" },
                { "transactionId", "TRX1234321" },
                { "custName", "TAN AHBENG" },
                { "custSecondaryName", "" },
                { "bankName", "scb" },
                { "bankReference", "bankReference" },
                { "bankReferenceTwo", "bankReferenceTwo" },
                { "amount", "50.00" },
                { "status", "success" },
                { "hashVal", "09e220a72773e3815b20755f0c6f3ef74e62e92b964652a8a53e3bd96b426623" }
            };

            var expectSignConcatSourceString =
                "UPLTRX1234321TRX1234321TAN AHBENG50.00successe3323b4dac596077fbc7198df06c52bf20990eb3";

            AssertValidateHashShouldReturn("CallBackDebit", parameters, parameters["hashVal"], expectSignConcatSourceString, true);
        }

        [Test]
        public void B02_ValidateHash_CallBackCredit()
        {
            var parameters = new Dictionary<string, string>
            {
                { "transactionId", "1234321" },
                { "referenceId", "PO1223131" },
                { "custName", "AlexRobert" },
                { "custBank", "mbb" },
                { "custBankAcc", "8001002293" },
                { "amount", "50.00" },
                { "status", "success" },
                { "hashVal", "cea83e521e95aa69788e32d081992ca3b92348b5dad5bf9a97ff75619f99874b" }
            };

            var expectSignConcatSourceString =
                "UPLTRX1234321PO1223131AlexRobertmbb800100229350.00successe3323b4dac596077fbc7198df06c52bf20990eb3";

            AssertValidateHashShouldReturn("CallBackCredit", parameters, parameters["hashVal"], expectSignConcatSourceString, true);
        }

        private void AssertValidateHashShouldReturn(string type,
            Dictionary<string, string> parameters,
            string hash,
            string expectSignConcatSourceString,
            bool expectedHashResult)
        {
            var actual = _target.ValidateHash(type, parameters, hash, _clientId, _clientSecret);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedHashResult);
        }

        private void AssertHashShouldReturn(
            string type,
            Dictionary<string, string> parameters,
            string expectSignConcatSourceString,
            string expectedHashResult)
        {
            var actual = _target.Hash(type, parameters, _clientId, _clientSecret);
            _target.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedHashResult);
        }
    }
}