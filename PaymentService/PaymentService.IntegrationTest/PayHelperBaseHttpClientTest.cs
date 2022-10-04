using NUnit.Framework;
using PaymentCore;
using PaymentCore.Models.Result;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class PayHelperBaseHttpClientTest
    {
        public class PayHelperBaseTest : PayHelperBase
        {
            #region ignore

            public override DepositResult Debit(Dictionary<string, string> inputs)
            {
                throw new NotImplementedException();
            }

            public override WithdrawResult Credit(Dictionary<string, string> inputs)
            {
                throw new NotImplementedException();
            }

            public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
            {
                throw new NotImplementedException();
            }

            public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
            {
                throw new NotImplementedException();
            }

            public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
            {
                throw new NotImplementedException();
            }

            public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
            {
                throw new NotImplementedException();
            }

            public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
            {
                throw new NotImplementedException();
            }

            #endregion

            public string TestPost()
            {
                var url = "http://52.221.19.140:59159/Payment/WithdrawalManualApprove";
                var body = "{\"TransId\": \"WD135922\",\"ThirdId\": \"\",\"Operator\": \"Admin\"}";
                var response = HttpPost(url, body, "application/json", "WD135922");
                return response;
            }

            public string TestPost_WithCertificate()
            {
                var url = "http://52.221.19.140:59159/Payment/WithdrawalManualApprove";
                var body = "{\"TransId\": \"WD135922\",\"ThirdId\": \"\",\"Operator\": \"Admin\"}";
                var response = HttpPost(url, body, "application/json", "WD135922", true);
                return response;
            }

            public string TestGet()
            {
                var url = "http://52.221.19.140:59159/Payment/GetClientIp?Name=abc";
                var response = HttpGet(url, string.Empty, "WD135922");
                return response;
            }

            public string TestGet_WithCertificate()
            {
                var url = "http://52.221.19.140:59159/Payment/GetClientIp?Name=abc";
                var response = HttpGet(url, string.Empty, "WD135922", true);
                return response;
            }
        }


        [Test]
        public void TestPost_JsonBody()
        {
            var payhelperbase = new PayHelperBaseTest();
            var actual = payhelperbase.TestPost();
            Assert.True(actual.Contains("withdrawal"));
        }

        [Test]
        public void TestPost_JsonBody_WithCertificateTrue()
        {
            var payhelperbase = new PayHelperBaseTest();
            var actual = payhelperbase.TestPost_WithCertificate();
            Assert.True(actual.Contains("withdrawal"));
        }

        [Test]
        public void TestGet()
        {
            var payhelperbase = new PayHelperBaseTest();
            var actual = payhelperbase.TestGet();
            Assert.AreEqual(actual, "218.32.244.12");
        }

        [Test]
        public void TestGet_WithCertificate()
        {
            var payhelperbase = new PayHelperBaseTest();
            var actual = payhelperbase.TestGet_WithCertificate();
            Assert.AreEqual(actual, "218.32.244.12");
        }
    }
}
