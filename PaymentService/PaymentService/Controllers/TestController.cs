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
        [Route("OOPay/{status}/order/create")]
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
        [Route("OOPay/{status}/payout/create")]
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

        [Route("OneWallet/{status}/SH/sendPay")]
        public IActionResult OneWalletResponse(string status)
        {
            switch (status.ToLower())
            {
                case "decodefail":
                    // case 1 Wrong request : json decode failed. For more information please read the documentation
                    return new ContentResult
                    {
                        Content = "eyJlcnJvcl9jb2RlIjo0MDUsImRhdGEiOnsibWVzc2FnZSI6Impzb24gZGVjb2RlIGZhaWxlZC4gRm9yIG1vcmUgaW5mb3JtYXRpb24gcGxlYXNlIHJlYWQgdGhlIGRvY3VtZW50YXRpb24ifX0="
                    };

                case "codefail":
                    // case 2 Success request with fail code : The given data was invalid. The notify url not in the whitelist.
                    return new ContentResult
                    {
                        Content = "eyJpdiI6Ino3V2JHcHpBVWV1dVZxQWVmcjRiQnc9PSIsInZhbHVlIjoibnpSUFJRUWRGSUxRZHUzWFFrQmI2NFwvRCtya1oxdCttblVGVllSMEdHaGlmcURBUGhaVDkzcURaK2VpY0tidlRWTFFrYktaR0hXbnp3cVVkQ05JMnpcL0xZZUdvc0QwNFBWNWtUK2ZETzRxTm9NSjRiNVwvcTBRbjZmeXNweWJHOEZlQkc1VmppVkxOaGdBWFBDZUtMeDZRPT0ifQ=="
                    };

                // success
                default:
                    return new ContentResult
                    {
                        Content = "eyJpdiI6IjJ1MkxkcXgxOVNyZ0FYU1FyMjRMY3c9PSIsInZhbHVlIjoiUEJoZTNLSExkaUNyUHhua0oydUpJZ2RGaU1PZjArbm9Ybm00dWVWbmFSaDd1empsTXJHeGlzUlJITk9KT3Y5NmErdGVWMnZ0S256eFY3bzFZdGxYUlBOd20yMDYvUzJCWnZnbHVVWjlaUjBwRjV6eHQ3M2pyb2s2bUtEY1pmOUJxdWc5dE40cVo0NGlJcmpCSUZBSk9RL3pkSFhvL29tWEJWd2VsMTJ0MEN2bDFSM0x2U1JIWFpqdlkzaGNza01Xc09QRlMxRlJMMEYxT0Y3aEJOUlM2eUNUN3AzMHYxMWlpZENsMnNwb0s5U1RkemwyYWcrbEZGRjhHYzk3dWxGMFkyQWhGejEzeHBDWSt2RElmUEhVanMrVHZGVVA1aEhXclI5Tmkwa0NFUWFmV1ZyRVhGZFJ5WFcvUTBPMmk5cXoifQ=="
                    };
            }
        }

        [Route("OneWallet/{status}/SH/sendWithdraw")]
        public IActionResult OneWalletCreditResponse(string status)
        {
            switch (status.ToLower())
            {
                case "decodefail":
                    // case 1 Wrong request : json decode failed. For more information please read the documentation
                    return new ContentResult
                    {
                        Content = "eyJlcnJvcl9jb2RlIjo0MDUsImRhdGEiOnsibWVzc2FnZSI6Impzb24gZGVjb2RlIGZhaWxlZC4gRm9yIG1vcmUgaW5mb3JtYXRpb24gcGxlYXNlIHJlYWQgdGhlIGRvY3VtZW50YXRpb24ifX0="
                    };

                case "codefail":
                    // case 2 Success request with fail code : The given data was invalid. The notify url not in the whitelist.
                    return new ContentResult
                    {
                        Content = "eyJpdiI6Ino3V2JHcHpBVWV1dVZxQWVmcjRiQnc9PSIsInZhbHVlIjoibnpSUFJRUWRGSUxRZHUzWFFrQmI2NFwvRCtya1oxdCttblVGVllSMEdHaGlmcURBUGhaVDkzcURaK2VpY0tidlRWTFFrYktaR0hXbnp3cVVkQ05JMnpcL0xZZUdvc0QwNFBWNWtUK2ZETzRxTm9NSjRiNVwvcTBRbjZmeXNweWJHOEZlQkc1VmppVkxOaGdBWFBDZUtMeDZRPT0ifQ=="
                    };

                // success
                default:
                    return new ContentResult
                    {
                        Content = "eyJpdiI6Ikh6Sk5nUU9aQlRqalVMbk5PSnE0eGc9PSIsInZhbHVlIjoidE8yVXM0VXFBODhkdlZiZmJncG1LOVRsamRXVG9FNXQzUStzVndSbFYxb2JJYlhVL0ZwalI4dlNZT0pEdkR6Q0NUYU5BRk9JNDE0Z0tKM3ZMcXlqeGZ2V0F6TkJlUWJsNFNEeTF2TTA1KzU2NmNTV3JWZ2ZzWVF4Z3YxZk1PNkYwa0YzSVVVNStlN1FQUm90aTVVM2x3PT0ifQ=="
                    };
            }
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

        [Route("PomPay/{status}/v2/api/payment")]
        public JsonResult PomPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var errorCode = isSuccess ? "" : "E1001";
            var message = isSuccess ? "" : "Service is temporarily not available";

            var response = new
            {
                errorCode,
                message,
                qrString = "abcd",
                referenceId = $"PP{DateTime.Now.ToString("yyyyMMddHHmmss")}"
            };

            return new JsonResult(response);
        }

        [Route("PomPay/{status}/v2/payout")]
        public JsonResult PomPayCreditResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var statusCode = isSuccess ? "success" : "fail";
            var errorCode = isSuccess ? "" : "E1001";
            var message = isSuccess ? "" : "Service is temporarily not available";

            var response = new
            {
                status = statusCode,
                errorCode,
                message,
                referenceId = $"PP{DateTime.Now.ToString("yyyyMMddHHmmss")}"
            };

            return new JsonResult(response);
        }


        [Route("DfPay/{status}/app/DFPayFun")]
        public IActionResult DfPayWithdrawalResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    systate = 0,
                    msg = "success"
                };

                return new JsonResult(response);
            }

            return BadRequest(new { systate = 99, msg = "Fail" });
        }

        [Route("LuckyPay/{status}/api/coin/pay/request")]
        [Route("LuckyPay/{status}/api/bank/agentPay/request")]
        public JsonResult LuckyPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var code = isSuccess ? 200 : 999;
            var message = isSuccess ? "OK" : "NOT OK";

            var response = new
            {
                success = isSuccess,
                code,
                message,
                data = new
                {
                    payUrl = "http://www.google.com"
                }
            };

            return new JsonResult(response);
        }

        [Route("LnwPay/{status}/services/api/v1/{type}-request")]
        [Route("LnwPay/{status}/services/api/v2/{type}-request")]
        public JsonResult LnwPayResponse(string status, string type)
        {
            switch (type.ToLower())
            {
                // Auth
                case "service":
                    var serviceResponse = new
                    {
                        status = "success",
                        message = "Service is available",
                        data = new
                        {
                            token_type = "Bearer",
                            access_token = "VzAyOmI2MTU5OWYyYmVhZDQ3ZmVhZjI5NWM4ZGNhNTA3MWM4OmJiZGE5YmZjLTRjMTctNGFhMS05ZTdhLWQ1OWM0NTI0ZGI2ZQ=="
                        }
                    };
                    return new JsonResult(serviceResponse);

                // DepositAuto
                case "account":
                    var response = new
                    {
                        status,
                        data = new
                        {
                            accountName = "ประพน ชอบเล่น",
                            accountNo = "0849814507",
                            accountBank = "BAY",
                            amount = 780.75m,
                            accountURL = "https://autopay321.com/public/account?id=0779d0a5-9315-4012-8d68-8c0915d7403d",
                            requestId = "bbda9bfc-4c17-4aa1-9e7a-d59c4524db6e",
                        }
                    };
                    return new JsonResult(response);

                // DepositQr
                case "qrcode":
                    var qrResponse = new
                    {
                        status,
                        data = new
                        {
                            promptName = "ประพน ชอบเล่น",
                            promptNo = "0849814507",
                            amount = 780.75m,
                            payload = "00020101021229370016A000000677010111011300668498145075802TH53037645406780.756304DC35",
                            qrcodeBase64 = "/9j/4AAQS......kZJRgAZ\"",
                            qrcodeURL = "https://lnwpay123.com/public/Image?id=bbda9bfc-4c17-4aa1-9e7ad59c4524db6e",
                            requestId = "bbda9bfc-4c17-4aa1-9e7a-d59c4524db6e"
                        }
                    };
                    return new JsonResult(qrResponse);

                // DepositTrue
                case "wallet":
                    var trueResponse = new
                    {
                        status,
                        data = new
                        {
                            amount = 780.75m,
                            walletURL = "https://a3.ot-pb-ipsv001.com/twallet/Account?id=153d775f-b54e-4904-b1b8-b4976f9b2697",
                            requestId = "bbda9bfc-4c17-4aa1-9e7a-d59c4524db6e"
                        }
                    };
                    return new JsonResult(trueResponse);

                // DepositPayway
                // DepositKHQr
                // case "payment":
                default:
                    var paymentResponse = new
                    {
                        status,
                        data = new
                        {
                            amount = 15.00m,
                            qrcodeURL = "https://khqr01.ot-pb-ipsv001.com/public/image?id=7f92397d-b7b2-4fc8-93cd-705fc3f279d7",
                            paymentUrl = "https://payway.ot-pb-ipsv001.com/public/image?id=81f73e51-02f8-4189-b111-84dcfe4f8af6",
                            requestId = "7f92397d-b7b2-4fc8-93cd-705fc3f279d7",
                            ref1 = "T003"
                        },
                        hash = "1df7f3eaf077cf9bd5892599b659225193e3991f695c360f448769c02fb38fc5"
                    };
                    return new JsonResult(paymentResponse);
            }
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

        [Route("SmartPay/{status}/api/pay/V2")]
        [Route("SmartPay/{status}/api/defray/V2")]
        [Route("JokPay/{status}/api/pay/V2")]
        [Route("JokPay/{status}/api/defray/V2")]
        public JsonResult SmartPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var code = isSuccess ? "0" : "-1";
            var msg = isSuccess ? "ok" : "not ok";

            var response = new
            {
                code,
                msg,
                detail = new
                {
                    PayURL = "http://www.google.com"
                }
            };

            return new JsonResult(response);
        }
        
        [Route("MbmPay/{status}/api/deposit/order")]
        [Route("MbmPay/{status}/api/payout/order")]
        public JsonResult MbmPayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            var code = isSuccess ? "0" : "-1";
            var msg = isSuccess ? "ok" : "not ok";

            var response = new
            {
                code,
                msg,
                data = "http://www.google.com"
            };

            return new JsonResult(response);
        }

        [Route("BCOTC/{status}/api/v2/merchant-orders")]
        [Route("BCOTC/{status}/api/v2/merchant-payout-requests")]
        public IActionResult BCOTCResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    status = "1",
                    msg = string.Empty,
                    payment_url = "https://payment_url",
                    payment_information = new 
                    {
                        bank_name = "bankName",
                        bank_branch = "bankBranch",
                        account_name = "accountName",
                        account_number = "111111",
                        alipay_url = string.Empty,
                        alipay_account_id = string.Empty,
                        alipay_user_full_name = string.Empty,
                        e_cny_wallet_id = string.Empty,
                        e_cny_user_name = string.Empty,
                        e_cny_bank_name = string.Empty
                    },
                    order_amount = new 
                    {
                        crypto_amount = string.Empty,
                        fiat_amount = "1000"
                    },
                    order_id = "BC-OTC123456"
                };

                return new JsonResult(response);
            }

            var errorMessage = new
            {
                status = "0",
                // msg = "An error occurred"
                msg = new
                {
                    payer = new []
                    {
                        "Payer cannot be longer than 4 characters"
                    }
                }
            };
            return BadRequest(errorMessage);
        }
        
        [Route("AeePay/{status}/api/payment")]
        [Route("AeePay/{status}/api/withdraw")]
        public IActionResult AeePayResponse(string status)
        {
            var isSuccess = CheckIsSuccess(status);
            if (isSuccess)
            {
                var response = new
                {
                    success = true,
                    resultCode = "10000",
                    resultMsg = "Success",
                    data = new
                    {
                        payUrl = "https://www.google.com",
                        orderno = "123456"
                    }
                };

                return new JsonResult(response);
            }

            var errorMessage = new
            {
                success = false,
                resultCode = "18000",
                resultMsg = "数据签名不通过",
            };
            return BadRequest(errorMessage);
        }

        [Route("OneDayPay/{status}/api/Deposit/Submit{method}")]
        [Route("OneDayPay/{status}/api/Withdrawal/Submit{method}")]
        public JsonResult MbmPayResponse(string status, string method)
        {
            var isSuccess = CheckIsSuccess(status);
            var successCode = isSuccess ? 200 : -1;
            var errorMessage = isSuccess ? "ok" : "not ok";
            var transactionId = DateTime.Now.ToString("yyyyMMddHHmmss");

            if (method.Equals("deposit", StringComparison.OrdinalIgnoreCase))
            {
                var debitResponse = new
                {
                    isSuccess,
                    successCode,
                    errorMessage,
                    data = new
                    {
                        transactionId,
                        url = "http://www.google.com"
                    }
                };
                return new JsonResult(debitResponse);
            }

            var creditResponse = new
            {
                isSuccess,
                successCode,
                errorMessage,
                data = new
                {
                    transactionId
                }
            };
            return new JsonResult(creditResponse);
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
