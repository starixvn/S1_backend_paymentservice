using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private string _actualAuth;
        private string _actualUrl;
        private string _actualData;

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
            var inputs = GenerateInputs();

            var depositResponse = GivenDepositResponse();

            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth ))
                .WithHeader("accept", "application/json")
                .PostJson<string, DepositResponse>(
                    Arg.Do<string>(actualUrl=> _actualUrl = actualUrl ), 
                    Arg.Do<string>(actualData => _actualData = actualData))
                .Returns((depositResponse, HttpStatusCode.OK));

            // Act
            var actual = _target.Debit(inputs);

            // Assert
            AssertDebitShouldReturn(actual);

            AssertCreateOrderUrlShouldReceive();
            AssertCreateOrderAuthorizationShouldReceive();
            AssertCreateOrderDataShouldReceive(inputs);
        }

        private static DepositResponse GivenDepositResponse()
        {
            return new DepositResponse
            {
                Status = "0",
                StatusMessage = "NO_ERROR",
                TranId = "TransID",
                PgTranId = "D0000000001",
                Token = "1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039",
                Amount = "1000",
                TotalFees = "1.15",
                Currency = "INR",
                RedirectTo = "https://api.{domain}.com/Api/StartWidget/1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039"
            };
        }

        private static Dictionary<string, string> GenerateInputs()
        {
            return new Dictionary<string, string>
            {
                { "Host", "http://api/" },
                { "Contract", "Contract" },
                { "ApiKey", "ApiKey" },
                { "CallBackUrl", "CallBackUrl" },
                { "ReturnUrl", "ReturnUrl" },
                { "CustId", "CustId" },
                { "Amount", "1000" },
                { "TransID", "TransID" }
            };
        }

        private void AssertCreateOrderDataShouldReceive(Dictionary<string, string> inputs)
        {
            var expectedData = JsonConvert.SerializeObject(new DepositRequest
            {
                HttpPostUrl = inputs["CallBackUrl"],
                ReturnUrl = inputs["ReturnUrl"],
                FailedReturnUrl = inputs["ReturnUrl"],
                Depositer = inputs["CustId"],
                Amount = int.Parse(inputs["Amount"]),
                TranId = inputs["TransID"],
                Remark = "service"
            });
            _actualData.Should().Be(expectedData);
        }

        private void AssertCreateOrderUrlShouldReceive()
        {
            var expectedUrl = "http://api/Api/QRDeposit";
            _actualUrl.Should().Be(expectedUrl);
        }

        private void AssertCreateOrderAuthorizationShouldReceive()
        {
            var expectedAuth = "Contract ApiKey";
            _actualAuth.Should().Be(expectedAuth);
        }

        private static void AssertDebitShouldReturn(DepositResult actual)
        {
            var expected = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = "D0000000001",
                Message = "NO_ERROR",
                Url = "https://api.{domain}.com/Api/StartWidget/1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039"
            };
            actual.Should().BeEquivalentTo(expected);
        }
    }
}