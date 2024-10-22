using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers
{
    public class TestController : Controller
    {
        [Route("RarepidPay/Api/QRDeposit")]
        public JsonResult RarepidPayDepositResponse()
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

        [Route("CommsPay/{success}/api/v1/payout/{merchantCode}")]
        public JsonResult CommsPayWithdrawalResponse(string success, string merchantCode)
        {
            var result = success.Equals("success", StringComparison.OrdinalIgnoreCase);
            var response = new
            {
                success = result,
                message = "test message"
            };

            return new JsonResult(response);
        }

        [Route("CashRuPay/web/createOnlineOrder")]
        public JsonResult CashRuPayDepositResponse()
        {
            var response = new
            {
                Code = "0",
                Msg = "success",
                Data = new
                {
                    OrderNo = "1324232451232",
                    Id = "2023101211343626342",
                    PayAmount = "400",
                    Url = "https://www.cashrupay.com/index/index?orderId=dd18382993263421"
                }
            };

            return new JsonResult(response);
        }

        [Route("CashRuPay/web/createOnlineOutOrder")]
        public JsonResult CashRuPayWithdrawalResponse()
        {
            var response = new
            {
                Code = "0",
                Msg = "success",
                Data = new
                {
                    OrderNo = "1324232451232",
                    Id = "2023101211343626342"
                }
            };

            return new JsonResult(response);
        }

        [Route("DirePay/{status}/api/callBack")]
        public IActionResult DirePayResponse(string status)
        {
            if (status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                var response = new
                {
                    message = "test",
                    txid = "abc1234",
                    pay_url = "https://www.google.com/"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Test Bad Response" });
        }

        [Route("DirePay/{status}/api/login")]
        public IActionResult DirePayLoginResponse(string status)
        {
            var response = new
            {
                access_token = "123456"
            };

            return new JsonResult(response);
        }
    }
}
