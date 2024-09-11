using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Payment.RarepidPay;
using PaymentCore.Exceptions;
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
            var inputs = GenerateInputs();
            var depositResponse = GivenDepositResponse("0", "NO_ERROR");

            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth ))
                .WithHeader("accept", "application/json")
                .PostJson<string, DepositResponse>(
                    Arg.Do<string>(actualUrl=> _actualUrl = actualUrl ), 
                    Arg.Do<string>(actualData => _actualData = actualData))
                .Returns((depositResponse, HttpStatusCode.OK));

            var expected = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = "D0000000001",
                Message = "NO_ERROR",
                Url = "https://api.{domain}.com/Api/StartWidget/1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039"
            };
            AssertDebitShouldReturn(inputs, expected);

            AssertCreateOrderUrlShouldReceive();
            AssertCreateOrderAuthorizationShouldReceive();
            AssertCreateOrderDataShouldReceive(inputs);
        }

        [Test]
        public void A02_Debit_fail()
        {
            var inputs = GenerateInputs();
            var depositResponse = GivenDepositResponse("306", "ERR_DUPLICATE_ID");

            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth))
                .WithHeader("accept", "application/json")
                .PostJson<string, DepositResponse>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                    Arg.Do<string>(actualData => _actualData = actualData))
                .Returns((depositResponse, HttpStatusCode.OK));

            var expected = new DepositResult
            {
                Success = false,
                ErrorCode = "9000",
                Message = "RarepidPay create order failed: Status=306, StatusMessage=ERR_DUPLICATE_ID",
            };
            AssertDebitShouldReturn(inputs, expected);

            AssertCreateOrderUrlShouldReceive();
            AssertCreateOrderAuthorizationShouldReceive();
            AssertCreateOrderDataShouldReceive(inputs);
        }

        private static DepositResponse GivenDepositResponse(string status, string statusMessage)
        {
            return new DepositResponse
            {
                Status = status,
                StatusMessage = statusMessage,
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

        private void AssertDebitShouldReturn(Dictionary<string,string> inputs, DepositResult expected)
        {
            var actual = _target.Debit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}