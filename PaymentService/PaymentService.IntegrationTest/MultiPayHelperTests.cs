using FluentAssertions;
using NUnit.Framework;
using Payment.MultiPay;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class MultiPayHelperTests
    {
        private PayHelper _target;
        private string _publicKey;
        private string _privateKey;

        [SetUp]
        public void SetUp()
        {
            _target = new PayHelper();

            _publicKey = "-----BEGIN PUBLIC KEY-----\nMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAtw2U4OWxA1GdIG+D1gWy\nnkmxk1JQtdU02X76T/iasXeG4Zs2niuxvbSXEW8s9eAIlo0PrU569VdDT/69zSYu\njzmHk74DbIFUsBnri7J+lxeuqzLKRsC76UQnaK+beTsxIX5UPdEj/ZQoq77mzk59\nJgXyKxneYnwv3Fzo3lgzT8bSBmv9ipv+4PdH2crMdnnJmhLC3ttJcKQY1WiaN5pw\nmT4L0koYe4h99+ufvUye4XLYjJWvaJs7bcszN/tdcBBERV32lsz4Ju0xZGrrvH6r\nPQH1vVRpMgDve5MRyb5OBibQzaVkE4SI6EswnoJfV1TuLjOdanJYbhM7q9JV8ryv\n6V+gVuBER4rY1wlW/8nQxUO2lqZAlhtB92PA1LSr6VuZQSBoIsMaOjJ9nuCVuaEh\nQgAiSNnFqNCZlmWWT9g7if7INOUCtZSSa33/nf8CPlZo9B1TxaGgG0fQdDPnrPM0\njsEmuTpwFnspqYAKKfSHNKx6t9yZWlEtym98IoC7bnKkxttc8E/86yHa7GQR3R1b\n+naS2hnLwDhPrTtdnsLlzPxumtTfTYMBd3OngJo8D6PhnXQMU9suEQJyE3cM8goB\n8VMMDxK4odwTGNDs+zZDTA0e8V5MbS8AFmAWDjJ0GudXlPireFeKqNmlrEhPdst5\nm2jqt8TsYfrdMHeZ1mdg//UCAwEAAQ==\n-----END PUBLIC KEY-----\n"; 
            _privateKey = "-----BEGIN PRIVATE KEY-----\\n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\\np6hkNZts3q9Jz4gAGv0xwWvXrdnSsw==\\n-----END PRIVATE KEY-----";
        }

        [Test]
        public void A01_Encrypt_ValidateSign_Debit()
        {
            var parameters = new Dictionary<string, object>
            {
                { "amount", "1000" },
                { "callback_url", "https://your-callback-url" },
                { "channel_code", "TEST" },
                { "order_id", "TEST00001" },
                { "timestamp", 1700000000123 }
            };

            var expectedConcatSourceString =
                "amount=1000&callback_url=https://your-callback-url&channel_code=TEST&order_id=TEST00001&timestamp=1700000000123";
            AssertEncryptShouldReturnSign(parameters, expectedConcatSourceString);
            AssertValidateSignShouldReturn(parameters, expectedConcatSourceString);
        }
        
        [Test]
        public void A02_Encrypt_ValidateSign_Credit()
        {
            var parameters = new Dictionary<string, object>
            {
                { "amount", "1000" },
                { "currency", "INR" },
                { "account_name", "John Doe" },
                { "card_number", "88888888888888" },
                { "bank_code", "SBIN0000300" },
                { "order_id", "TEST00001" },
                { "channel_code", "TEST" },
                { "timestamp", 1700000000123 },
                { "callback_url", "https://your-callback-url" }
            };

            var expectedConcatSourceString =
                "account_name=John Doe&amount=1000&bank_code=SBIN0000300&callback_url=https://your-callback-url&card_number=88888888888888&channel_code=TEST&currency=INR&order_id=TEST00001&timestamp=1700000000123";
            AssertEncryptShouldReturnSign(parameters, expectedConcatSourceString);
            AssertValidateSignShouldReturn(parameters, expectedConcatSourceString);
        }

        [Test]
        public void B01_ValidateSign_CallBackDebit()
        {
            var parameters = new Dictionary<string, object>
            {
                { "order_id", "TEST00001" },
                { "platform_order_id", "MPIN00001" },
                { "status", "1" },
                { "amount", "1000" },
                { "true_amount", "1000" },
                { "timestamp", "1700000000123" },
                { "sign", "nU7HcRjR9YR6lTH5kgO2HhlbeV8d+rRhVlq7LxRmUr8zMjRCYRom5buTNRUHXuTr4QkS5kTHOUnA7ju9HR6GGNnmlxILNHhTS1tyXNn8ArAX3Jo0XcyU6DO7MeFC3rol/ccdQ8+eeewCPTtXrb/pDhHXemEp6MsN2dac+P1OWE8nHZIR6SOVD717uyJxqobJ/RAI94tTF+TiFMdh+g6iL1GLl2SGHX3HWBbQQuvXa4vN9sdVeHPBefXhN5tTLXCQKoRH6UU5fkmKsnp085ZBYlgsK5fEAV6+ZEq5JlNCo7+Ae/sNds8CET2Nni3K0BlT7owLXWiCnYPXIh/jmHaYntW8jlS3Wivvt9LsmG7bb5W5w2IgGgYf5Gxl793hprUTghqGSH86TPQTuhSLjShfaZ7MTwjhW41/mrNIVMRqrErZRwvtzURBSviDjz+/NMbdYKKeyYNeM8cbP7pSUGYijO9gfODKHAHQqP5nPQEzCMvOxiKIGtqTUDqNrjYC4SiIsKB4s1yOdzxTFDCwkJfArZ7/xn/92RHvi/iyTXk3I/NzP8udJxdnBLqqpYTX5SVy7z7Kdv7//j48niSmVElF5gLeXwQnUjvBzWqpOWRmuCphfkbPE1PEEYsyvVFTBNFvJMy5o6MRYFlL5k+fDdva9caXFBuYMcPxPGv4XaKMqnc=" }
            };

            var expectedConcatSourceString =
                "amount=1000&order_id=TEST00001&platform_order_id=MPIN00001&status=1&timestamp=1700000000123&true_amount=1000";
            AssertValidateSignShouldReturn(parameters, expectedConcatSourceString);
        }

        [Test]
        public void B02_ValidateSign_CallBackCredit()
        {
            var parameters = new Dictionary<string, object>
            {
                { "order_id", "TEST00001" },
                { "platform_order_id", "MPOUT00001" },
                { "status", "1" },
                { "amount", "1000" },
                { "timestamp", "1700000000123" },
                { "sign", "RkC+4oHiRezYGyC775DX745CekiIm59J2QcxN4q2Ley7f1loWQSduSbz3V0aqQguGgpXrMvCCuu2kvDJXILfDaVxW8195qkC6aQ0LrFpo8ffbHNEGzKiLhPaNY9tw/tZkROwm/I/VyyU6nN/COSn8fI9BtrgO/D9gv73WQ17AqsCUedNxt/NXfK8zJPA+Pyo0KShxsphCGXeAslFMMUqicgjNQWnXriRvOhM6COir8Sjx84tbOGYuB530qZH4R47uyMimjXZ4qaZl9tBXWzngExH84+pAnQcQGo6Q7yuEak5xri/VKdQTLtRUQEqWCkgwjvFWXKoO82YgSFPqXl7hkJUX/eZRok4/hdwVBL0kG3VJ54t1QLoWSqU49oHlQD2FyVqet72dd0+q/LbgWZ3ybTyTqkZ5EubT3Qj+dbO2hMxJd0zipPck6LgjO7+m4CmAdN5X+UPG11ihfpnJ2ciSJPPJGpD69rHzoPomFmShNQedu+yycGbey7A4xIT/3jPs1NP0WI+0mM4d8s+hQyAHRAFF0BjHS/MqPXZr/0tPY2mLpncJmbJxX+2gWE9nFKs7qlD+IuJRCvdAIJGOEVbZJGZXVam8rwqrjfQYtUJW0B4+ATBfoNGf8WBkDTLO+lViX4b7Dx2AcyHIHsmZs/WGbQJwy5IiIi6ZArj4yiSAHc=" }
            };

            var expectedConcatSourceString =
                "amount=1000&order_id=TEST00001&platform_order_id=MPOUT00001&status=1&timestamp=1700000000123";
            AssertValidateSignShouldReturn(parameters, expectedConcatSourceString);
        }

        private void AssertValidateSignShouldReturn(Dictionary<string, object> parameters, string expectedConcatSourceString)
        {
            var (actualValidateResult, actualDecryptData) = _target.ValidateSign(parameters, _privateKey);
            actualDecryptData.Should().Be(expectedConcatSourceString);
            actualValidateResult.Should().BeTrue();
        }

        private void AssertEncryptShouldReturnSign(Dictionary<string, object> parameters, string expectedConcatSourceString)
        {
            var actual = _target.Encrypt(parameters, _publicKey);
            _target.SignConcatSourceString.Should().Be(expectedConcatSourceString);
            parameters.Add("sign", actual);
        }
    }
}