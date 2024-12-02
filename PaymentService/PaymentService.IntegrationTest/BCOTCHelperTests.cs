using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.BCOTC;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class BCOTCHelperTests
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

            _secretKey = "123456";
        }

        [Test]
        public void A01_GetSign_Debit()
        {
            var model = new DepositPost
            {
                MerchantId = "ABC123",
                MerchantOrderId = "1230110052858051",
                Amount = "3000.00",
                NotifyUrl = "https://example.com/notify",
                Payer = "David",
                PaymentMethod = 100,
                ApplyTimeStamp = 1675998891.711982
            };

            var expectSignConcatSourceString = "{\"amount\":\"3000.00\",\"api_key\":\"123456\",\"apply_timestamp\":1675998891.711982,\"merchant_id\":\"ABC123\",\"merchant_order_id\":\"1230110052858051\",\"notify_url\":\"https://example.com/notify\",\"payer\":\"David\",\"payment_method\":100}";
            var expectedSignResult = "f9cc03a52eff8e26397ba62ab214f03b";

            AssertGetPostSign(model, expectSignConcatSourceString, expectedSignResult);
            AssertPostModelShouldBeSame(model);
        }

        [Test]
        public void B01_VerifySign_Debit()
        {
            var receiveModel = new PaymentReceive
            {
                MerchantId = "12345",
                OrderId = "d8c707db-d618-4f85-ba2d-3fc4d4ef55af",
                MerchantOrderId = "1230110052858051",
                Amount = "3000.00",
                CryptoAmount = "392.770000",
                Status = "completed",
                CompletedAt = 1677043762.096119m,
                Md5Sign = "99baf0e54cf6d96c8ab56c3b5277f752"
            };

            var expectSignConcatSourceString = "{\"amount\":\"3000.00\",\"api_key\":\"123456\",\"completed_at\":1677043762.096119,\"crypto_amount\":\"392.770000\",\"merchant_id\":\"12345\",\"merchant_order_id\":\"1230110052858051\",\"order_id\":\"d8c707db-d618-4f85-ba2d-3fc4d4ef55af\",\"status\":\"completed\"}";
            AssertVerifySign(receiveModel, expectSignConcatSourceString);
        }

        private void AssertGetPostSign(DepositPost model, string expectSignConcatSourceString,
            string expectedSignResult)
        {
            var actual = _target.GetPostSign(model, _secretKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }

        private static void AssertPostModelShouldBeSame(DepositPost model)
        {
            var expectModel = new DepositPost
            {
                MerchantId = model.MerchantId,
                MerchantOrderId = model.MerchantOrderId,
                Amount = model.Amount,
                NotifyUrl = model.NotifyUrl,
                Payer = model.Payer,
                PaymentMethod = model.PaymentMethod,
                ApplyTimeStamp = model.ApplyTimeStamp
            };
            model.Should().BeEquivalentTo(expectModel);
        }

        private void AssertVerifySign(PaymentReceive receiveModel, string expectSignConcatSourceString)
        {
            var actual = _target.VerifySign(receiveModel, _secretKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(true);
        }
    }
}