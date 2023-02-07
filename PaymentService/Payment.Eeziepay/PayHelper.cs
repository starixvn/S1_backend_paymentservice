using PaymentCore;
using System.Globalization;
using System.Xml.Serialization;
using PaymentCore.Models.Result;

namespace Payment.Eeziepay
{
    public class PayHelper : PayHelperBase
    {
        Dictionary<string, string> bankDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            {"VND","HCM" },
            {"THB","Bangkok" },
            {"MYR","Kuala Lumpur" },
            {"IDR","Jakarta" }
        };
        public class result
        {
            public string service_version { get; set; }
            public string sign { get; set; }
        }

        public class CallBackCreditReq : result
        {
            public string billno { get; set; }
            public string partner_orderid { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string account_name { get; set; }
            public string account_number { get; set; }
            public string bank_code { get; set; }
            public string status { get; set; }
            public string fee { get; set; }
            public string bank_charge { get; set; }
            public string error_code { get; set; }
            public string error_description { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackCreditReq>(ToJson(formData));

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.partner_orderid;

            if (data.status == "000")
            {
                oNotify.TransStatus = TransStatus.Success;
                oNotify.RequestAmount = decimal.Parse(data.amount);
                oNotify.ThirdId = data.billno;
                oNotify.Message = data.status;
                oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
            }
            else if (data.status == "001")
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.ThirdId = data.billno;
                oNotify.Message = data.status;
                oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
            }
            else
            {
                oNotify.TransStatus = TransStatus.Fail;
                oNotify.ThirdId = data.billno;
                oNotify.Message = data.status;
                oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
            }

            return oNotify;
        }

        public class CallBackDebitReq : result
        {
            public string billno { set; get; }
            public string partner_orderid { set; get; }
            public string currency { set; get; }
            public string amount { set; get; }
            public string status { set; get; }
        }
        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var merchant_no = payResource["merchant_no"];
            var key = payResource["key"];
            var amount = decimal.Parse(data.amount) / 100;
            var p = $"service_version={data.service_version}&billno={data.billno}&partner_orderid={data.partner_orderid}&currency={data.currency}&amount={data.amount}&status={data.status}&key={key}";
            var sign = ToSHA1(p).ToUpper();

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.partner_orderid;
            if (sign == data.sign)
            {
                if (data.status == "000" || data.status == "002")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = amount;
                    oNotify.ActualAmount = amount;
                    oNotify.ThirdId = data.billno;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
                }
                else if (data.status == "001")
                {
                    oNotify.TransStatus = TransStatus.Pending;
                    oNotify.ThirdId = data.billno;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.ThirdId = data.billno;
                    oNotify.Comment = data.status;
                    oNotify.Message = data.status;
                    oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = $"<xml><billno>{data.billno}</billno><status>OK</status></xml>";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private class CreditReq : result
        {
            public string partner_code { get; set; }
            public string partner_orderid { get; set; }
            public string member_id { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string account_name { get; set; }
            public string account_number { get; set; }
            public string bank_province { get; set; }
            public string bank_city { get; set; }
            public string bank_branch { get; set; }
            public string bank_code { get; set; }
            public string notify_url { get; set; }
        }


        [XmlRoot("xml")]
        public class Payout : result
        {
            public string billno { set; get; }
            public string partner_orderid { set; get; }
            public string currency { set; get; }
            public string amount { set; get; }
            public string bank_code { set; get; }
            public string status { set; get; }
            public string fee { set; get; }
            public string bank_charge { set; get; }
            public string error_code { set; get; }
            public string error_description { set; get; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var creditReq = new CreditReq();
            creditReq.service_version = "2.1";
            creditReq.partner_code = inputs["merchant_no"];
            creditReq.partner_orderid = inputs["TransID"];
            creditReq.member_id = inputs["CustId"];
            creditReq.currency = inputs["PayCurrencyId"];
            var amount = decimal.Parse(inputs["Amount"]) * 100;
            creditReq.amount = amount.ToString("#");
            creditReq.account_name = inputs["AccountName"];
            creditReq.account_number = inputs["AccountNo"];

            creditReq.bank_province = bankDic[creditReq.currency];
            creditReq.bank_city = bankDic[creditReq.currency];
            creditReq.bank_branch = bankDic[creditReq.currency];
            creditReq.bank_code = inputs["BankCode"];
            creditReq.notify_url = inputs["CallBackUrl"];

            var p = $"service_version={creditReq.service_version}&partner_code={creditReq.partner_code}&partner_orderid={creditReq.partner_orderid}&" +
                    $"member_id={creditReq.member_id}&currency={creditReq.currency}&amount={creditReq.amount}&account_name={creditReq.account_name}&" +
                    $"account_number={creditReq.account_number}&bank_province={creditReq.bank_province}&bank_city={creditReq.bank_city}&bank_branch={creditReq.bank_branch}&" +
                    $"bank_code={creditReq.bank_code}&notify_url={creditReq.notify_url}&key={inputs["key"]}";
            creditReq.sign = ToSHA1(p).ToUpper();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(creditReq));
            var sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPost(sUrl, sParam, "application/x-www-form-urlencoded", creditReq.partner_orderid);

            var withdrawResult = new WithdrawResult();
            var xmlSerializer = new XmlSerializer(typeof(Payout));
            using var reader = new System.IO.StringReader(sResponse);
            var payout = (Payout)xmlSerializer.Deserialize(reader);
            if (payout.status == "000" || payout.status == "001")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = payout.error_description;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = payout.error_description;
            }

            return withdrawResult;
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>
            {
                { "service_version", "2.1" },
                { "partner_code", inputs["merchant_no"] },
                { "currency", inputs["PayCurrencyId"] },
                { "partner_orderid", inputs["TransID"] }
            };
            var amount = decimal.Parse(inputs["Amount"]) * 100;
            formData.Add("amount", amount.ToString("#"));
            var time = new DateTimeOffset(DateTime.Now);
            formData.Add("trans_time", time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            formData.Add("redirect_url", inputs["ReturnUrl"]);
            formData.Add("backend_url", inputs["CallBackUrl"]);
            if (!string.IsNullOrEmpty(inputs["PayCode"]))
                formData.Add("bank_code", inputs["PayCode"]);
            else
                formData.Add("bank_code", inputs["BankCode"]);
            formData.Add("member_ip", "");
            formData.Add("member_id", inputs["CustId"]);

            var p = $"service_version={formData["service_version"]}&partner_code={formData["partner_code"]}&partner_orderid={formData["partner_orderid"]}&" +
                    $"member_id={formData["member_id"]}&member_ip={formData["member_ip"]}&currency={formData["currency"]}&amount={formData["amount"]}&" +
                    $"backend_url={formData["backend_url"]}&redirect_url={formData["redirect_url"]}&bank_code={formData["bank_code"]}&trans_time={formData["trans_time"]}&key={inputs["key"]}";
            formData.Add("sign", ToSHA1(p).ToUpper());

            var oDepositResult = new DepositResult();
            oDepositResult.Url = sUrl;
            oDepositResult.FormData = formData;
            oDepositResult.Success = true;
            oDepositResult.Message = "";
            return oDepositResult;
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
