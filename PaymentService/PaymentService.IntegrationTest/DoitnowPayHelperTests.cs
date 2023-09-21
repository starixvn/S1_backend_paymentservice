using NUnit.Framework;
using Payment.DoitnowPay;


namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class DoitnowPayHelperTests
    {
        private readonly IServiceProvider _provider;
        [Test]
        public void HashTest()
        {
            var testData = new Dictionary<string, string>()
            {
                { "clientId", "NOVA" },
                { "transactionId", "DD1401406" },
                { "custName", "hulkchou" },
                { "amount", "100" },
                { "returnUrl", "http://www.n88dev.com:/en/Cashier/Deposit?UserLang=en" },
                { "callbackUrl", "http://payment.n88dev.com:59159/Payment/DepositCallBack/DD1401406" },
                { "paymentMethod", "eWallet" }
            };
            var expect = "e7e032d86ae9e0c3d434657ccbc956adea99d0662129f0e999aba6fee6face5a";
            var payHelper = new PayHelper(_provider); 
            var actual = payHelper.Hash(testData, "84dc3f1fae83e831657e339414719b9e4fbfae39");

            Assert.AreEqual(expect, actual);
        }
        [Test]
        public void CallBackSuccessHashTest()
        {
            var testData = new Dictionary<string, string>()
            {
                { "clientId", "NOVA" },
                { "referenceId", "TK2208172A4E93" },
                { "transactionId", "DD1401723" },
                { "custName", "習近平" },
                { "amount", "500.00" },
                { "status", "success" }
            };
            var expect = "2734ac4aefe0dd265f50830d4dbd25d29e9fe1fd57c62252af7737df4491eede";
            var payHelper = new PayHelper(_provider); 
            var actual = payHelper.Hash(testData, "84dc3f1fae83e831657e339414719b9e4fbfae39");

            Assert.AreEqual(expect, actual);
        }
        
        [Test]
        public void CallBackFailedHashTest()
        {
            var testData = new Dictionary<string, string>()
            {
                { "clientId", "NOVA" },
                { "transactionId", "DP2925450" },
                { "status", "failed" }
            };
            var expect = "3732d010c0d0b152097a150904381e842cbc3895801b275ca2c02cf3538e31ca";
            var payHelper = new PayHelper(_provider); 
            var actual = payHelper.Hash(testData, "92eac68297c0613385d6329c3c9409e5d623f100");

            Assert.AreEqual(expect, actual);
        }
    }
}