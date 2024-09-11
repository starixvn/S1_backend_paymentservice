using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.RarepidPay;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class RarepidPayHelperTests
    {
        private PayHelper _target;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            var flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(flurlHttpHelper);
            _target = new PayHelper(serviceProvider);
        }

        [Test]
        public void A01_Debit_success()
        {
            // Arrange`
            var inputs = new Dictionary<string, string>();

            // Act
            var actual = _target.Debit(inputs);

            // Assert
            var expected = new DepositResult
            {
                Success = true
            };
            actual.Should().BeEquivalentTo(expected);
        }
    }
}