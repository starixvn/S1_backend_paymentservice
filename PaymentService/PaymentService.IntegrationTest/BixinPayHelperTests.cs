using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Payment.BixinPay;
using PaymentCore.Interface;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class BixinPayHelperTests
    {
        private IFlurlHttpHelper _flurlHttpHelper;
        private string _privateKey;
        private string _publicKey;
        private string _signKey;
        private PayHelper _target;

        [SetUp]
        public void Setup()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var logger = Substitute.For<ILogger<PayHelper>>();
            serviceProvider.GetService<ILogger<PayHelper>>().Returns(logger);

            _flurlHttpHelper = Substitute.For<IFlurlHttpHelper>();
            serviceProvider.GetService<IFlurlHttpHelper>().Returns(_flurlHttpHelper);
            _target = new PayHelper(serviceProvider);

            _signKey = "Key";
            _publicKey =
                "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAj2rxch8MBbqCZNJWfD3bMV4bWvwp4lEGrDei3E1GmkkHoyJ1wO34ahOwEOE2t+sAIb7CQJ/v44eVQPNgst867mBXRQObd0WddlsgtstJYKAeXKzygoivhUJruVgZAYaNcJUYvgSChPHnpeZtBZcdE8nkRy+9SoFNBEHaDlKkoYg528wYlkkv+e5jXcFBAWkQsJ9oXcs4hKXMPzC1cTZI7HZ6Z8nQJo+SyH1ofT6dOHroPgJgHJZAzPNP2+Ydg80GjWNFXYoF1eVDuGjy88+bRgk1Yvdze21UI0iiriZcnh1r1LeFoMZaGqUXlBNyuEToWMTrhq/lPSjLGFhrFG/p2wIDAQAB";
            _privateKey =
                "MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCPavFyHwwFuoJk0lZ8PdsxXhta/CniUQasN6LcTUaaSQejInXA7fhqE7AQ4Ta36wAhvsJAn+/jh5VA82Cy3zruYFdFA5t3RZ12WyC2y0lgoB5crPKCiK+FQmu5WBkBho1wlRi+BIKE8eel5m0Flx0TyeRHL71KgU0EQdoOUqShiDnbzBiWSS/57mNdwUEBaRCwn2hdyziEpcw/MLVxNkjsdnpnydAmj5LIfWh9Pp04eug+AmAclkDM80/b5h2DzQaNY0VdigXV5UO4aPLzz5tGCTVi93N7bVQjSKKuJlyeHWvUt4WgxloapReUE3K4ROhYxOuGr+U9KMsYWGsUb+nbAgMBAAECggEAOcbZkiIT09L5aOib0t2QimjhGFxx+yytRX/8akfvY4R8QSpGyfyUkRdxXLt+McXIfc+9rg9AsycR5+KkWCs6MII7ZiA352ryLpYzrbBj1TtsP+WkpRWvsD277CUjncu4+1v1ZcenVSunKNyVmyTvn+M7xbnjxjok7swgrph4nmIFUrvcpQt3aVRdq97VowYVOkF7O2/46W6c+vLn+n6VFahR56MX/Mfojlv7vu5aQfH0Fwo68TdbT41NTAkpr8wF8pdrIOOk42sA2QXXuy5PiRfIf1fKDHE+XBmB1DZBBIGako2hMzESMofs1oBiLysTIOLhtADBcS+HSxehxecEsQKBgQD4szRJcJiMNRuhPpWyAGhJ3xw6JHpb3/+kmC0AEN1hiTTOGK6/dLv6IJG4KXwtFtHdqV2SP9fbK1IMgB+5nrlSY1eMIDz/L4YF+WUyGmF/6322baqDX7wmUpGHbeChCiV/uqpiMwt5Zx4kPQbGaYHnG3bLU41M89pIIOQMRGdptQKBgQCToJ7znZypOtQgHWGSSFnEKIn5D2qcmHmlKiq3l8jwMIgXZQdnptT/vmQsVUpjGMvA5iWvyjiVdrpUUF4jlIEKflnbyzoU6ZZfFOC5I/5mNuOFtxsp0WMT5NiPJKgeQkgFqNYL+aTfiqWJ4TCK4R+3pTq1TMR9FEwzAWTZFi//TwKBgH0acFftCt+kq3RuLhEvgUZ/tAxPYzmqMUfTwPdChIpW4bg21Fi82P5WudahD7UCwKk3VDmIYgX/AbQGE29lN6YJEPYNFYHsnweMT+pnONqHVGKbQeX6F4RTjp9Il3GK0BgzhxgEw2XbiF8ET88pLc5ak2RucCrgk9IqsWy2UCrtAoGATimN3ziE+MjwBSzpOzVZadV478hszwqf5YYE5f3UYu+fLpvMJuIbBmk8yohCAWPghXqVW5S7Ds+06e0/WqFHDVJGq2vXjbj7Z847CWt4R5AqOOLw29xEFAVnb6qd5J99WJnC8sYzuDPFX9I+DFoTm/08rz89DWiQy7SiO5pWwVsCgYAfUjz1iAoUiHpXHWWNdTfK+TbsN0R78kbXxcz5WGSGFTPMZiN9AqX1hfPW6NYyG/EpiIWlkOfU5BctxbrenLkpCRzQ/xbJ6BdortOJaxRXP4X+m09Rl82lAgJC3TkMGeDBM9jol1W9Lvoz1pKkyOpa1AVSNfL+9QmNwASms+OvPw==";
        }

        [Test]
        public void A01_GetPostSign()
        {
            var parameters = new Dictionary<string, string>
            {
                {"merchno", "11223344"},
                {"orderId", "123456"},
                {"payType", "3"},
                {"amount", "100.00" },
                {"requestCurrency", "1" },
                {"asyncUrl", "http://testBixinPay"},
                {"syncUrl", "http://testBixinPay"},
                {"requestTime", "20210128164612"},
                {"apiVersion", "2"},
                {"attach", "eibbed"}
            };

            var expectSignConcatSourceString = "amount=100.00&apiVersion=2&asyncUrl=http://testBixinPay&attach=eibbed&merchno=11223344&orderId=123456&payType=3&requestCurrency=1&requestTime=20210128164612&syncUrl=http://testBixinPay&secretKey=Key";
            var expectedSignResult = "790706601d6b13b2ca428a8e44ae2ddb";

            AssertGetPostSign(parameters, expectSignConcatSourceString, expectedSignResult);
        }

        [Test]
        public void B01_GetWithdrawalPostSign()
        {
            var parameters = new Dictionary<string, string>
            {
                {"merchno", "4b284a5186"},
                {"orderId", "1566541511369"},
                {"amount", "100.00" },
                {"account", "mayun" },
                {"tradeType", "1" },
                {"cardNo", "12345678901234567" },
                {"bankName", "gongshang" },
                {"depositBank", "hangzhou branch" },
                {"asyncUrl", "http://asyncUrl.com"},
                {"timestamp", "20190823142957"},
                {"attach", "attach"}
            };

            var expectSignConcatSourceString = "account=mayun&amount=100.00&asyncUrl=http://asyncUrl.com&attach=attach&bankName=gongshang&cardNo=12345678901234567&depositBank=hangzhou branch&merchno=4b284a5186&orderId=1566541511369&timestamp=20190823142957&tradeType=1&secretKey=Key";
            var expectedResult = "d9ZFqXWRn7HfjpA7t0w/uHOSvuxJeLBtS4h4SNjR5SHRAmTGSd1qDmVXrLI+2iuV8nXNk9vkA7Pg4vL42IdHes9cW+Y+Z8k2mBAViJWbJ0EbvapPGjLdy/BhDYo7bxrSlXj/dQBP68PN5+zXYoFapHXjzGL16pMFuDgqOMZPfsdGNN/1Vl9ajhg9caChKmN+YmXNBUzPDOZ4jHPMtqmR3lDg9h4HewtnbXXwdQyte1zIFs8ox0WIFNH3a6BTwafdcjQLN7MznWxUwTJX8D/A0VliP2XnOowuXQCYYjdu0LPoyzuDlxljFRWTXhwKtKJ7zlVqanW6z83WdNLFhNjgnQ==";
            AssertGetWithdrawalPostSign(parameters, expectSignConcatSourceString, expectedResult);
        }

        [Test]
        public void C01_VerifyWithdrawalSign()
        {
            var parameters = new Dictionary<string, string>
            {
                {"timestamp", "20190823142957"},
                {"orderNo", "123456789"},
                {"merchno", "4b284a5186"},
                {"orderId", "1566541511369"},
                {"amount", "100.00" },
                {"account", "mayun" },
                {"tradeType", "1" },
                {"cardNo", "12345678901234567" },
                {"bankName", "gongshang" },
                {"depositBank", "hangzhou branch" },
                {"attach", "attach"},
                {"status", "1"},
                {"sign", "DfPJqQstqwSA9/8YHDQZKhoC/X7X/ZnF1ToCVSAJsSivEku58JCYIU5NAtFuc8GrtvxgPDKUhvk14xZYwbnFAkE6tQf3Ift6MjnTn+9iipmx66O/VgKxFez3hXRvytyc4YrTcVzh18aFbB2DSjYrhnoOzhjrVOAWlizrQWQ+W6XI90hdjBoECPZX9GXB7J8tcOhDI8JU0Y3Y2X8X+yFwPcTPQTgCRpfL4Oxd2lAPnhnFJbGZE0cLWChNXXwVTSMh5o9yuoYhzKfeAeFR1FysNUcyEyX44EGQRgpDZkHwmFhEKJ6QbO9iQi6jX1sePYEQ7p2OnfkFMBEQcWiLCmg5sg=="}
            };

            var expectSignConcatSourceString = "account=mayun&amount=100.00&attach=attach&bankName=gongshang&cardNo=12345678901234567&depositBank=hangzhou branch&merchno=4b284a5186&orderId=1566541511369&orderNo=123456789&status=1&timestamp=20190823142957&tradeType=1&secretKey=Key";
            AssertVerifyWithdrawalSign(parameters, expectSignConcatSourceString);
        }


        private void AssertGetPostSign(Dictionary<string, string> testData, string expectSignConcatSourceString, string expectedSignResult)
        {
            var actual = _target.GetPostSign(testData, _signKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedSignResult);
        }

        private void AssertGetWithdrawalPostSign(Dictionary<string, string> parameters, string expectSignConcatSourceString, string expectedResult)
        {
            var actual = _target.GetWithdrawalPostSign(parameters, _signKey, _privateKey);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(expectedResult);
        }

        private void AssertVerifyWithdrawalSign(Dictionary<string, string> parameters, string expectSignConcatSourceString)
        {
            var actual = _target.VerifyWithdrawalSign(parameters, _signKey, _publicKey, parameters["sign"]);
            PayHelper.SignConcatSourceString.Should().Be(expectSignConcatSourceString);
            actual.Should().Be(true);
        }
    }
}