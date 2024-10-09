using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.RarepidPay;
using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;
using PaymentService.Service.Model;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class RarepidPayHelperTests : PayHelperTestBase
    {
        private const string RarepidPaySetting = "{\"Host\": \"http://localhost:59159/RarepidPay\", \"ApiKey\": \"ApiKey\", \"PayName\": \"RarepidPay\", \"Contract\": \"Contract\"}";
        private string _actualAuth;
        private DepositPost _actualData;
        private string _actualUrl;
        private WithdrawalPost _actualWData;
        private IFlurlHttpHelper _flurlHttpHelper;
        private PayHelper _target;

        [SetUp]
        public void A00_Setup()
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
            var transId = "DD00001";
            var inputs = GenerateInputs(transId);
            var depositResponse = GivenDepositResponse("0", "NO_ERROR");
            GivenHttpHelperPostJsonReturnsDepositResponse(depositResponse);

            var expected = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = "D0000000001",
                Message = "NO_ERROR",
                Url = "https://api.{domain}.com/Api/StartWidget/1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039"
            };
            AssertDebitShouldReturn(inputs, expected);

            var expectedData = new DepositPost
            {
                HttpPostUrl = inputs["CallBackUrl"],
                ReturnUrl = inputs["ReturnUrl"],
                FailedReturnUrl = inputs["ReturnUrl"],
                Depositer = inputs["CustId"],
                Amount = int.Parse(inputs["Amount"]).ToString("F0"),
                TranId = inputs["TransID"],
                Remark = "service"
            };
            AssertHttpHelperPostJsonShouldReceive(
                expectedData,
                "http://localhost:59159/RarepidPay/Api/QRDeposit",
                "Contract ApiKey");
        }

        [Test]
        public void A02_Debit_fail()
        {
            var transId = "DD00001";
            var inputs = GenerateInputs(transId);
            var depositResponse = GivenDepositResponse("306", "ERR_DUPLICATE_ID");
            GivenHttpHelperPostJsonReturnsDepositResponse(depositResponse);

            var expected = new DepositResult
            {
                Success = false,
                ErrorCode = "9000",
                Message = "RarepidPay create order failed: Status=306, StatusMessage=ERR_DUPLICATE_ID",
            };
            AssertDebitShouldReturn(inputs, expected);

            var expectedData = new DepositPost
            {
                HttpPostUrl = inputs["CallBackUrl"],
                ReturnUrl = inputs["ReturnUrl"],
                FailedReturnUrl = inputs["ReturnUrl"],
                Depositer = inputs["CustId"],
                Amount = int.Parse(inputs["Amount"]).ToString("F0"),
                TranId = inputs["TransID"],
                Remark = "service"
            };
            AssertHttpHelperPostJsonShouldReceive(
                expectedData,
                "http://localhost:59159/RarepidPay/Api/QRDeposit",
                "Contract ApiKey");
        }

        [Test]
        public void B01_CallBackDebit_SignSuccess()
        {
            var formData = new Dictionary<string, string>();
            var headers = new Dictionary<string, string>();
            var payResource = new Dictionary<string, string>
            {
                { "ApiKey", "xov5j8a09uea5dvug9q0jw" }
            };
            var bodyContent = @"{
                ""contract"": ""ABCTEST"",
                ""token"": ""1ae3ebc1-41ab4a4e-9ef2-0a34ec05a039"",
                ""tran_id"": ""depositId"",
                ""pg_tran_id"": ""1"",
                ""status"": ""8"",
                ""status_message"": ""NO_ERROR"",
                ""tran_status"": ""2"",
                ""tran_message"": ""Processing"",
                ""decline_reason"": ""reached max login trials"",
                ""remark"": ""nn"",
                ""amount"": ""1000"",
                ""total_fees"": ""23"",
                ""currency"": ""INR"",
                ""depositer"": ""Jon Doe"",
                ""from_bank_code"": ""KOTAK"",
                ""from_bank_account"": ""112233445566"",
                ""bank_reference"": ""bank transaction success reference number"",
                ""signature"": ""0c612678f967c0f130090312e96c63be32990cfea830f19ae9351667a35ea6aa"",
                ""created_at"": ""9/14/2022 3:59:41 PM"",
                ""updated_at"": ""9/14/2022 3:59:41 PM"",
                ""reference"": ""1321""
            }";
            var actual = _target.CallBackDebit(formData, bodyContent, headers, payResource);

            var expected = new NotifyDepositResult
            {
                ThirdId = "1",
                DepositId = "depositId",
                Comment = "8",
                TransStatus = TransStatus.Success,
                ActualAmount = 1000M,
                Response = Definition.CallbackResponse
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void C01_Credit_success()
        {
            var transId = "WD138065";
            var inputs = GenerateWithdrawalInputs(transId);
            var withdrawalResponse = GenerateWithdrawalResponse("0", "NO_ERROR");
            GivenHttpHelperPostJsonReturnsWithdrawalResponse(withdrawalResponse);

            var expected = new WithdrawResult
            {
                Success = true,
                Message = "NO_ERROR"
            };
            AssertCreditSuccessShouldReturn(inputs, expected);

            var expectedData = new WithdrawalPost
            {
                HttpPostUrl = $"http://www.callbackurl.com/Payment/WithdrawalCallBack/{transId}",
                Amount = "1000.00",
                ToBankCode = "AXIS",
                ToBankAccount = "58585858",
                ToAccountName = "Jatinder Mesi",
                IfscCode = "ABCDE012345",
                TranId = transId,
                Remark = "payout"
            };
            AssertHttpHelperPostJsonForWithdrawalShouldReceive(
                expectedData,
                "http://localhost:59159/RarepidPay/Api/Withdraw",
                "Contract ApiKey");
        }

        [Test]
        public void C02_Credit_fail()
        {
            var transId = "WD138065";
            var inputs = GenerateWithdrawalInputs(transId);
            var withdrawalResponse = GenerateWithdrawalResponse("306", "ERR_DUPLICATE_ID");
            GivenHttpHelperPostJsonReturnsWithdrawalResponse(withdrawalResponse);

            var expected = new WithdrawResult
            {
                Success = false,
                Message = "RarepidPay create payout failed: Status=306, StatusMessage=ERR_DUPLICATE_ID"
            };
            AssertCreditSuccessShouldReturn(inputs, expected);

            var expectedData = new WithdrawalPost
            {
                HttpPostUrl = $"http://www.callbackurl.com/Payment/WithdrawalCallBack/{transId}",
                Amount = "1000.00",
                ToBankCode = "AXIS",
                ToBankAccount = "58585858",
                ToAccountName = "Jatinder Mesi",
                IfscCode = "ABCDE012345",
                TranId = transId,
                Remark = "payout"
            };
            AssertHttpHelperPostJsonForWithdrawalShouldReceive(
                expectedData,
                "http://localhost:59159/RarepidPay/Api/Withdraw",
                "Contract ApiKey");
        }

        [Test]
        public void D01_CallBackCredit_SignSuccess()
        {
            var transId = "WD138065";
            var (formData, bodyContent, headers, payResource) =
                GenerateParameters(transId, WithdrawalType.OnlineBanking, CurrencyEnum.INR);

            var expectedNotifyWithdrawResult = new NotifyWithdrawResult
            {
                WithdrawId = transId,
                ThirdId = "5438543",
                TransStatus = TransStatus.Success,
                Response = "OK",
                Message = "NO_ERROR"
            };
            AssertCallBankCreditShouldReturn(formData, bodyContent, headers, payResource, expectedNotifyWithdrawResult);
        }
        private static BankInfoPay GenerateBankInfo()
        {
            var bankInfoPay = new BankInfoPay
            {
                PayId = 92,
                PayCurrencyId = "INR",
                BankCode = "AXIS",
                BankName = "Axis Bank",
                SysId = 244,
                PayCode = string.Empty
            };
            return bankInfoPay;
        }

        private static DepositResponse GivenDepositResponse(string status, string statusMessage)
        {
            return new DepositResponse
            {
                Status = status,
                StatusMessage = statusMessage,
                PgTranId = "D0000000001",
                RedirectTo = "https://api.{domain}.com/Api/StartWidget/1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039"
            };
        }

        private void AssertCallBankCreditShouldReturn(
            Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource,
            NotifyWithdrawResult expectedNotifyWithdrawResult)
        {
            var actual = _target.CallBackCredit(formData, bodyContent, headers, payResource);
            actual.Should().BeEquivalentTo(expectedNotifyWithdrawResult);
        }

        private void AssertCreditSuccessShouldReturn(Dictionary<string, string> inputs, WithdrawResult expected)
        {
            var actual = _target.Credit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }

        private void AssertDebitShouldReturn(Dictionary<string, string> inputs, DepositResult expected)
        {
            var actual = _target.Debit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }

        private void AssertHttpHelperPostJsonForWithdrawalShouldReceive(WithdrawalPost expectedData, string expectedUrl, string expectedAuth)
        {
            _actualWData.Should().BeEquivalentTo(expectedData);
            _actualUrl.Should().Be(expectedUrl);
            _actualAuth.Should().Be(expectedAuth);
        }

        private void AssertHttpHelperPostJsonShouldReceive(DepositPost expectedData, string expectedUrl, string expectedAuth)
        {
            _actualData.Should().BeEquivalentTo(expectedData);
            _actualUrl.Should().Be(expectedUrl);
            _actualAuth.Should().Be(expectedAuth);
        }

        private Dictionary<string, string> GenerateFormData()
        {
            return new Dictionary<string, string>();
        }

        private Dictionary<string, string> GenerateHeaders()
        {
            return new Dictionary<string, string>();
        }

        private Dictionary<string, string> GenerateInputs(string transId)
        {
            var inputs = new Dictionary<string, string>();

            inputs.AddRange(GenerateInputsFromOriginalRequest(transId));

            var depositInfo =
                "{\"Mode\": \"2\", \"PayId\": \"92\", \"SysId\": \"0\", \"GroupId\": \"28\", \"PayCode\": \"\", \"PayName\": \"RarepidPay\", \"ChannelId\": \"772\", \"GroupName\": \"UPI\", \"MaxAmount\": \"50000.0000\", \"MinAmount\": \"500.0000\", \"CurrencyId\": \"61\", \"PayAccount\": null, \"PointLevel\": \"0\", \"ChannelName\": \"RarepidPay\", \"DepositType\": \"MobilePayment\", \"ExtraDataType\": \"0\", \"PayCurrencyId\": \"INR\", \"DisplayGroupName\": \"UPI\", \"DisplayChannelName\": \"RarepidPay\"}";
            inputs.AddRange(GenerateInputsFromDepositInfo(depositInfo));
            inputs.AddRange(GenerateDepositInputsForMobilePay(depositInfo, new BankInfoPay()));

            inputs.AddRange(GenerateDepositInputsFromCustInfo());
            inputs.AddRange(GenerateInputsFromPaySetting(RarepidPaySetting));

            return inputs;
        }

        private (Dictionary<string, string>, string, Dictionary<string, string>, Dictionary<string, string>) GenerateParameters(
            string transId,
            WithdrawalType withdrawalType,
            CurrencyEnum currency)
        {
            var formData = GenerateFormData();
            var bodyContent = GenerateRarepidPayBodyContent();
            var headers = GenerateHeaders();
            var payResource = GeneratePayResource(transId, withdrawalType, currency);

            return (formData, bodyContent, headers, payResource);
        }

        private Dictionary<string, string> GeneratePayResource(string transId, WithdrawalType withdrawalType, CurrencyEnum currency)
        {
            var payResource = GenerateInputsFromPaySetting(RarepidPaySetting);
            payResource.Add("TransId", transId);
            payResource.Add("WithdrawalType", ((int)withdrawalType).ToString());
            payResource.Add("CurrencyId", ((int)currency).ToString());

            return payResource;
        }

        private string GenerateRarepidPayBodyContent()
        {
            return @"{
                ""contract"": ""PACSEA128TEST"",
                ""token"": ""1ae3ebc1-41ab4a4e-9ef2-0a34ec05a039"",
                ""tran_id"": ""WD138065"",
                ""payout_tran_id"": ""5438543"",
                ""status"": ""8"",
                ""status_message"": ""NO_ERROR"",
                ""payout_status"": ""8"",
                ""payout_message"": ""Complete"",
                ""decline_reason"": ""reached max login trials"",
                ""remark"": ""nn"",
                ""amount"": ""1000"",
                ""total_fees"": ""23"",
                ""currency"": ""INR"",
                ""to_bank_code"": ""KOTAK"",
                ""to_bank_account"": ""112233445566"",
                ""to_account_name"": ""112233445566"",
                ""ifsc_code"": ""ICICI12345678"",
                ""phone_number"": ""112233445566"",
                ""signature"": ""95160ce0d8ca4e068dd0331d816b2ed3690168cd4769d4fbdee2483c09764576"",
                ""created_at"": ""2017-01-01 16:41:21"",
                ""payment_date"": ""2017-01-01 16:41:21""
            }";
        }

        private Dictionary<string, string> GenerateWithdrawalInputs(string transId)
        {
            var inputs = new Dictionary<string, string>();
            inputs.AddRange(GenerateWithdrawalInputsFromOriginalRequest(transId));

            var withdrawalInfo = "{\"PayId\": 92, \"SysId\": 244, \"IFSCCode\": \"ABCDE012345\", \"AccountNo\": \"9416030505\", \"ChannelId\": 773, \"AccountName\": \"Jatinder Mesi\"}";
            inputs.AddRange(GenerateInputsFromWithdrawalInfoForOnlineBanking(WithdrawalType.OnlineBanking, withdrawalInfo));

            inputs.AddRange(GenerateWithdrawalInputsFromCustInfo());

            inputs.AddRange(GenerateWithdrawalInputsFromPayInfo(GenerateBankInfo()));
            inputs.AddRange(GenerateInputsFromWithdrawalTypeForOnlineBanking(withdrawalInfo, GenerateBankInfo()));


            inputs.AddRange(GenerateInputsFromPaySetting(RarepidPaySetting));
            return inputs;
        }

        private WithdrawalResponse GenerateWithdrawalResponse(string status, string statusMessage)
        {
            return new WithdrawalResponse
            {
                Status = status,
                StatusMessage = statusMessage
            };
        }

        private void GivenHttpHelperPostJsonReturnsDepositResponse(DepositResponse depositResponse)
        {
            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth))
                .PostJson<DepositPost, DepositResponse>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                    Arg.Do<DepositPost>(actualData => _actualData = actualData))
                .Returns((depositResponse, HttpStatusCode.OK));
        }
        private void GivenHttpHelperPostJsonReturnsWithdrawalResponse(WithdrawalResponse withdrawalResponse)
        {
            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth))
                .PostJson<WithdrawalPost, WithdrawalResponse>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                    Arg.Do<WithdrawalPost>(actualData => _actualWData = actualData))
                .Returns((withdrawalResponse, HttpStatusCode.OK));
        }
    }
}