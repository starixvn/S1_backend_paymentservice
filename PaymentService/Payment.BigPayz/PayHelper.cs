using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentCore;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.BigPayz
{
    public class PayHelper : PayHelperBase
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
            //var req = new Dictionary<string, string>
            //    {
            //        { "MerchantCode",  },
            //        { "ReturnURL",  },
            //        { "FailedReturnURL",  },
            //        { "HTTPPostURL",  },
            //        { "Amount",  },
            //        { "Currency",  },
            //        { "ItemID",  },
            //        { "PlayerId",  },
            //        { "Hash",  },
            //        { "BankCode",  },
            //        { "SenderVerification",  },
            //        { "ClientFullName",  },
            //    };

            //var (response, statusCode) = _httpHelper
            //        .PostUrlEncoded<Dictionary<string, string>, DepositResponse>($"{baseApiUrl}{Path.Deposit}", req);
            throw new NotImplementedException();

        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
        {
            throw new NotImplementedException();
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}