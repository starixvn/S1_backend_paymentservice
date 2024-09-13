using NUnit.Framework;

using Payment.RapidPay;

namespace PaymentService.IntegrationTest
{
    [TestFixture()]
    public class RapidPayHelperTests
    {
        private PayHelper _payHelper;
        private Dictionary<string, string> _inputs;


        // [TestCase("THB", "Nova88thbG", "VRCvgnAjDZXEWnUi5iGG", ExpectedResult = true)]
        // public bool DebitTest(string payCurrencyId, string user, string apiKey)
        // {
        //     _payHelper = new PayHelper();
        //     //_payHelper = Substitute.For<PayHelper>();
        //     //_payHelper.Debit(Arg.Any<Dictionary<string, string>>()).Returns(new DepositResult() { Success = true });
        //     _inputs = new Dictionary<string, string>();
        //     _inputs.Add("PayCurrencyId", payCurrencyId);
        //     _inputs.Add("THBUser", user);
        //     _inputs.Add("THBAPI", apiKey);
        //     _inputs.Add("host", "https://liveapi.fpay.support/");
        //     _inputs.Add("Amount", "100");
        //     _inputs.Add("TransID", "DP123456789");
        //     var result = _payHelper.Debit(_inputs);
        //     return result.Success;
        // }
        //[TestCase(ExpectedResult = true)]
        //public bool CreditTest() 
        //{
        //    _payHelper = new PayHelper();
        //    _inputs = new Dictionary<string, string>();
        //    _inputs.Add("PayCurrencyId", "MYR");
        //    _inputs.Add("MYRUser", "Nova88G");
        //    _inputs.Add("MYRAPI", "YzoErN8tkYqqkwXWlkRE");
        //    _inputs.Add("host", "https://liveapi.fpay.support/");
        //    _inputs.Add("Amount", "100");
        //    _inputs.Add("TransID", "WD123456789");
        //    var result = _payHelper.Credit(_inputs);
        //    return result.Success;
        //}
    }
}
