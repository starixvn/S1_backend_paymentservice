using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
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
            if (CheckIsSuccess(status))
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

        [Route("SpeedPay/{success}/{method}")]
        public JsonResult SpeedPayResponse(string success, string method)
        {
            var methodMapper = new Dictionary<string, string>
            {
                { "qrh5", "deposit" },
                { "payout", "withdrawal" }
            };

            var resCode = success.Equals("success", StringComparison.OrdinalIgnoreCase) ? "000" : "999";

            if (methodMapper[method].Equals("deposit", StringComparison.OrdinalIgnoreCase))
            {
                var response = new
                {
                    amount = "200",
                    orderNo = "POSTMAN_20211001140546",
                    gbpOrderNo = "QRHA20211001060547000200868977",
                    createTime = "20211001060546",
                    resultUrl = "http://www.google.com",
                    code = "0000",
                    msg = "Success"
                };

                return new JsonResult(
                    new
                    {
                        version = "1.0",
                        merchantNo = "GBP111222",
                        resCode,
                        resMsg = "test msg",
                        response = ToBase64Encode(ToJson(response))
                    });
            }
            else
            {
                var response = new
                {
                    code = "0000",
                    msg = "订单建立成功",
                    amount = "100.00",
                    fee = "2.00",
                    orderNo = "Order12345789",
                    gbpOrderNo = "190101185511101",
                    createTime = "20101231032055"
                };

                return new JsonResult(
                    new
                    {
                        merchantNo = "GBP111222",
                        resCode,
                        resMsg = "test msg",
                        response = ToBase64Encode(ToJson(response))
                    });
            }
        }

        [Route("Help2Pay/{success}")]
        public IActionResult Help2PayWithdrawalResponse(string success)
        {
            var isSuccess = success.Equals("success", StringComparison.OrdinalIgnoreCase);
            var statusCode = isSuccess ? "000" : "001";
            var msg = isSuccess ? "Success" : "Invalid Merchant";
            var responseXml = $"<Payout><statusCode>{statusCode}</statusCode><message>{msg}</message></Payout>";

            // Clear existing response headers if necessary
            Response.Clear();

            // Add the content type header for XML
            Response.Headers["Content-Type"] = "text/xml";

            // Return the XML response
            return Content(responseXml, "text/xml");
        }


        [Route("BigPayz/{status}/payin/depositv2")]
        public IActionResult BigPayzDepositResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    transaction = "009-212-20-750",
                    redirect_to = "https://www.google.com",
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("BigPayz/{status}/DuitNow/Deposit")]
        public IActionResult BigPayzDepositQrCodeResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    invoice_number = "009-212-20-750",
                    redirect_to = "https://www.google.com",
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("BigPayz/{status}/payout/withdrawal")]
        public IActionResult BigPayzWithdrawalResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    error_code = "0",
                    error_message = "Operation success",
                    invoice_number = "000-MY01-126-20-140",
                    currency_code = "MYR",
                    merchant_balance = "9620.0"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("OOPay/{status}/shou/new")]
        public IActionResult OOPayDepositResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    code = "0",
                    text = "success",
                    amount = "1000",
                    channelNo = "2",
                    tradeNo = "123456",
                    targetUrl = "https://www.google.com/"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("OOPay/{status}/fu/new")]
        public IActionResult OOPayWithdrawalResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    code = "0",
                    text = "success",
                    amount = "1000",
                    channelNo = "2",
                    tradeNo = "123456",
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("GameWallet/{status}")]
        public JsonResult GameWalletWithdrawalResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var statusCode = isSuccess ? "002" : "001";
            var statusMessage = isSuccess ? "SUCCESS" : "test msg";
            var dateNow = DateTime.Now.ToString("yyyyMMddHHmmss");

            var response = new
            {
                actualAmount = "100",
                amount = "100",
                bank = "xxx",
                currency = "THB",
                customer = "customer",
                datetime = dateNow,
                key = "xxx",
                merchant = "xxx",
                reference = "wid",
                refId = dateNow,
                statementDate = dateNow,
                statusCode,
                statusMessage,
                transactionId = dateNow
            };

            return new JsonResult(response);
        }

        [Route("EBPay/{status}/api/merchant/{method}/v2")]
        [Route("EBPay/{status}/api/merchant/{method}/create/v2")]
        public JsonResult EBPayResponse(string status, string method)
        {
            var isSuccess = CheckIsSuccess(status);
            var isDeposit = method.Equals("deposit", StringComparison.OrdinalIgnoreCase);
            var code = isSuccess ? "200" : "400";
            var msg = isSuccess ? "success": "fail msg";

            if (isDeposit)
            {
                var response = new
                {
                    code,
                    msg,
                    data = new
                    {
                        url = "http://www.google.com"
                    }
                };

                return new JsonResult(response);
            }
            else
            {
                var response = new
                {
                    code,
                    msg,
                    data = new
                    {
                        orderId = DateTime.Now.ToString("yyyyMMddHHmmss")
                    }
                };

                return new JsonResult(response);
            }
        }

        [Route("988Pay/{status}/placeOrder")]
        [Route("808Pay/{status}/placeOrder")]
        public IActionResult _988PayDepositResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    code = "0",
                    msg = "操作成功",
                    data = new
                    {
                        orderNo = "PO1716992597174341632",
                        payUrl = "https://www.google.com/"
                    }
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("988Pay/{status}/withdrawOrder")]
        [Route("808Pay/{status}/withdrawOrder")]
        public IActionResult _988PayWithdrawalResponse(string status)
        {
            if (CheckIsSuccess(status))
            {
                var response = new
                {
                    code = "0",
                    msg = "操作成功",
                    data = new
                    {
                        orderNo = "TO1716990940768194560",
                        Status = "1"
                    }
                };

                return new JsonResult(response);
            }

            return BadRequest(new { error = "Bad Response" });
        }

        [Route("PowerBank/{status}/SetDepositV3")]
        [Route("PowerBank/{status}/SetWithdrawal")]
        public JsonResult PowerBankResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var result = isSuccess ? 10 : 99;
            var msg = isSuccess ? "success" : "fail";

            var response = new
            {
                result,
                msg,
                url = "http://www.google.com"
            };

            return new JsonResult(response);
        }

        [Route("PapayaPay/{status}/api/v2/create-qr-payment")]
        public IActionResult PapayaPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var statusCode = isSuccess ? 200 : 999;
            var message = isSuccess ? "success" : "fail";

            if (isSuccess)
            {
                var response = new
                {
                    statusCode,
                    message,
                    payurl = "http://www.google.com"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { statusCode, message });
        }

        [Route("PapayaPay/{status}/api/v2/create-fundout")]
        public IActionResult PapayaPayCreditResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var statusCode = isSuccess ? 200 : 999;
            var message = isSuccess ? "success" : "fail";

            if (isSuccess)
            {
                var response = new
                {
                    message,
                    fundOutStatus = "fundOutStatus",
                    data = new
                    {
                        accountNumber = "123",
                        amount = 999m,
                        bankCode = "abc",
                        createdDate = DateTime.Now,
                        currencyCode = "THB",
                        fundOutCallbackStatus = "status",
                        fundOutDescription = "description",
                        fundOutPaymentReference = "ref"
                    }
                };

                return new JsonResult(response);
            }

            return BadRequest(new { statusCode, message });
        }

        [Route("YFBPay/{status}/order/create")]
        public IActionResult YFBPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var statusCode = isSuccess ? 200 : 999;
            var code = isSuccess ? "0" : "-1";
            var text = isSuccess ? "OK" : "NOT OK";

            if (isSuccess)
            {
                var response = new
                {
                    code,
                    text,
                    tradeNo = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    targetUrl = "http://www.google.com"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { code, text });
        }

        [Route("YFBPay/{status}/payout/create")]
        public IActionResult YFBPayCreditResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var statusCode = isSuccess ? 200 : 999;
            var code = isSuccess ? "0" : "-1";
            var text = isSuccess ? "OK" : "NOT OK";

            if (isSuccess)
            {
                var response = new
                {
                    code,
                    text,
                    tradeNo = DateTime.Now.ToString("yyyyMMddHHmmss")
                };

                return new JsonResult(response);
            }

            return BadRequest(new { code, text });
        }

        [Route("NewXfuoo/{status}/usdt/rate")]
        public IActionResult NewXfuooGetExchangeRateResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    status = 0,
                    data = new
                    {
                        fiatCurrency = "cny",
                        rate = "6.37"
                    }
                };

                return new JsonResult(response);
            }

            return BadRequest(new { status = 1, code = "030102", msg = "Exchange Fail" });
        }

        [Route("NewXfuoo/{status}/usdt/payin")]
        public IActionResult NewXfuooDepositResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    status = 0,
                    data = new
                    {
                        payUrl = "https://www.google.com.tw"
                    }
                };

                return new JsonResult(response);
            }

            return BadRequest(new { status = 1, code = "030102", msg = "Fail" });
        }

        [Route("NewXfuoo/{status}/usdt/payout")]
        public IActionResult NewXfuooWithdrawalResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    status = 0,
                    data = new
                    {
                        tradeId = "UAP2021082615PA1GNK"
                    }
                };

                return new JsonResult(response);
            }

            return BadRequest(new { status = 1, code = "030102", msg = "Fail" });
        }

        [Route("NewXyPay/{status}/pay/unifiedorder")]
        [Route("NewXyPay/{status}/withdraw/unifiedorder")]
        public IActionResult NewXyPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    payUrl = "http://www.google.com"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { errorCode = 20001, errorMsg = "上游通道通讯异" });
        }

        [Route("BixinPay/{status}/api/cash/placeCas")]
        public IActionResult BixinPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    responseContent = new
                    {
                        code = "0",
                        status = "0",
                        orderId = "123456"
                    }
                };

                return new JsonResult(response);
            }

            var errorMessage = new
            {
                responseContent = new
                {
                    code = "0",
                    status = "2",
                    msg = "fail",
                    OrderId = "123456"
                }
            };
            return BadRequest(errorMessage);
        }

        private static bool CheckIsSuccess(string status)
        {
            return status.Equals("success", StringComparison.OrdinalIgnoreCase);
        }

        private string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        private string ToBase64Encode(string str)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
