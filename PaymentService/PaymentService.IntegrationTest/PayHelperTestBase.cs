using Newtonsoft.Json;
using PaymentCore.Models;
using PaymentService.Service.Model;

namespace PaymentService.IntegrationTest;

public class PayHelperTestBase
{
    protected Dictionary<string, string> GenerateDepositInputsForMobilePay(string depositInfo, BankInfoPay bankInfoPay)
    {
        var depositInfoDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(depositInfo);

        var inputs = new Dictionary<string, string>
        {
            { "PayAccount", depositInfoDictionary["PayAccount"] },
            { "SysId", depositInfoDictionary["SysId"] }
        };

        if (inputs.ContainsKey("SysId"))
        {
            inputs.Add("BankName", bankInfoPay.BankName);
            inputs.Add("BankCode", bankInfoPay.BankCode);
        }

        return inputs;
    }

    protected Dictionary<string, string> GenerateDepositInputsFromCustInfo()
    {
        var customer = GenerateCustomer();

        return new Dictionary<string, string>
        {
            { "CustId", customer.CustId.ToString() },
            { "UserName", customer.UserName },
            { "AccountName", customer.AccountName },
            { "Lang", customer.UserLang },
            { "PhoneNo", customer.PhoneNo },
            { "Birth_d", customer.Birth_d },
            { "Birth_m", customer.Birth_m },
            { "Birth_y", customer.Birth_y }
        };
    }

    protected Dictionary<string, string> GenerateInputsFromDepositInfo(string info)
    {
        var depositInfo = new DepositInfoModel
        {
            CurrencyId = 61,
            Amount = 1000,
            DepositInfo = info
        };

        return new Dictionary<string, string>
        {
            { "CurrencyId", depositInfo.CurrencyId.ToString() },
            { "Amount", depositInfo.Amount.ToString("G29") },
            { "DepositInfo", depositInfo.DepositInfo }
        };
    }

    protected Dictionary<string, string> GenerateInputsFromOriginalRequest(string transId)
    {
        var req = new
        {
            TransId = transId,
            ClientIP = "1.0.0.1",
            ReturnUrl = "ReturnUrl",
            DeviceType = DeviceTypeEnum.Desktop
        };

        return new Dictionary<string, string>
        {
            { "TransID", req.TransId },
            { "ClientIP", req.ClientIP },
            { "ReturnUrl", req.ReturnUrl },
            { "CallBackUrl", $"http://www.callbackurl.com/Payment/DepositCallBack/{transId}"},
            { "DeviceType", ((int)req.DeviceType).ToString() },
            { "IsTest", "false" },
            { "PayLangId", "en" }
        };
    }
    protected Dictionary<string, string> GenerateInputsFromPaySetting(string setting)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(setting);
    }

    protected Dictionary<string, string> GenerateInputsFromWithdrawalInfoForOnlineBanking(WithdrawalType withdrawalType,
        string info)
    {
        var withdrawalInfo = new WithdrawalRequest
        {
            CurrencyId = 61,
            Amount = 1000,
            AccountNo = "58585858",
            WithdrawalInfo = info,
            WithdrawalType = withdrawalType
        };

        return new Dictionary<string, string>
        {
            { "CurrencyId", withdrawalInfo.CurrencyId.ToString() },
            { "Amount", withdrawalInfo.Amount.ToString("G29") },
            { "AccountNo", withdrawalInfo.AccountNo },
            { "WithdrawalInfo", withdrawalInfo.WithdrawalInfo },
            { "WithdrawalType", ((int)withdrawalInfo.WithdrawalType).ToString() }
        };
    }
    protected Dictionary<string, string> GenerateInputsFromWithdrawalTypeForOnlineBanking(
        string withdrawalInfo,
        BankInfoPay bankInfo)
    {
        var withdrawalInfoDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(withdrawalInfo);

        return new Dictionary<string, string>
        {
            { "BankName", bankInfo.BankName },
            { "BankCode", bankInfo.BankCode },
            { "IFSCCode", withdrawalInfoDictionary["IFSCCode"] }
        };
    }

    protected Dictionary<string, string> GenerateWithdrawalInputsFromCustInfo()
    {
        var customer = GenerateCustomer();

        return new Dictionary<string, string>
        {
            { "CustId", customer.CustId.ToString() },
            { "UserName", customer.UserName },
            { "AccountName", customer.AccountName },
            { "Lang", customer.UserLang },
            { "PhoneNo", customer.PhoneNo },
            { "PhoneVerified", customer.PhoneVerified.ToString() },
            { "AreaCode", customer.AreaCode }
        };
    }

    protected Dictionary<string, string> GenerateWithdrawalInputsFromOriginalRequest(string transId)
    {
        return new Dictionary<string, string>
        {
            { "TransID", transId },
            { "ClientIP", "1.0.0.1" },
            { "OutgoingIp", "1.0.0.1" },
            { "CallBackUrl", $"http://www.callbackurl.com/Payment/WithdrawalCallBack/{transId}"},
            { "IsTest", "false" }
        };
    }

    protected Dictionary<string, string> GenerateWithdrawalInputsFromPayInfo(BankInfoPay bankInfo)
    {
        return new Dictionary<string, string>
        {
            { "PayCurrencyId", bankInfo.PayCurrencyId },
            { "PayCode", bankInfo.PayCode }
        };
    }
    private static Customer GenerateCustomer()
    {
        return new Customer
        {
            CustId = 651403,
            UserName = "debbieinr001",
            AccountName = "Jatinder Mesi",
            UserLang = "en",
            PhoneNo = "966521475",
            Birth_d = "1",
            Birth_m = "1",
            Birth_y = "1990",
            PhoneVerified = true,
            AreaCode = "+66",
        };
    }

    protected Dictionary<string, string> GenerateInputsFromPayInfo(int payId, string payCurrencyId, string payCode)
    {
        var payInfo = new PayInfo
        {
            PayId = payId,
            PayCurrencyId = payCurrencyId,
            PayCode = payCode
        };

        return new Dictionary<string, string>
        {
            { "PayCurrencyId", payInfo.PayCurrencyId },
            { "PayCode", payInfo.PayCode }
        };
    }

    protected Dictionary<string, string> GenerateCallBackParametersFromPayResource(string setting, DepositRequest depositRequest)
    {
        var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(setting);
        inputs.Add("TransID", depositRequest.TransId);
        inputs.Add("DepositInfo", depositRequest.DepositInfo);
        inputs.Add("DepositType", ((int)depositRequest.DepositType).ToString());
        inputs.Add("RequestAmount", depositRequest.Amount.ToString("G29"));
        inputs.Add("CurrencyId", depositRequest.CurrencyId.ToString());

        return inputs;
    }

    protected Dictionary<string, string> GenerateCreditCallBackParametersFromPayResource(string setting, WithdrawalRequest withdrawalRequest)
    {
        var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(setting);
        inputs.Add("TransId", withdrawalRequest.TransId);
        inputs.Add("WithdrawalType", ((int)withdrawalRequest.WithdrawalType).ToString());
        inputs.Add("TranCurrencyIdsId", withdrawalRequest.CurrencyId.ToString());

        return inputs;
    }
}