using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.CashRuPay;
using PaymentCore;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;
using PaymentService.Service.Model;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class CashRuPayHelperTests : PayHelperTestBase
    {
        private const string CashRuPaySetting =
            "{\"Host\": \"http://localhost:59159/CashRuPay\", \"AesKey\": \"QL591OUVR7UUISBH\", \"Md5Key\": \"0YCT2PE2EVFRXK0HE6WRNE6LE3C8ARHC\", \"PayName\": \"CashRuPay\", \"MerchantCode\": \"87829322\"}";

        private const string DepositInfo =
            "{\"Mode\": \"2\", \"PayId\": \"94\", \"SysId\": \"0\", \"GroupId\": \"28\", \"PayCode\": \"0\", \"PayName\": \"CashRuPay\", \"ChannelId\": \"778\", \"GroupName\": \"UPI\", \"MaxAmount\": \"50000.0000\", \"MinAmount\": \"500.0000\", \"CurrencyId\": \"61\", \"PayAccount\": null, \"PointLevel\": \"0\", \"ChannelName\": \"CashRuPay\", \"DepositType\": \"MobilePayment\", \"ExtraDataType\": \"0\", \"PayCurrencyId\": \"\", \"DisplayGroupName\": \"UPI\", \"DisplayChannelName\": \"CashRuPay\"}";

        private CashRuPayPost _actualData;
        private string _actualUrl;
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
            _target.CurrentTimeStamp = "1692247923";
        }

        [Test]
        public void A01_Debit_success()
        {
            var transId = "DEP1323442232222";
            var inputs = GenerateInputs(transId);


            var depositResponse = GenerateDepositResponse("0", "success");
            GivenHttpHelperPostJsonReturnsDepositResponse(depositResponse);

            var expected = new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = "2023101211343626342",
                Url = "https://www.cashrupay.com/index/index?orderId=dd18382993263421"
            };
            AssertDebitShouldReturn(inputs, expected);
            var expectedSignConcatSourceString = "878293221692247923";
            AssertSignConcatSourceString(expectedSignConcatSourceString);
            var expectedUrl = "http://localhost:59159/CashRuPay/web/createOnlineOrder";
            var expectedPostData = new CashRuPayPost
            {
                AgentCode = "87829322",
                TimeStamp = "1692247923",
                Data =
                    "P5UQTjGAVLgU1p6TDyC3aaT4qBUyAtDX1of1O0eFV69krjtdJ3/ysk4ph8dmjSmqEAoNz2XnGfQrGNpjiT8hAsIkIchh/VE44pJWKUSKVoPP8T755jRa8gLWDgX6IvVHDA/v8MgA6MuBoM+W9EWq8P7Kj2RqjxurNnDQQzejBCuBc78PToS+9R8iuKV0xT9eqZVLd6NQ31XhlqntD1pqNPLlzI1+i/LO/vEllLGZoa4jMsRkhnCSkflb7DFFa485",
                Sign = "1510eaa27bd9c94f60d7f22bc957df8e"
            };
            AssertHttpHelperPostJsonShouldReceive(expectedUrl, expectedPostData);
        }

        [Test]
        public void A02_Debit_fail()
        {
            var transId = "DEP1323442232222";
            var inputs = GenerateInputs(transId);

            var depositResponse = GenerateDepositResponse("500", "Failed");
            GivenHttpHelperPostJsonReturnsDepositResponse(depositResponse);

            var expected = new DepositResult
            {
                Success = false,
                ErrorCode = "9000",
                Message = "CashRuPay CreateOrder response error: Code=500, Msg=Failed"
            };
            AssertDebitShouldReturn(inputs, expected);
            var expectedSignConcatSourceString = "878293221692247923";
            AssertSignConcatSourceString(expectedSignConcatSourceString);
            AssertHttpHelperPostJsonShouldReceive("http://localhost:59159/CashRuPay/web/createOnlineOrder", new CashRuPayPost
            {
                AgentCode = "87829322",
                TimeStamp = "1692247923",
                Data =
                    "P5UQTjGAVLgU1p6TDyC3aaT4qBUyAtDX1of1O0eFV69krjtdJ3/ysk4ph8dmjSmqEAoNz2XnGfQrGNpjiT8hAsIkIchh/VE44pJWKUSKVoPP8T755jRa8gLWDgX6IvVHDA/v8MgA6MuBoM+W9EWq8P7Kj2RqjxurNnDQQzejBCuBc78PToS+9R8iuKV0xT9eqZVLd6NQ31XhlqntD1pqNPLlzI1+i/LO/vEllLGZoa4jMsRkhnCSkflb7DFFa485",
                Sign = "1510eaa27bd9c94f60d7f22bc957df8e"
            });
        }

        [Test]
        public void B01_EncryptDepositPostData()
        {
            var aesKey = "QL591OUVR7UUISBH";
            var data =
                "{\"orderno\":\"DEP1323442232222\",\"payamount\":400,\"callurl\":\"http://127.0.0.1/api/deposit/callback\",\"trscode\":1}";
            var actual = _target.GetEncryptData(data, aesKey);
            actual.Should()
                .Be(
                    "P5UQTjGAVLgU1p6TDyC3aaT4qBUyAtDX1of1O0eFV6+jprNjQG3kQqMc0LO9RZozOtiQRA4+VRFsCJxmYWhIfjGvx39Y0XQzYCt64s4Awyf4xFXCemvLUbxGJw7kRQVTh1W6rl05ZPPjBQQziRIyHA==");
        }

        [Test]
        [TestCase("1", TransStatus.Success)]
        [TestCase("0", TransStatus.Fail)]
        public void C01_CallBackDebit(string receiveStatus, TransStatus transStatus)
        {
            var receiveFormData = new Dictionary<string, string>
            {
                { "status", receiveStatus },
                { "customerid", "87829322" },
                { "sdorderno", "DD12345" },
                { "total_fee", "1200.00" },
                { "paytime", "1692247923" },
                { "sign", "46662bb40b8a79006b8809f880d291bc" }
            };
            var (formData, bodyContent, headers, payResource) = GenerateCallBackDebitParameters(receiveFormData);

            var expected = new NotifyDepositResult
            {
                DepositId = "DD12345",
                ThirdId = "DD12345",
                Comment = receiveStatus,
                TransStatus = transStatus,
                ActualAmount = 1200M,
                Response = "success"
            };
            AssertCallBackDebitShouldReturn(formData, bodyContent, headers, payResource, expected);
        }

        [Test]
        public void C02_CallBackDebit_SignFail()
        {
            var receiveFormData = new Dictionary<string, string>
            {
                { "status", "1" },
                { "customerid", "87829322" },
                { "sdorderno", "DD12345" },
                { "total_fee", "1200.00" },
                { "paytime", "1692247923" },
                { "sign", "xxxxxxx" }
            };
            var (formData, bodyContent, headers, payResource) = GenerateCallBackDebitParameters(receiveFormData);

            var expected = new NotifyDepositResult
            {
                DepositId = "DD12345",
                TransStatus = TransStatus.Pending,
                Response = "Signature verification failed."
            };

            AssertCallBackDebitShouldReturn(formData, bodyContent, headers, payResource, expected);
        }

        [Test]
        public void D01_VerifySign()
        {
            var md5Key = "0YCT2PE2EVFRXK0HE6WRNE6LE3C8ARHC";
            var receive = new CallBackRequest
            {
                Status = "1",
                CustomerId = "87829322",
                sdOrderNo = "DD12345",
                TotalFee = "1200.00",
                PayTime = "1692247923",
                Sign = "46662bb40b8a79006b8809f880d291bc"
            };

            AssertVerifySignShouldBeTrue(receive, md5Key);
        }

        [Test]
        public void E01_Credit_Success()
        {
            var transId = "WD138065";
            var inputs = GenerateWithdrawalInputs(transId);
            var withdrawalResponse = GenerateWithdrawalResponse("0", "success", transId);
            GivenHttpHelperPostJsonReturnsWithdrawalResponse(withdrawalResponse);

            var expected = new WithdrawResult
            {
                Success = true,
                WithdrawId = transId,
                Message = "success"
            };
            AssertCreditShouldReturn(inputs, expected);

            var expectedSignConcatSourceString = "878293221692247923";
            AssertSignConcatSourceString(expectedSignConcatSourceString);

            var expectedUrl = "http://localhost:59159/CashRuPay/web/createOnlineOutOrder";
            var expectedPostData = new CashRuPayPost{
                AgentCode = "87829322",
                TimeStamp = "1692247923",
                Data = "6j3HenDkowUtwq2aSolsRV7cFaqaKRSABGCV+Xjf6x6auOjqC+eIXD9MziGoCjb/eQ/0j1iXdcugaY2mrXrNp6nGClwyCje0bbb9RWQlBJpAljAKMGbQHpGA8jzZJa4BToateGU0HMU4QTz6d7P3z8n1dkUpF6EsY/6eMSulIPL9V4KXHCqdpLanePkmLBgSfaoqIhZcplP4hT1tjp2n/5WRFFXE1Gc92PMZ/3KcTW9bEAaT5lmkIBExoXS8wc+Iy3y7ChLVHsj7Tru6a6mEHA==",
                Sign = "1510eaa27bd9c94f60d7f22bc957df8e"
            };
            AssertHttpHelperPostJsonShouldReceive(expectedUrl, expectedPostData);
        }

        [Test]
        public void E02_Credit_Fail()
        {
            var transId = "WD138065";
            var inputs = GenerateWithdrawalInputs(transId);
            var withdrawalResponse = GenerateWithdrawalResponse("500", "Failed", transId);
            GivenHttpHelperPostJsonReturnsWithdrawalResponse(withdrawalResponse);

            var expected = new WithdrawResult
            {
                Success = false,
                Message = "CashRuPay CreatePayout response error: Code=500, Msg=Failed"
            };
            AssertCreditShouldReturn(inputs, expected);

            var expectedSignConcatSourceString = "878293221692247923";
            AssertSignConcatSourceString(expectedSignConcatSourceString);

            var expectedUrl = "http://localhost:59159/CashRuPay/web/createOnlineOutOrder";
            var expectedPostData = new CashRuPayPost
            {
                AgentCode = "87829322",
                TimeStamp = "1692247923",
                Data = "6j3HenDkowUtwq2aSolsRV7cFaqaKRSABGCV+Xjf6x6auOjqC+eIXD9MziGoCjb/eQ/0j1iXdcugaY2mrXrNp6nGClwyCje0bbb9RWQlBJpAljAKMGbQHpGA8jzZJa4BToateGU0HMU4QTz6d7P3z8n1dkUpF6EsY/6eMSulIPL9V4KXHCqdpLanePkmLBgSfaoqIhZcplP4hT1tjp2n/5WRFFXE1Gc92PMZ/3KcTW9bEAaT5lmkIBExoXS8wc+Iy3y7ChLVHsj7Tru6a6mEHA==",
                Sign = "1510eaa27bd9c94f60d7f22bc957df8e"
            };
            AssertHttpHelperPostJsonShouldReceive(expectedUrl, expectedPostData);
        }

        [Test]
        [TestCase("1",TransStatus.Success)]
        [TestCase("0",TransStatus.Fail)]
        [TestCase("xxxx",TransStatus.Pending)]
        public void F01_CallBackCredit_Success(string receiveStatus, TransStatus transStatus)
        {
            var receiveFormData = new Dictionary<string, string>
            {
                { "status", receiveStatus },
                { "customerid", "87829322" },
                { "sdorderno", "WD12345" },
                { "total_fee", "1200.00" },
                { "paytime", "1692247923" },
                { "sign", "f6031edbff1f807684a3305f9bec1be4" }
            };
            var (formData, bodyContent, headers, payResource) = GenerateCallBackCreditParameters(receiveFormData);

            var expected = new NotifyWithdrawResult
            {
                WithdrawId = "WD12345",
                ThirdId = "WD12345",
                TransStatus = transStatus,
                Response = "success",
                Message = receiveStatus
            };
            AssertCallBackCreditShouldReturn(formData, bodyContent, headers, payResource, expected);
        }

        [Test]
        public void F02_CallBackCredit_SignFail()
        {
            var receiveFormData = new Dictionary<string, string>
            {
                { "status", "1" },
                { "customerid", "87829322" },
                { "sdorderno", "WD12345" },
                { "total_fee", "1200.00" },
                { "paytime", "1692247923" },
                { "sign", "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" }
            };
            var (formData, bodyContent, headers, payResource) = GenerateCallBackCreditParameters(receiveFormData);

            var expected = new NotifyWithdrawResult
            {
                WithdrawId = "WD12345",
                TransStatus = TransStatus.Pending,
                Response = "Signature verification failed."
            };
            AssertCallBackCreditShouldReturn(formData, bodyContent, headers, payResource, expected);
        }

        private (Dictionary<string, string> formData,
            string bodyContent,
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource) 
            GenerateCallBackCreditParameters(Dictionary<string, string> formData)
        {
            var bodyContent = string.Empty;
            var headers = new Dictionary<string, string>();
            var withdrawalRequest = new WithdrawalRequest
            {
                TransId = "DD12345",
                WithdrawalInfo = DepositInfo,
                WithdrawalType = WithdrawalType.OnlineBanking,
                Amount = 1200,
                CurrencyId = 61
            };
            var payResource = GenerateCreditCallBackParametersFromPayResource(CashRuPaySetting, withdrawalRequest);
            return (formData, bodyContent, headers, payResource);
        }

        private void AssertCallBackCreditShouldReturn(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource, NotifyWithdrawResult expected)
        {
            var actual = _target.CallBackCredit(formData, bodyContent, headers, payResource);
            actual.Should().BeEquivalentTo(expected);
        }

        private void AssertCreditShouldReturn(Dictionary<string, string> inputs, WithdrawResult expected)
        {
            var actual = _target.Credit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }

        private void GivenHttpHelperPostJsonReturnsWithdrawalResponse(Response<CreateWithdrawal> withdrawalResponse)
        {
            _flurlHttpHelper
                .PostJson<CashRuPayPost, Response<CreateWithdrawal>>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                    Arg.Do<CashRuPayPost>(actualData => _actualData = actualData))
                .Returns((withdrawalResponse, HttpStatusCode.OK));
        }

        private Response<CreateWithdrawal> GenerateWithdrawalResponse(string code, string message, string withdrawalId)
        {
            return new Response<CreateWithdrawal>
            {
                Code = code,
                Msg = message,
                Data = new CreateWithdrawal
                {
                    Id = "12345",
                    OrderNo = withdrawalId
                }
            };
        }

        private Dictionary<string, string> GenerateWithdrawalInputs(string transId)
        {
            var inputs = new Dictionary<string, string>();
            inputs.AddRange(GenerateWithdrawalInputsFromOriginalRequest(transId));

            var withdrawalInfo = "{\"PayId\": 94, \"SysId\": 244, \"IFSCCode\": \"ABCDE012345\", \"AccountNo\": \"9416030505\", \"ChannelId\": 779, \"AccountName\": \"Jatinder Mesi\"}";
            inputs.AddRange(GenerateInputsFromWithdrawalInfoForOnlineBanking(WithdrawalType.OnlineBanking, withdrawalInfo));

            inputs.AddRange(GenerateWithdrawalInputsFromCustInfo());

            inputs.AddRange(GenerateWithdrawalInputsFromPayInfo(GenerateBankInfo()));
            inputs.AddRange(GenerateInputsFromWithdrawalTypeForOnlineBanking(withdrawalInfo, GenerateBankInfo()));


            inputs.AddRange(GenerateInputsFromPaySetting(CashRuPaySetting));
            return inputs;
        }

        private static BankInfoPay GenerateBankInfo()
        {
            var bankInfoPay = new BankInfoPay
            {
                PayId = 94,
                PayCurrencyId = "INR",
                BankCode = "AXIS",
                BankName = "Axis Bank",
                SysId = 244,
                PayCode = string.Empty
            };
            return bankInfoPay;
        }

        private void AssertVerifySignShouldBeTrue(CallBackRequest receive, string md5Key)
        {
            var expectedSignConcatSourceString = $"{receive.sdOrderNo}{receive.PayTime}";
            var actual = _target.VerifySign(receive.Sign, md5Key);
            actual.Should().BeTrue();
            AssertSignConcatSourceString(expectedSignConcatSourceString);
        }

        private void AssertCallBackDebitShouldReturn(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
            Dictionary<string, string> payResource, NotifyDepositResult expected)
        {
            var actual = _target.CallBackDebit(formData, bodyContent, headers, payResource);
            actual.Should().BeEquivalentTo(expected);
        }

        private (Dictionary<string, string> formData, 
            string bodyContent, 
            Dictionary<string, string> headers,
            Dictionary<string, string> payResource) 
            GenerateCallBackDebitParameters(Dictionary<string, string> formData)
        {
            var bodyContent = string.Empty;
            var headers = new Dictionary<string, string>();
            var depositRequest = new DepositRequest
            {
                TransId = "DD12345",
                DepositInfo = DepositInfo,
                DepositType = DepositType.MobilePayment,
                Amount = 1200,
                CurrencyId = 61
            };
            var payResource = GenerateCallBackParametersFromPayResource(CashRuPaySetting, depositRequest);
            return (formData, bodyContent, headers, payResource);
        }

        private void AssertSignConcatSourceString(string expected)
        {
            PayHelper.SignConcatSourceString.Should().Be(expected);
        }

        private void AssertHttpHelperPostJsonShouldReceive(string expectedUrl, CashRuPayPost expectedPostData)
        {
            _actualUrl.Should().Be(expectedUrl);
            _actualData.Should().BeEquivalentTo(expectedPostData);
        }


        private Dictionary<string, string> GenerateInputs(string transId)
        {
            var inputs = new Dictionary<string, string>();

            inputs.AddRange(GenerateInputsFromOriginalRequest(transId));
            inputs.AddRange(GenerateInputsFromPaySetting(CashRuPaySetting));

            inputs.AddRange(GenerateInputsFromDepositInfo(DepositInfo));
            inputs.AddRange(GenerateDepositInputsForMobilePay(DepositInfo, new BankInfoPay()));

            inputs.AddRange(GenerateInputsFromPayInfo(94, "", "0"));
            inputs.AddRange(GenerateDepositInputsFromCustInfo());

            return inputs;
        }

        private static Response<CreateDeposit> GenerateDepositResponse(string code, string msg)
        {
            return new Response<CreateDeposit>
            {
                Code = code,
                Msg = msg,
                Data = new CreateDeposit
                {
                    OrderNo = "1324232451232",
                    Id = "2023101211343626342",
                    PayAmount = "400",
                    Url = "https://www.cashrupay.com/index/index?orderId=dd18382993263421"
                }
            };
        }

        private void AssertDebitShouldReturn(Dictionary<string, string> inputs, DepositResult expected)
        {
            var actual = _target.Debit(inputs);
            actual.Should().BeEquivalentTo(expected);
        }

        private void GivenHttpHelperPostJsonReturnsDepositResponse(Response<CreateDeposit> response)
        {
            _flurlHttpHelper
                .PostJson<CashRuPayPost, Response<CreateDeposit>>(
                    Arg.Do<string>(actualUrl => _actualUrl = actualUrl),
                    Arg.Do<CashRuPayPost>(actualData => _actualData = actualData))
                .Returns((response, HttpStatusCode.OK));
        }
    }
}