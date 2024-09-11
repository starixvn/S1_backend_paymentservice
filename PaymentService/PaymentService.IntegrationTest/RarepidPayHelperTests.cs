using System.Net;
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
        private IFlurlHttpHelper _flurlHttpHelper;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);
        }

        [Test]
        public void A01_Debit_success()
        {
            // Arrange
            var inputs = new Dictionary<string, string>
            {
                { "Host", "Host" },
                { "Contract", "Contract" },
                { "ApiKey", "ApiKey" },
                { "CallBackUrl", "CallBackUrl" },
                { "ReturnUrl", "ReturnUrl" },
                { "CustId", "CustId" },
                { "Amount", "1000" },
                { "TransID", "TransID" }
            };

            var depositResponse = new DepositResponse
            {
                Status = "0"
            };
            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Any<string>())
                .WithHeader("accept", "application/json")
                .PostJson<string, DepositResponse>(Arg.Any<string>(), Arg.Any<string>())
                .Returns((depositResponse, HttpStatusCode.OK));

            // Act
            var actual = _target.Debit(inputs);

            // Assert
            var expected = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
            };
            actual.Should().BeEquivalentTo(expected);
        }
    }
}