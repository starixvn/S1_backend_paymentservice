using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers
{
    public class TestController : Controller
    {
        [Route("RarepidPay/Api/QRDeposit")]
        public JsonResult Index()
        {
            var depositResponse = new
            {
                status = "0",
                status_message = "NO_ERROR",
                tran_id = "DD1413167",
                pg_tran_id = "D0000000001",
                token = "1ae3ebc1-41ab-4a4e-9ef2-0a34ec05a039",
                amount = "1000",
                total_fees = "1.15",
                currency = "INR",
                redirect_to = "https://www.google.com/"
            };

            return new JsonResult(depositResponse);
        }

        [Route("RarepidPay/Api/Withdraw")]
        public JsonResult RarepidPayWithdrawalResponse()
        {
            var withdrawalResponse = new
            {
                status = "0",
                status_message = "NO_ERROR",
                tran_id = " 621096",
                payout_tran_id = "C00000000013422",
                token = "f0375599-b077-47aa-b39a-6267a8bca571",
                remark = "payout for player PokerGod666",
                amount = "50",
                total_fees = "1.15",
                currency = "INR",
                to_bank_code = "KOTAK",
                to_bank_account = "123455778980",
                ifsc_code = "123455778980",
                phone_number = "123455778980"
            };

            return new JsonResult(withdrawalResponse);
        }
    }
}
