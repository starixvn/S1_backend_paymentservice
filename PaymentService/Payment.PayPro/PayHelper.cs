using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.PayPro;

public class PayHelper :　PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    
    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }
    
    #region Debit
    
    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["Host"];
        var merchantId = inputs["MerchantID"];
        var merchantKey = inputs["MerchantKey"];
        var merchantPass1 = inputs["MerchantPass1"];
        var merchantPass2 = inputs["MerchantPass2"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var payCurrencyCode = inputs["PayCurrencyId"];
        var strAmount = reqAmount.ToString("G29");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var payCode = inputs["PayCode"];
        var bankCode = inputs["BankCode"];
        var merchantIVKey = merchantPass1 + merchantPass2;
        
        try
        {
            //索取銀行清單保留 以免後續還要用
            // var bankPlainReq = new Dictionary<string, string>
            // {
            //     { "merchantId", merchantId },
            //     { "currency", "VND" },
            //     { "amount", "100000000" }
            // };
            // var bankData = AESEncryptContent(ToJson(bankPlainReq), merchantKey,merchantIVKey);
            // var bankReq = new Dictionary<string, string>
            // {
            //     { "merchantId", merchantId } ,
            //     { "message", bankData}
            // };
            // var bankRes =  GetBankList(apiUrl, bankReq);
            
            var payMethod = payCode switch
            {
                "OnlineBanking" => "offline",
                "momopay_vn" => "qrpay",
                "vittelpay_vn" => "qrpay",
                "zalopay_vn" => "qrpay",
                _ => "offline"
            };

            if (payMethod == "qrpay")
            {
                bankCode = payCode;
            }

            var plainReq = new Dictionary<string, string>
            {
                {"merchantId", merchantId},
                {"transactionId", transId},
                {"transactionType", "D"},
                {"payMethod", payMethod},
                {"currency", payCurrencyCode},
                {"amount", strAmount},
                {"callback", notifyUrl},
                {"response", notifyUrl},
                {"designatedBankCode", bankCode}
            };
            
            var data = AESEncryptContent(ToJson(plainReq), merchantKey,merchantIVKey);

            var req = new Dictionary<string, string>
            {
                { "merchantId", merchantId } ,
                { "message", data}
            };

            _logger.LogInformation($"plainData={JsonConvert.SerializeObject(plainReq)}.");
            
            var res = CreateOrder(apiUrl, req);

            var response = new DepositResult
            {
                Success = true,
                ErrorCode = res.Status ,
                Url = res.RedirectUrl,
            };

            return response;
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"PayPro Deposit failed, Code:{ex.Code}, Message:{ex.Message}");
            return new DepositResult(false, ex.Code, ex.Message);
        }
    }
    
    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var transId = payResource["TransID"]; 
        var merchantKey = payResource["MerchantKey"];
        var merchantPass1 = payResource["MerchantPass1"];
        var merchantPass2 = payResource["MerchantPass2"];
        var merchantIVKey = merchantPass1 + merchantPass2;
        
        //沒簽名可驗 解析出來依照transStatus去做單的狀態
        var req = FromJson<CallBackDebitRequest>(bodyContent);
        var decryptDebitDate = AESDecryptContent(req.Message, merchantKey, merchantIVKey);
        var decryptDebitResult = FromJson<CallBackDebitData>(decryptDebitDate);
        
        
        _logger.LogInformation($"PayPro - decryptDebitResult={JsonConvert.SerializeObject(decryptDebitResult)}.");
        
        var transStatus = PayProDefinition.GetPayInTransStatus(decryptDebitResult.Status);

        var response = new
        {
            status = PayProDefinition.CallbackResponse,
            message = ""
        };
        
        var result = new NotifyDepositResult
        {
            ThirdId = decryptDebitResult.ReferenceId,
            DepositId = transId,
            TransStatus = transStatus,
            ActualAmount = decryptDebitResult.Amount,
            Response = JsonConvert.SerializeObject(response)
        };

        return result;
    }
    
    #endregion
    
    #region Credit
    
    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var apiUrl = inputs["Host"];
        var merchantId = inputs["MerchantID"];
        var merchantKey = inputs["MerchantKey"];
        var merchantPass1 = inputs["MerchantPass1"];
        var merchantPass2 = inputs["MerchantPass2"];
        var reqAmount = decimal.Parse(inputs["Amount"]);
        var strAmount = reqAmount.ToString("G29");
        var transId = inputs["TransID"];
        var notifyUrl = inputs["CallBackUrl"];
        var accountName = inputs["AccountName"];
        var accountNo = inputs["AccountNo"];
        var bankCode = inputs["BankCode"];
        //詢問是用bankName 但用bankCode可過 先保留
        //var bankName = inputs["BankName"];
        var currencyId = inputs["PayCurrencyId"];
        var merchantIVKey = merchantPass1 + merchantPass2;

        try
        {
            var plainReq = new Dictionary<string, string>
            {
                { "merchantId", merchantId },
                { "transactionId", transId },
                { "transactionType", "W" },
                { "currency", currencyId },
                { "amount", strAmount },
                { "callback", notifyUrl },
                { "bankName", bankCode },
                { "bankAcctName", accountName },
                { "bankAccNum", accountNo},
                { "response", notifyUrl },
            };
            
            var data = AESEncryptContent(ToJson(plainReq), merchantKey,merchantIVKey);

            var req = new Dictionary<string, string>
            {
                { "merchantId", merchantId } ,
                { "message", data}
            };

            _logger.LogInformation($"plainData={JsonConvert.SerializeObject(plainReq)}.");
            
            var res = Payout(apiUrl, req);

            if (res.Status == PayProDefinition.CallbackFailResponse)
            {
                var failResponse = new WithdrawResult
                {
                    Success = false,
                    RawContent = JsonConvert.SerializeObject(res)
                };

                return failResponse;
            }
            
            var response = new WithdrawResult
            {
                Success = true,
                RawContent = JsonConvert.SerializeObject(res)
            };

            return response;
            
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"PayPro Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
    
    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var transId = payResource["TransId"]; 
        var merchantKey = payResource["MerchantKey"];
        var merchantPass1 = payResource["MerchantPass1"];
        var merchantPass2 = payResource["MerchantPass2"];
        var merchantIVKey = merchantPass1 + merchantPass2;
        
        //沒簽名可驗 解析出來依照transStatus去做單的狀態
        var req = FromJson<CallBackCreditRequest>(bodyContent);
        var decryptCreditDate = AESDecryptContent(req.Message, merchantKey, merchantIVKey);
        var decryptCreditResult = FromJson<CallBackCreditData>(decryptCreditDate);
        
        _logger.LogInformation($"PayPro - decryptDebitResult={JsonConvert.SerializeObject(decryptCreditResult)}.");
        
        var transStatus = PayProDefinition.GetPayOutTransStatus(decryptCreditResult.Status);

        var response = new
        {
            status = PayProDefinition.CallbackResponse,
            message = ""
        };
        
        var result = new NotifyWithdrawResult
        {
            ThirdId = decryptCreditResult.ReferenceId,
            TransStatus = transStatus,
            Response = JsonConvert.SerializeObject(response),
            WithdrawId = transId,
            Message = decryptCreditResult.TransactionTypeDesc
        };

        return result;
    }

    #endregion

    #region UnUsed

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Private

    private CreateOrderResponse CreateOrder(string baseApiUrl, Dictionary<string, string> req)
    { 
        var apiUrl = baseApiUrl + PayProDefinition.Path.CreateOrder;

        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostUrlEncoded<Dictionary<string, string>, CreateOrderResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        { 
            throw new CustomException( 
                $"PayPro creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Status == "fail")
            throw new CustomException("PayPro creat order failed Failed.");

        return result;
    }

    private CreatePayoutResponse Payout(string baseApiUrl, Dictionary<string, string> req)
    {
        var apiUrl = baseApiUrl + PayProDefinition.Path.CreatePayout;
        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostUrlEncoded<Dictionary<string, string>, CreatePayoutResponse>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        {
            throw new CustomException(
                $"PayPro create payout failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.Status == "fail")
            throw new CustomException("PayPro create payout failed, msg=" + result.Errors);

        return result;
    }
    
    private GetBankList GetBankList(string baseApiUrl, Dictionary<string, string> req)
    { 
        var apiUrl = baseApiUrl + "/api/merchant/banklist";

        var (result, statusCode) = _httpHelper
            .WithHeader("accept", MediaType.Json)
            .PostUrlEncoded<Dictionary<string, string>, GetBankList>(apiUrl, req);

        if (!statusCode.IsSuccessStatus())
        { 
            throw new CustomException(
                $"PayPro creat order failed, HttpStatus: {(int)statusCode} {statusCode}");
        }

        if (result.status == "fail")
            throw new CustomException("PayPro creat order failed Failed.");

        return result;
    }

    #endregion
    
}