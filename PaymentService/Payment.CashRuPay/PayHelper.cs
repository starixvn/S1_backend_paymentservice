using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.CashRuPay;

public class PayHelper : PayHelperBase
{
    private readonly IFlurlHttpHelper _httpHelper;
    private readonly ILogger<PayHelper> _logger;
    private string? _currentTimeStamp;

    public PayHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<PayHelper>>();
        _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
    }

    internal static string? SignConcatSourceString { get; set; }

    internal string CurrentTimeStamp
    {
        private get => string.IsNullOrEmpty(_currentTimeStamp)
            ? DateTimeOffset.Now.ToUnixTimeSeconds().ToString()
            : _currentTimeStamp;
        set => _currentTimeStamp = value;
    }

    public override NotifyWithdrawResult CallBackCredit(
        Dictionary<string, string> formData,
        string bodyContent,
        Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        var receive = new CallBackRequest
        {
            Status = formData["status"],
            CustomerId = formData["customerid"],
            sdOrderNo = formData["sdorderno"],
            TotalFee = formData["total_fee"],
            PayTime = formData["paytime"],
            Sign = formData["sign"]
        };

        SignConcatSourceString = $"{receive.sdOrderNo}{receive.PayTime}";
        if (!VerifySign(receive.Sign, payResource["Md5Key"]))
        {
            return new NotifyWithdrawResult
            {
                WithdrawId = receive.sdOrderNo,
                TransStatus = TransStatus.Pending,
                Response = "Signature verification failed."
            };
        }

        return new NotifyWithdrawResult
        {
            WithdrawId = receive.sdOrderNo,
            ThirdId = receive.sdOrderNo,
            TransStatus = Definition.GetTransStatus(receive.Status),
            Response = Definition.CallbackResponse,
            Message = receive.Status
        };
    }

    public override NotifyDepositResult CallBackDebit(
        Dictionary<string, string> formData, 
        string bodyContent,
        Dictionary<string, string> headers, 
        Dictionary<string, string> payResource)
    {
        var receive = new CallBackRequest
        {
            Status = formData["status"],
            CustomerId = formData["customerid"],
            sdOrderNo = formData["sdorderno"],
            TotalFee = formData["total_fee"],
            PayTime = formData["paytime"],
            Sign = formData["sign"]
        };

        SignConcatSourceString = $"{receive.sdOrderNo}{receive.PayTime}";
        if (!VerifySign(receive.Sign, payResource["Md5Key"]))
        {
            return new NotifyDepositResult
            {
                DepositId = receive.sdOrderNo,
                TransStatus = TransStatus.Pending,
                Response = "Signature verification failed."
            };
        }

        var result = new NotifyDepositResult
        {
            DepositId = receive.sdOrderNo,
            ThirdId = receive.sdOrderNo,
            Comment = receive.Status,
            TransStatus = Definition.GetTransStatus(receive.Status),
            ActualAmount = decimal.Parse(receive.TotalFee),
            Response = Definition.CallbackResponse
        };

        return result;
    }

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override WithdrawResult Credit(Dictionary<string, string> inputs)
    {
        var baseApiUrl = inputs["Host"];
        var md5Key = inputs["Md5Key"];
        var aesKey = inputs["AesKey"];
        var merchantCode = inputs["MerchantCode"];
        var transId = inputs["TransID"];
        var timeStamp = CurrentTimeStamp;
        try
        {
            SignConcatSourceString = $"{merchantCode}{timeStamp}";
            var data = new CashRuPayWithdrawalPostData
            {
                OrderNo = transId,
                Money = int.Parse(inputs["Amount"]),
                NotifyUrl = inputs["CallBackUrl"],
                ReceiveAccount = inputs["AccountNo"],
                ReceiveBanktype = inputs["IFSCCode"],
                ReceiveName = inputs["AccountName"],
            };

            var dataContent = ToJson(data);
            _logger.LogInformation($"Data before encryption: {dataContent}");

            var post = new CashRuPayPost
            {
                AgentCode = merchantCode,
                TimeStamp = timeStamp,
                Data = GetEncryptData(dataContent, aesKey),
                Sign = GetPostSign(SignConcatSourceString, md5Key)
            };

            var withdrawalResponse = CreatePayout(baseApiUrl, post);
            return new WithdrawResult
            {
                Success = true,
                WithdrawId = withdrawalResponse.Data.OrderNo,
                Message = withdrawalResponse.Msg
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation(ex.Message);

            return new WithdrawResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        var baseApiUrl = inputs["Host"];
        var md5Key = inputs["Md5Key"];
        var aesKey = inputs["AesKey"];
        var merchantCode = inputs["MerchantCode"];
        var transId = inputs["TransID"];
        var timeStamp = CurrentTimeStamp;

        try
        {
            SignConcatSourceString = $"{merchantCode}{timeStamp}";
            var data = new CashRuPayPostData
            {
                OrderNo = transId,
                PayAmount = int.Parse(inputs["Amount"]),
                ThirdUserId = inputs["CustId"],
                TrsCode = inputs["PayCode"],
                CallUrl = inputs["CallBackUrl"],
                ThirdUserLevel = 1
            };

            var dataContent = ToJson(data);
            _logger.LogInformation($"Data before encryption: {dataContent}");

            var post = new CashRuPayPost
            {
                AgentCode = merchantCode,
                TimeStamp = timeStamp,
                Data = GetEncryptData(dataContent, aesKey),
                Sign = GetPostSign(SignConcatSourceString, md5Key)
            };
            var response = CreateOrder(baseApiUrl, post);

            return new DepositResult
            {
                Success = true,
                ErrorCode = "0",
                ThirdId = response.Data.Id,
                Url = response.Data.Url
            };
        }
        catch (CustomException ex)
        {
            _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

            return new DepositResult(false, ex.Code, ex.Message);
        }
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    internal string GetEncryptData(string dataContent, string aesKey)
    {
        var dataContentBytes = Encoding.UTF8.GetBytes(dataContent);
        var aesKeyBytes = Encoding.UTF8.GetBytes(aesKey);
        var aes = new RijndaelManaged
        {
            // 文件上說明需用pkcs5padding，透過網路資料與測試結果得知使用PaddingMode.PKCS7可以得到一樣的結果，故在這邊使用PaddingMode.PKCS7
            Padding = PaddingMode.PKCS7,
            Mode = CipherMode.ECB,
            Key = aesKeyBytes
        };
        var cryptoTransform = aes.CreateEncryptor();
        var encryptResult = cryptoTransform.TransformFinalBlock(dataContentBytes, 0, dataContentBytes.Length);
        return Convert.ToBase64String(encryptResult);
    }

    internal bool VerifySign(string sign, string md5Key)
    {
        var sourceString = $"{SignConcatSourceString}{md5Key}";
        var resultSignature = ToMD5(sourceString);
        return sign.Equals(resultSignature, StringComparison.OrdinalIgnoreCase);
    }

    private Response<CreateDeposit> CreateOrder(string baseApiUrl, CashRuPayPost post)
    {
        var apiUrl = GetUrl(baseApiUrl, Path.CreateDeposit);
        var (result, _) = _httpHelper
            .PostJson<CashRuPayPost, Response<CreateDeposit>>(apiUrl, post);

        if (result.Code != Definition.SuccessCode)
        {
            throw new CustomException($"CashRuPay CreateOrder response error: Code={result.Code}, Msg={result.Msg}");
        }

        return result;
    }

    private Response<CreateWithdrawal> CreatePayout(string baseApiUrl, CashRuPayPost post)
    {
        var apiUrl = GetUrl(baseApiUrl, Path.CreateWithdrawal);
        var (result, _) = _httpHelper
            .PostJson<CashRuPayPost, Response<CreateWithdrawal>>(apiUrl, post);

        if (result.Code != Definition.SuccessCode)
        {
            throw new CustomException($"CashRuPay CreatePayout response error: Code={result.Code}, Msg={result.Msg}");
        }

        return result;
    }
    private string GetPostSign(string? signConcatSourceString, string md5Key)
    {
        var sourceString = $"{signConcatSourceString}{md5Key}";
        return ToMD5(sourceString).ToLower();
    }
    private string GetUrl(string baseUrl, string path)
    {
        return $"{baseUrl.TrimEnd('/')}{path}";
    }
}