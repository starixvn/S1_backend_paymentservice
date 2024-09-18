using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth))
                .WithHeader("accept", "application/json")
                .PostJson<string, DepositResponse>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
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
                ThirdId = "depositId",
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
            var withdrawalResponse = GenerateWithdrawalResponse("0", "NO_ERROR", "WD138065");
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
            AssertHttpHelperPostJsonShouldReceive(
                JsonConvert.SerializeObject(expectedData),
                "http://localhost:59159/RarepidPay/Api/Withdraw",
                "Contract ApiKey");
        }

        [Test]
        public void C02_Credit_fail()
        {
            var transId = "WD138065";
            var inputs = GenerateWithdrawalInputs(transId);
            var withdrawalResponse = GenerateWithdrawalResponse("306", "ERR_DUPLICATE_ID", "WD138065");
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
            AssertHttpHelperPostJsonShouldReceive(
                JsonConvert.SerializeObject(expectedData),
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
                ThirdId = "1",
                TransStatus = TransStatus.Success,
                Response = "OK",
                Message = "NO_ERROR"
            };
            AssertCallBankCreditShouldReturn(formData, bodyContent, headers, payResource, expectedNotifyWithdrawResult);
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
            var payResource = GenerateInputsFromPaySetting();
            payResource.Add("TransId", transId);
            payResource.Add("WithdrawalType", ((int)withdrawalType).ToString());
            payResource.Add("CurrencyId", ((int)currency).ToString());

            return payResource;
        }

        private Dictionary<string, string> GenerateHeaders()
        {
            return new Dictionary<string, string>();
        }

        private string GenerateRarepidPayBodyContent()
        {
            return @"{
                ""contract"": ""PACSEA128TEST"",
                ""token"": ""1ae3ebc1-41ab4a4e-9ef2-0a34ec05a039"",
                ""tran_id"": ""depositId"",
                ""payout_tran_id"": ""1"",
                ""status"": ""0"",
                ""status_message"": ""NO_ERROR"",
                ""payout_status"": ""2"",
                ""payout_message"": ""Processing"",
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
                ""signature"": ""26bd1272d38a3a3d34871874d8b9a1d162990ce0f2af505b77db9d90fd61d10d"",
                ""created_at"": ""2017-01-01 16:41:21"",
                ""payment_date"": ""2017-01-01 16:41:21""
            }";
        }

        private Dictionary<string, string> GenerateFormData()
        {
            return new Dictionary<string, string>();
        }

        private void AssertHttpHelperPostJsonShouldReceive(string expectedData, string expectedUrl, string expectedAuth)
        {
            _actualData.Should().Be(expectedData);
            _actualUrl.Should().Be(expectedUrl);
            _actualAuth.Should().Be(expectedAuth);
        }

        private void AssertCreditSuccessShouldReturn(Dictionary<string, string> inputs, WithdrawResult expected)
        {
            var actual = _target.Credit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }

        private void GivenHttpHelperPostJsonReturnsWithdrawalResponse(WithdrawalResponse withdrawalResponse)
        {
            _flurlHttpHelper
                .WithHeader("Authorization", Arg.Do<string>(actualAuth => _actualAuth = actualAuth))
                .WithHeader("accept", "application/json")
                .PostJson<string, WithdrawalResponse>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                    Arg.Do<string>(actualData => _actualData = actualData))
                .Returns((withdrawalResponse, HttpStatusCode.OK));
        }

        private WithdrawalResponse GenerateWithdrawalResponse(string status, string statusMessage, string transId)
        {
            return new WithdrawalResponse
            {
                Status = status,
                StatusMessage =  statusMessage,
                TranId = transId,
                PayoutTranId =  "C00000000013422",
                Token =  "f0375599-077-47aa-b39a-6267a8bca571",
                Remark =  "payout for player PokerGod666",
                Amount =  "1000",
                TotalFees =  "1.15",
                Currency = "INR",
                ToBankCode =  "KOTAK",
                ToBankAccount =  "123455778980",
                IfscCode =  "123455778980",
                PhoneNumber = "123455778980"
            };
        }

        private Dictionary<string, string> GenerateWithdrawalInputs(string transId)
        {
            var inputs = new Dictionary<string, string>();
            inputs.AddRange(GenerateInputsFromCustInfo());
            inputs.AddRange(GenerateInputsFromWithdrawalInfo());
            inputs.AddRange(GenerateInputsFromPayInfo());
            inputs.AddRange(GenerateInputsFromWithdrawalTypeOnlineBanking());
            inputs.AddRange(GenerateInputsFromPaySetting());
            inputs.AddRange(GenerateInputsFromOriginalRequest(transId));
            return inputs;
        }

        private Dictionary<string, string> GenerateInputsFromOriginalRequest(string transId)
        {
            return new Dictionary<string, string>
            {
                { "TransID", transId },
                { "ClientIP", "1.0.0.1" },
                { "OutgoingIp", "1.0.0.1" }, 
                { "CallBackUrl", $"http://www.callbackurl.com/Payment/WithdrawalCallBack/{transId}"},
                { "IsTest", "false" }
            };
        }

        private Dictionary<string, string> GenerateInputsFromPaySetting()
        {
            var pay = new Pay
            {
                Setting =
                    "{\"Host\": \"http://localhost:59159/RarepidPay\", \"ApiKey\": \"ApiKey\", \"PayName\": \"RarepidPay\", \"Contract\": \"Contract\"}"
            };

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(pay.Setting);
        }

        private Dictionary<string, string> GenerateInputsFromWithdrawalTypeOnlineBanking()
        {
            var withdrawalInfo = new WithdrawalRequest
            {
                WithdrawalInfo =
                    "{\"PayId\": 92, \"SysId\": 244, \"IFSCCode\": \"ABCDE012345\", \"AccountNo\": \"9416030505\", \"ChannelId\": 773, \"AccountName\": \"Jatinder Mesi\"}"
            };

            var withdrawalInfoDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(withdrawalInfo.WithdrawalInfo);
            var payInfo = GeneratePayInfo();

            return new Dictionary<string, string>
            {
                { "BankName", payInfo.BankName },
                { "BankCode", payInfo.BankCode },
                { "IFSCCode", withdrawalInfoDictionary["IFSCCode"] }
            };
        }

        private Dictionary<string, string> GenerateInputsFromPayInfo()
        {
            var payInfo = GeneratePayInfo();

            return new Dictionary<string, string>
            {
                { "PayCurrencyId", payInfo.PayCurrencyId },
                { "PayCode", payInfo.PayCode }
            };
        }

        private static BankInfoPay GeneratePayInfo()
        {
            var payInfo = new BankInfoPay
            {
                PayId = 92,
                PayCurrencyId = "INR",
                BankCode = "AXIS",
                BankName = "Axis Bank",
                SysId = 244,
                PayCode = string.Empty
            };
            return payInfo;
        }

        private Dictionary<string, string> GenerateInputsFromWithdrawalInfo()
        {
            var withdrawalInfo = new WithdrawalRequest
            {
                CurrencyId = 61,
                Amount = 1000,
                AccountNo = "58585858",
                WithdrawalInfo =
                    "{\"PayId\": 92, \"SysId\": 244, \"IFSCCode\": \"ABCDE012345\", \"AccountNo\": \"9416030505\", \"ChannelId\": 773, \"AccountName\": \"Jatinder Mesi\"}",
                WithdrawalType = WithdrawalType.OnlineBanking
            };

            return new Dictionary<string, string>
            {
                { "CurrencyId", withdrawalInfo.CurrencyId.ToString() },
                { "Amount", withdrawalInfo.Amount.ToString("G29") },
                { "AccountNo", withdrawalInfo.AccountNo },
                { "WithdrawalInfo", withdrawalInfo.WithdrawalInfo },
                { "WithdrawalType", ((int)withdrawalInfo.WithdrawalType).ToString() }
            };
        }

        private Dictionary<string, string> GenerateInputsFromCustInfo()
        {
            var customer = new Customer
            {
                CustId = 651403,
                UserName = "debbieinr001",
                AccountName = "Jatinder Mesi",
                UserLang = "en",
                PhoneNo = "966521475",
                PhoneVerified = true,
                AreaCode = "+66",
            };

            return new Dictionary<string, string>
            {
                { "CustId", customer.CustId.ToString() },
                { "UserName", customer.UserName },
                { "AccountName", customer.AccountName },
                { "Lang", customer.UserLang },
                { "PhoneNo", customer.PhoneNo },
                { "PhoneVerified", customer.PhoneVerified.ToString() },
                { "AreaCode", customer.AreaCode }
            };
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
            var expectedData = JsonConvert.SerializeObject(new DepositPost
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

        private void AssertDebitShouldReturn(Dictionary<string, string> inputs, DepositResult expected)
        {
            var actual = _target.Debit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}