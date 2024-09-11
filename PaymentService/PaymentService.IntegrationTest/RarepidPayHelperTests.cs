using NSubstitute;
using NUnit.Framework;
using Payment.RarepidPay;

namespace PaymentService.IntegrationTest
{
    [TestFixture]
    public class RarepidPayHelperTests
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void A01_()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            var payHelper = new PayHelper(serviceProvider);

        }
    }
}