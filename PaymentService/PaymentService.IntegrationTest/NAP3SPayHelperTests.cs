using NUnit.Framework;
using Payment.NAP3SPay;
using NSubstitute;
using PaymentCore;
using PaymentCore.Models.Result;

namespace PaymentService.IntegrationTest
{
    [TestFixture()]
    public class NAP3SPayHelperTests
    {
        private PayHelper _payHelper;
        private Dictionary<string, string> _inputs;

        [SetUp]
        public void Setup()
        {
            //Arrange           
            _payHelper = new PayHelper();

            _inputs = new Dictionary<string, string>
            {
                { "key", "e3cd4369-3585-41ad-b2b8-944bc99eb19b" },
                { "host", "https://n3s172.banglangtim.net/api/recharge" },
            };
        }

    
        [TestCase("094581000110649", "626505814575", "Mobi", 50000,ExpectedResult = true)]
        [TestCase("094581000110650", "189864065110", "Mobi", 50000, ExpectedResult = true)]
        [TestCase("20000147501695", "624480651839201", "VT", 50000, ExpectedResult = true)]
        [TestCase("20000147504104", "626608369258630", "VT", 50000, ExpectedResult = true)]
        [TestCase("59000018335292", "58361564714690", "Vina", 50000, ExpectedResult = true)]
        public bool DebitTest(string serial, string code, string payCode, decimal amount)
        {
            _payHelper = Substitute.For<PayHelper>();
            _payHelper.Debit(Arg.Any<Dictionary<string, string>>()).Returns(new DepositResult() { Success = true });

            _inputs.Add("Serial", serial);
            _inputs.Add("Code", code);
            _inputs.Add("PayCode", payCode);
            _inputs.Add("TransID", Guid.NewGuid().ToString());
            _inputs.Add("Amount", amount.ToString());
            var result = _payHelper.Debit(_inputs);
            return result.Success; 
        }


    }
}