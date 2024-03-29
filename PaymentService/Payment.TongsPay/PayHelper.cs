using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PaymentCore;
using PaymentCore.Helpers;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models;
using PaymentCore.Models.Result;

namespace Payment.TongsPay;

public class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private const decimal AmountFix = 100;
    
    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }
    
    #region Debit

    public override DepositResult Debit(Dictionary<string, string> input)
    {
        var baseApiUrl = input["Host"];
        var key = input["OurPrivateKey"];
        var transId = input["TransID"];
        var merchantNo = input["MerchantNo"];
        var reqAmount = decimal.Parse(input["Amount"]) * AmountFix;
        var isMobile = int.Parse(input["DeviceType"]) != (int)DeviceTypeEnum.Desktop;

        try
        {
            var req = new Dictionary<string, string>
            {
                { "charset", "UTF-8" },
                { "merchantCode", merchantNo },
                { "orderNo", transId },
                { "amount", reqAmount.ToString("G29") },
                { "channel", input["PayCode"] },
                { "bankCode", isMobile ? "BANK_MOBILE" : "CASHIER" },
                { "remark", input["UserName"] },
                { "notifyUrl", input["CallBackUrl"] },
                { "returnUrl", input["ReturnUrl"] },
                { "extraReturnParam", string.Empty }
            };
            
            req.Add("sign", GetSign(req, key));
            req.Add("signType", "RSA");
            req.Add("userId", input["AccountName"]);

            return new DepositResult
            {
                Success = true,
                FormData = req,
                Url = baseApiUrl + Path.Deposit
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> input)
    {
        var key = input["PublicKey"];
        var req = FromJson<DepositCallbackRequest>(JsonConvert.SerializeObject(formData));
        var dic4Sign = new Dictionary<string, string> // 有順序
        {
            { "merchantCode", formData["merchantCode"] },
            { "orderNo", formData["orderNo"] },
            { "amount", formData["amount"] },
            { "successAmt", formData["successAmt"] },
            { "payOrderNo", formData["payOrderNo"] },
            { "orderStatus", formData["orderStatus"] },
            { "extraReturnParam", formData["extraReturnParam"] }
        };

        if (!VerifySign(dic4Sign, key, req.Sign))
        {
            return new NotifyDepositResult
            {
                DepositId = req.TransId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        var transStatus = TongsPayDefinition.GetPayinTransStatus(req.Status);

        return new NotifyDepositResult
        {
            DepositId = req.TransId,
            ActualAmount = req.Amount / AmountFix,
            ThirdId = req.TransId,
            Comment = req.Status,
            TransStatus = transStatus,
            Response = TongsPayDefinition.CallbackResponse
        };
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Credit

    public override WithdrawResult Credit(Dictionary<string, string> input)
    {
        var baseApiUrl = input["Host"];
        var key = input["OurPrivateKey"];
        var transId = input["TransID"];
        var merchantNo = input["MerchantNo"];
        var reqAmount = decimal.Parse(input["Amount"]) * AmountFix;

        try
        {
            var req = new Dictionary<string, string>
            {
                { "charset", "UTF-8" },
                { "merchantCode", merchantNo },
                { "orderNo", transId },
                { "amount", reqAmount.ToString("G29") },
                { "bankCode", input["BankCode"] },
                { "accountNumber", input["AccountNo"] },
                { "accountName", input["AccountName"] },
                { "provinceName", "省" },
                { "cityName", "市" },
                { "remark", input["UserName"] },
                { "notifyUrl", input["CallBackUrl"] },
                { "extraReturnParam", string.Empty }
            };
            
            req.Add("sign", GetSign(req, key));
            req.Add("signType", "RSA");

            var (response, statusCode) = _httpHelper
                .PostUrlEncoded<Dictionary<string, string>, WithdrawalResponse>($"{baseApiUrl}{Path.Withdrawal}", req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException($"TongsPay withdrawal failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (response.Code is not ("PAYING" or "SUCCESS"))
            {
                throw new CustomException(response.Message, response.Code);
            }

            return new WithdrawResult
            {
                Success = true,
                RawContent = JsonConvert.SerializeObject(response)
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

            return new WithdrawResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent,
        Dictionary<string, string> headers, Dictionary<string, string> payResource)
    {
        var key = payResource["PublicKey"];
        var req = FromJson<WithdrawalCallbackRequest>(JsonConvert.SerializeObject(formData));
        var dic4Sign = new Dictionary<string, string> // 有順序
        {
            { "merchantCode", formData["merchantCode"] },
            { "orderNo", formData["orderNo"] },
            { "amount", formData["amount"] },
            { "recode", formData["recode"] },
            { "msg", formData["msg"] },
            { "extraReturnParam", formData["extraReturnParam"] }
        };

        if (!VerifySign(dic4Sign, key, req.Sign))
        {
            return new NotifyWithdrawResult
            {
                ThirdId = req.TransId,
                TransStatus = TransStatus.Pending,
                Response = "Signature does not match."
            };
        }

        var transStatus = TongsPayDefinition.GetPayoutTransStatus(req.Code);

        return new NotifyWithdrawResult
        {
            ThirdId = req.TransId,
            TransStatus = transStatus,
            Message = req.Message,
            Response = TongsPayDefinition.CallbackResponse,
            WithdrawId = req.TransId
        };
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Private

    private string GetSign(IEnumerable<KeyValuePair<string, string>> input, string key)
    {
        var str = string.Join('&', input
            .Select(x => $"{x.Key}={x.Value}"));

        _logger.LogInformation(str);
        
        var sign = RsaSign(str, key, HashAlgorithmName.SHA1);
        var urlEncodedSign = HttpUtility.UrlEncode(sign);

        return urlEncodedSign;
    }
    private bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string publicKey, string target)
    {
        target = HttpUtility.HtmlDecode(target);
        var str = string.Join('&', param
            .Select(x => $"{x.Key}={x.Value}"));
        
        _logger.LogInformation(str);
        
        var result = RsaHelper.RsaVerifyData(str, target, publicKey, HashAlgorithmName.SHA1);

        return result;
    }

    #endregion
}