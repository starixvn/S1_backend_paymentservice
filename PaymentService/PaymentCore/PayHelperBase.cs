using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace PaymentCore
{
    public enum TransStatus
    {
        Pending,
        Success,
        Fail
    }
    public class DepositResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SuggestAmount { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public string ThirdId { get; set; }
        public string Memo { get; set; }
        public string RawContent { get; set; }
    }
    public class WithdrawResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string WithdrawId { get; set; }
        public decimal Amount { get; set; }
        public string RawContent { get; set; }
    }

    public delegate void InsertPaymentLogEventHandler(string url, string input, string output, string transID);
    public delegate void ErrorLogEventHandler(string log);
    public interface IPayHelper
    {
        DepositResult Debit(Dictionary<string, string> inputs);
        WithdrawResult Credit(Dictionary<string, string> inputs);
        Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs);

        event InsertPaymentLogEventHandler InsertPaymentLog;
        EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs);
        EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs);
        NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource);
        NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource);

    }

    public interface IDepositResult
    {
        TransStatus TransStatus { get; set; }
        string Message { get; set; }
        string DepositId { get; set; }
        string ThirdId { get; set; }
        decimal RequestAmount { get; set; }
        decimal ActualAmount { get; set; }
        decimal Fee { get; }
        string Comment { get; set; }
    }
    public class EnquiryDepositResult : IDepositResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string DepositId { get; set; }
        public string ThirdId { get; set; }
        public decimal RequestAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Fee { get { return RequestAmount - ActualAmount; } }
        public string Comment { get; set; }
        public string RawContent { get; set; }
    }
    public class EnquiryWithdrawResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string WithdrawId { get; set; }
        public decimal Amount { get; set; }
        //public decimal Fee { get { return OrderAmount - FactAmount; } }
        public string RawContent { get; set; }
    }
    public class NotifyDepositResult : IDepositResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string DepositId { get; set; }
        public string ThirdId { get; set; }
        public decimal RequestAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Fee { get { return RequestAmount - ActualAmount; } }
        public string Comment { get; set; }
        public string Response { get; set; }
    }
    public class NotifyWithdrawResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string WithdrawId { get; set; }
        public decimal RequestAmount { get; set; }
        //public decimal Fee { get { return OrderAmount - FactAmount; } }
        public string ThirdId { get; set; }
        public string Response { get; set; }
    }
    public abstract class PayHelperBase : IPayHelper
    {
        public event InsertPaymentLogEventHandler InsertPaymentLog;

        public abstract DepositResult Debit(Dictionary<string, string> inputs);
        public abstract WithdrawResult Credit(Dictionary<string, string> inputs);
        public abstract Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs);
        public abstract EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs);
        public abstract EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs);
        public abstract NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource);
        public abstract NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource);

        /// <summary>
        /// 1. 會夾帶Content-Type
        /// 2. 簡體字會變成亂碼，且無法還原
        /// </summary>
        /// 
        private readonly string proxyHost;
        private readonly int proxyPort;
        public PayHelperBase(string proxyHost = null, int proxyPort = 0)
        {
            this.proxyHost = proxyHost;
            this.proxyPort = proxyPort;
        }
        protected string HttpPost(string url, string parameters, string contentType, string transID)
        {
            WebRequest request = WebRequest.Create(url);

            request.Method = "POST";
            if (string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            string postData = parameters;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            using WebResponse response = request.GetResponse();

            dataStream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            InsertPaymentLog?.Invoke(url, parameters, responseFromServer, transID);

            return responseFromServer;
        }

        public void DoInsertPaymentLog(string url, string input, string output, string SerialID)
        {
            InsertPaymentLog?.Invoke(url, input, output, SerialID);
        }

        /// <summary>
        /// 1. 不會夾帶Content-Type
        /// 2. 簡體字可正常顯示
        /// </summary>
        protected async System.Threading.Tasks.Task<string> HttpPostAsync(string url, Dictionary<string, string> parameters, string SerialID)
        {
            string responseBody = "";

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                var content = new FormUrlEncodedContent(parameters);
                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                responseBody = await response.Content.ReadAsStringAsync();

                InsertPaymentLog?.Invoke(url, string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}").ToArray()), responseBody, SerialID);
            }

            return responseBody;
        }

        protected string ToMD5(string encodeStr)
        {
            using (var cryptoMD5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(encodeStr);
                var hash = cryptoMD5.ComputeHash(bytes);
                var md5 = BitConverter.ToString(hash)
                  .Replace("-", String.Empty);

                return md5;
            }
        }
        protected string GetRandomString4(int length)
        {
            var str = "0123456789abcdefghijklmnopqrstuvwxyz";//ABCDEFGHIJKLMNOPQRSTUVWXYZ
            var next = new Random();
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }
            return builder.ToString();
        }
        protected int GetTimestemp()
        {
            return Convert.ToInt32((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
        }
        protected string unicode_js_1(string str)
        {
            string outStr = "";
            Regex reg = new Regex(@"(?i)\\u([0-9a-f]{4})");
            try
            {
                outStr = reg.Replace(str, delegate (Match m1)
                {
                    return ((char)Convert.ToInt32(m1.Groups[1].Value, 16)).ToString();
                });
            }
            catch (Exception ex)
            {

            }
            return outStr;
        }

        protected T FromJson<T>(string jsonString)
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                return default(T);
            }
        }
        protected string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
        protected bool IsJSON(string jsonString)
        {
            var value = jsonString.Trim();

            return (value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]"));
        }

        /// <summary>
        /// 移除小數點後多餘的0
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected decimal RemoveZeroAfterPoint(decimal value)
        {
            return value / 1.000000000000000000000000000000000m;
        }

        protected int LengthAfterPoint(string str)
        {

            if (str.IndexOf('.') > 0 && str.IndexOf('.') != str.Length)
            {
                string result = str.Substring(str.IndexOf('.') + 1, str.Length - str.IndexOf('.') - 1);
                if (result == "")
                    return 0;
                else if (int.Parse(result) == 0)
                    return 0;
                else
                    return result.Length;
            }
            else
            {
                return 0;
            }
        }
    }

}
