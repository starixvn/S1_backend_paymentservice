using FluentAssertions;
using NUnit.Framework;
using Payment.OneWallet;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class OneWalletHelperTest
    {
        private string _key;
        private SubOneWalletHelperPayHelper _target;

        [SetUp]
        public void Setup()
        {
            _target = new SubOneWalletHelperPayHelper();
            _key = "kV6W9c31tL2XuK1MALP/rw==";
        }

        [Test]
        public void A01_Encrypt_Debit()
        {
            var parameters = new DepositPost
            {
                ShOrderNo = "202406261022",
                BankCode = "TEST_HGY",
                OrderAmount = 100,
                OrderType = "100",
                NotifyUrl = "https://example.com/deposit",
                Note = "note"
            };

            var actual = _target.Encrypt(parameters, _key);
            var expected =
                "eyJpdiI6IjJ1MkxkcXgxOVNyZ0FYU1FyMjRMY3c9PSIsInZhbHVlIjoiOGV5UFFwZ2dpMmVsNWhmdkluU3c2a1RTekNpV25IQkp5d29uNUFxRjB6UG8vZXBqbjgydklOc0hKSit2RXk4OTdTcXlvT2tFS0l4RC9iWFZUV3JPUC9EV05FOWNZVXRGKzAyNk80TGZYcXJheTUycll6U1M1Y0haRUdpcEhSTjhtMWxEZGVsU3kxZTZLcFlIem1nL1ArYmZwejFOdTR6aWJzYlV0aVJ6TjdlbHN6TDNMcndpZElKdDRwWkpxUFVpd0RQZ0s2bVRXOXErRnAvSk1Qc1FRUT09In0=";
            actual.Should().Be(expected);
        }

        [Test]
        public void A02_Encrypt_Credit()
        {
            var parameters = new WithdrawalPost
            {
                ShOrderNo = "2TX5d8dc37804727",
                BankCode = "BBL",
                BankAccount = "123456999",
                BankUserName = "JoJo",
                OrderAmount = 100,
                NotifyUrl = "https://example.com/callback",
                OperateType = "AUTO",
                Note = "note"
            };

            var actual = _target.Encrypt(parameters, _key);
            var expected =
                "eyJpdiI6IjJ1MkxkcXgxOVNyZ0FYU1FyMjRMY3c9PSIsInZhbHVlIjoiOGV5UFFwZ2dpMmVsNWhmdkluU3c2b3p3aUxaRDc2a210NWZYa2k0eDhpRGZDaTYxYVphZDlHeFpjQTlSc3E0U1dsMGR0Y21CcllTQmRDbnlpNEdONTBwY2xhOHZoanpaY2Q4Lzd3TlBTODVJOW5sVmw0ZytHTStsT0EyaDZtK0ZRMkFlSndRZUY5Q2orT0lxeGVvcklJSWFmYWVQem1Sd1M5S0pPb0kvTmkwYnQwMjg1emFFRVJUdlc4Mk43RGNiNWZ6T0UycFBGQ1JNNEpYV1lPYmJCUksycWxHZjhOeEsrL3hFdmJ4NFZjRHNJOXlXODRINXRoanNSQi84YTFQNDdYd3hwOEJ4SHcvb2Uzbjg1ZnduWVE9PSJ9";
            actual.Should().Be(expected);
        }

        [Test]
        public void B01_TryDecrypt_DebitResponse()
        {
            var rawString =
                "eyJpdiI6IjJ1MkxkcXgxOVNyZ0FYU1FyMjRMY3c9PSIsInZhbHVlIjoiVHEya3ZGOEd3VndQMVkxMDdBbDQzdlJHdU5RalBSL1I2RURhN29ZV2RvS3pTUDlYdkE2aXVJUGFSSGlNcGpkUHVFRldSdElVZGxDbDFrUmdYNGdNdjczdUptZ0k0ZzllaTA1eFdVekxXdkNkOXpyczJNSmh3NzFVL3dSaTRoMGdqTGJiejBpMHg0YktyMzA0Um5PZkNrN1Fqc3QvakZ3dWhFSE9pSlgxbEdINUxxR3pRY1FsYmkyZy9mR3dhQ3hRY2ovcVBXWW1HNDh1SGNMVEt4TDZ4OVp6bnB5MElkcXhZRUZ5cjlBWGxhTUI4ZU9vK2JtT3VpMXYxMHBaSlV3SytNak91QWNwOG9mRTB3UlVXZFFzM2NubUppZWUzcFJ4NjZjWXMxMEJBc09NS0FVVVEySFNTMlNpVGFoQVR3SmVKdDEvU2dmb3hndzAzSkh5UFZ2RVpCdFlTekFaWHcrNHJMTzMrQlJZek5ST3NuUi9seHZ6M3NoK053UXNmM2IyUE5Pbi9wdWdxZXMrVHJoTmd2NmhRNVhvTitsUFJwaUlsQU1vZ2h3ZENVNkVMemhwNTF0QTJvRWZHeTZnMERWNktVaVdVdlMwOFJiY1pPMkdkcngvT2RrLzBQbVEwOGdPZEY4MHRZTWp4RUtVdkx0TVYyanNWeFliMzFVMHNMREIifQ==";

            var expectedDecryptString =
                "{\"error_code\":\"200\",\"data\":{\"order_id\":\"123456\",\"order_amount\":\"100\",\"request_amount\":\"100\",\"url\":[\"https://deposit1.com?token=1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN\",\"https://deposit2.com?token=1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN\",\"https://deposit3.com?token=1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN\"],\"token\":\"1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN\",\"message\":null}}";
            var expectedReceiveModel = new DepositResponseData
            {
                OrderId = "123456",
                OrderAmount = "100",
                RequestAmount = "100",
                Url = new []
                {
                    "https://deposit1.com?token=1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN",
                    "https://deposit2.com?token=1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN",
                    "https://deposit3.com?token=1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN"
                },
                Token = "1AYo99S4vymMeAWHsvWeWGH7XKblhL8tuDYlfhAN"
            };
            var expectedErrorMessage = "";

            var (actualDecryptSuccess, actualDecryptString, actualReceiveModel, actualErrorMessage) =
                _target.TryDecrypt<DepositResponse>(rawString, _key);

            actualDecryptSuccess.Should().BeTrue();
            actualDecryptString.Should().Be(expectedDecryptString);
            actualReceiveModel.Data.Should().BeEquivalentTo(expectedReceiveModel);
            actualErrorMessage.Should().Be(expectedErrorMessage);
        }

        [Test]
        public void B02_TryDecrypt_DebitReceive()
        {
            var rawString =
                "eyJpdiI6InhIUUFwVXg2Q3hnSTZNcVUvR3RvZkE9PSIsInZhbHVlIjoiQXRUSEUxb25HRUQ2VUdKVG84bWxwT2I1cWdYc0JWVjgySkoxcTE2SndVUHl6aFIvOWdrZy9aSG9Lbk90bnVaUElVOGpnNEVoaTJhWEprNElaWUhoeEZjeFhJUWhhRUh0RE1zUDFUNExvaW9MdDhUYmV3djVCaEtaTFRFMHdaMThOTkRlWDhVTkM4ZkFSVTJVMzdTMld3PT0ifQ==";

            var expectedDecryptString =
                "{\"sh_order_no\":\"DD1413217\",\"order_status\":\"3\",\"order_amount\":\"500\",\"paid_amount\":\"500\",\"request_amount\":\"500\"}";
            var expectedReceiveModel = new DepositCallBackRequest
            {
                ShOrderNo = "DD1413217",
                OrderStatus = "3",
                OrderAmount = "500",
                RequestAmount = "500",
                PaidAmount = "500"
            };
            var expectedErrorMessage = "";

            var (actualDecryptSuccess, actualDecryptString, actualReceiveModel, actualErrorMessage) =
                _target.TryDecrypt<DepositCallBackRequest>(rawString, _key);

            actualDecryptSuccess.Should().BeTrue();
            actualDecryptString.Should().Be(expectedDecryptString);
            actualReceiveModel.Should().BeEquivalentTo(expectedReceiveModel);
            actualErrorMessage.Should().Be(expectedErrorMessage);
        }

        [Test]
        public void B03_TryDecrypt_CreditResponse()
        {
            var rawString =
                "eyJpdiI6Ikh6Sk5nUU9aQlRqalVMbk5PSnE0eGc9PSIsInZhbHVlIjoidE8yVXM0VXFBODhkdlZiZmJncG1LOVRsamRXVG9FNXQzUStzVndSbFYxb2JJYlhVL0ZwalI4dlNZT0pEdkR6Q0NUYU5BRk9JNDE0Z0tKM3ZMcXlqeGZ2V0F6TkJlUWJsNFNEeTF2TTA1KzU2NmNTV3JWZ2ZzWVF4Z3YxZk1PNkYwa0YzSVVVNStlN1FQUm90aTVVM2x3PT0ifQ==";

            var expectedDecryptString =
                "{\"data\":{\"message\":null,\"order_id\":\"W-0103201000001\",\"sh_order_no\":\"2TX5d8dc37804727\"},\"error_code\":\"200\"}";
            var expectedReceiveModel = new WithdrawalResponseData
            {
                OrderId = "W-0103201000001",
                ShOrderNo = "2TX5d8dc37804727"
            };
            var expectedErrorMessage = "";

            var (actualDecryptSuccess, actualDecryptString, actualReceiveModel, actualErrorMessage) =
                _target.TryDecrypt<WithdrawalResponse>(rawString, _key);

            actualDecryptSuccess.Should().BeTrue();
            actualDecryptString.Should().Be(expectedDecryptString);
            actualReceiveModel.Data.Should().BeEquivalentTo(expectedReceiveModel);
            actualErrorMessage.Should().Be(expectedErrorMessage);
        }

        [Test]
        public void B04_TryDecrypt_CreditReceive()
        {
            var rawString =
                "eyJpdiI6Im1uWTNDVkZraHhlZ0puYkRtSWVRSHc9PSIsInZhbHVlIjoiT2N1MUVLVGNKZnJIZWFrQW5ZMVVpaHlqVUM3ZHhSQWRTTSsxZGxERTlrNDRWV3BJdHlBR3NVcXltNFUrWXdUUm15U2llMEFwL25xcWF4b3dWbmpCbVBBODgvQlNaT1kvS3U2T2lCM2ZpVkpxOU5ndGUwcUJYWktBdGk5ZGpzNHQifQ==";

            var expectedDecryptString =
                "{\"order_amount\":\"505.00\",\"order_status\":\"3\",\"paid_amount\":\"505.00\",\"sh_order_no\":\"622971\"}";
            var expectedReceiveModel = new WithdrawalCallBackRequest
            {
                ShOrderNo = "622971",
                OrderStatus = "3",
                OrderAmount = "505.00",
                PaidAmount = "505.00"
            };
            var expectedErrorMessage = "";

            var (actualDecryptSuccess, actualDecryptString, actualReceiveModel, actualErrorMessage) =
                _target.TryDecrypt<WithdrawalCallBackRequest>(rawString, _key);

            actualDecryptSuccess.Should().BeTrue();
            actualDecryptString.Should().Be(expectedDecryptString);
            actualReceiveModel.Should().BeEquivalentTo(expectedReceiveModel);
            actualErrorMessage.Should().Be(expectedErrorMessage);
        }
    }

    internal class SubOneWalletHelperPayHelper : PayHelper
    {
        internal override string GetRandomBase64String()
        {
            return "2u2Ldqx19SrgAXSQr24Lcw==";
        }
    }
}