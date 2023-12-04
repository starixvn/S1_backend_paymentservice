using NUnit.Framework;

using Payment.CoPay;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class CoPayHelperTests
    {
        [Test]
        public void SignTest()
        {
            var testData = new Dictionary<string, string>()
            {
                { "uid", "nova2" },
                { "orderid", "DP123456" },
                { "channel", "907" },
                { "notify_url", "http://payment.n88dev.com:59159" },
                { "return_url", "https://stag.bwing.com" },
                { "amount",  "500" },
                { "userip", "123.4.5.67" },
                { "timestamp", "1655460961" },
                { "custom", "" }
            };
            var payHelper = new PayHelper();
            var expect = "83E6EF82AD8519E94A497E63B55B650A";

            var actual = payHelper.Sign(testData, "4nBa2Pxocn36vdq7");

            Assert.AreEqual(expect, actual);
        }
    }
}