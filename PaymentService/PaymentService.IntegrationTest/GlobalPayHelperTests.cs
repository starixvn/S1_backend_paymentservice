using NUnit.Framework;
using Payment.GlobalPay;

namespace PaymentService.IntegrationTest
{
    [TestFixture()]
    public class GlobalPayHelperTests
    {
        private PayHelper _payHelper;
        private Dictionary<string, string> _inputs;

        [TestCase("10000000", "DP123456843", ExpectedResult = true)]
        public bool DebitTest(string amount, string transID)
        {
            _payHelper = new PayHelper();
            //_payHelper = Substitute.For<PayHelper>();
            //_payHelper.Debit(Arg.Any<Dictionary<string, string>>()).Returns(new DepositResult() { Success = true });
            _inputs = new Dictionary<string, string>();
            //_inputs.Add("PayCurrencyId", payCurrencyId);
            //_inputs.Add("THBUser", user);
            //_inputs.Add("THBAPI", apiKey);
            //_inputs.Add("host", "https://liveapi.fpay.support/");
            //_inputs.Add("Amount", "100");
            //_inputs.Add("TransID", "DP123456789
            _inputs.Add("host", "http://35.187.218.91/");
            _inputs.Add("key", "KncN2ResvmFNU2Yvv0t0L67EMf6LRE");
            _inputs.Add("alliesNo", "IG20230425008");
            _inputs.Add("clientId", "92Hmllu8deHStPTmEiA2SmX277IUnBXjJL2VHWjd1rI=");
            _inputs.Add("TransID", transID);
            _inputs.Add("Amount", amount);
            _inputs.Add("ReturnUrl", "https://stag.goal123.com/vn/");
            _inputs.Add("CallBackUrl", $"http://payment.n88dev.com:59159/Payment/DepositCallBack/{transID}");
            _inputs.Add("PayCode", "90-909");
            var result = _payHelper.Debit(_inputs);
            return result.Success;
        }

        //[TestCase("10000000", "WD136735", ExpectedResult = true)]
        public bool CreditTest(string amount, string transID)
        {
            _payHelper = new PayHelper();
            //_payHelper = Substitute.For<PayHelper>();
            //_payHelper.Debit(Arg.Any<Dictionary<string, string>>()).Returns(new DepositResult() { Success = true });
            _inputs = new Dictionary<string, string>();
            //_inputs.Add("PayCurrencyId", payCurrencyId);
            //_inputs.Add("THBUser", user);
            //_inputs.Add("THBAPI", apiKey);
            //_inputs.Add("host", "https://liveapi.fpay.support/");
            //_inputs.Add("Amount", "100");
            //_inputs.Add("TransID", "DP123456789
            _inputs.Add("host", "http://35.187.218.91/");
            _inputs.Add("key", "KncN2ResvmFNU2Yvv0t0L67EMf6LRE");
            _inputs.Add("alliesNo", "IG20230425008");
            _inputs.Add("clientId", "92Hmllu8deHStPTmEiA2SmX277IUnBXjJL2VHWjd1rI=");
            _inputs.Add("TransID", transID);
            _inputs.Add("Amount", amount);
            _inputs.Add("CallBackUrl", $"http://payment.n88dev.com:59159/Payment/WithdrawalCallBack/{transID}");
            _inputs.Add("PayCode", "90-909");
            _inputs.Add("AccountName", "WILLIE");
            _inputs.Add("AccountNo", "123456789");
            _inputs.Add("BankCode", "BIDV");
            _inputs.Add("BankName", "BIDV BANK");
            var result = _payHelper.Credit(_inputs);
            return result.Success;
        }
    }
}
