using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;

namespace PaymentCore
{
    public enum TransStatus
    {
        Pending,
        Success,
        Fail
    }

    public class ExchangeRate
    {
        public long Updated_timestamp { get; set; }
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
    }

    public class ExchangeResult
    {
        public ExchangeResult()
        {
        }

        public ExchangeResult(string errorCode)
            : this(errorCode, null, null)
        {
        }

        public ExchangeResult(string errorCode, string message)
            : this(errorCode, message, null)
        {
        }

        public ExchangeResult(string errorCode, string message, Dictionary<string, string> formData)
        {
            this.ErrorCode = errorCode;
            this.Message = message;
            this.FormData = formData;
        }

        public string Message { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public string ErrorCode { get; set; }
    }

    public class DepositResult
    {
        public DepositResult()
        {
        }

        public DepositResult(bool success, string errorCode, string message = "")
        {
            this.Success = success;
            this.ErrorCode = errorCode;
            this.Message = message;
        }
        
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal SuggestAmount { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public string ThirdId { get; set; }
        public string Memo { get; set; }
        public string RawContent { get; set; }
        /// <summary>
        /// 需要更新 DepositInfo.DepositInfo 時使用此欄位
        /// </summary>
        public string DepositInfo { get; set; }
    }
    public class WithdrawResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string WithdrawId { get; set; }
        public decimal Amount { get; set; }
        public string RawContent { get; set; }
        /// <summary>
        /// 需要更新 WithdrawalInfo.WithdrawalInfo 時使用此欄位
        /// </summary>
        public string WithdrawalInfo { get; set; }
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
    public interface IExchangeHelper
    {
        //ExchangeResult GetExchangeAsync(Dictionary<string, string> payResource);
        //public System.Threading.Tasks.Task<ExchangeResult> GetExchangeAsync(Dictionary<string, string> payResource);
        public ExchangeResult GetExchange(Dictionary<string, string> payResource);
    }
    public interface IPayHelperNoCallBackUrl
    {
        string FetchTransId(Dictionary<string, string> formData, string bodyContent );
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
        string RefThirdId { get; set; }
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
        public string RefThirdId { get; set; }
    }
    public interface IWithdrawResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string WithdrawId { get; set; }
        public decimal RequestAmount { get; set; }
        public string ThirdId { get; set; }
    }
    public class EnquiryWithdrawResult : IWithdrawResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string WithdrawId { get; set; }
        public decimal RequestAmount { get; set; }
        public string ThirdId { get; set; }
        public string RawContent { get; set; }
    }
    public class NotifyDepositResult : IDepositResult
    {
        public TransStatus TransStatus { get; set; }
        public string Message { get; set; }
        public string DepositId { get; set; }
        public string RefThirdId { get; set; }
        public string ThirdId { get; set; }
        public decimal RequestAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Fee { get { return RequestAmount - ActualAmount; } }
        public string Comment { get; set; }
        public string Response { get; set; }
    }
    public class NotifyWithdrawResult : IWithdrawResult
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
        public static IPayHttpHelper HttpHelper { get; set; }
        public static IPaymentHelper PaymentHelper { get; set; }
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
        public static string proxyHost;
        public static int proxyPort;

        protected string HttpGet(string url, string parameters, string transID, bool ignoreCertificate = false)
        {
            var request = WebRequest.CreateHttp($"{url}?{parameters}");
            request.Method = "GET";
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            if (ignoreCertificate)
            {
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            }

            using var response = request.GetResponse();
            using var dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();

            InsertPaymentLog?.Invoke(url, parameters, responseFromServer, transID);

            return responseFromServer;
        }
        
        protected string HttpPost(string url, string parameters, string contentType, string transId, bool ignoreCertificate = false)
        {
            var request = WebRequest.CreateHttp(url);

            request.Method = "POST";
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            var postData = parameters;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;
            if (ignoreCertificate)
            {
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            }

            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response;

            try
            {
                response = request.GetResponse();
            }
            catch (WebException e)
            {
                response = (HttpWebResponse)e.Response;
            }

            dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            InsertPaymentLog?.Invoke(url, parameters, responseFromServer, transId);

            return responseFromServer;
        }

        protected string HttpPost(string url, string parameters, string contentType, string accesstoken, string transID, bool ignoreCertificate)
        {
            var request = WebRequest.CreateHttp(url);

            request.Method = "POST";
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            var postData = parameters;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;
            if (ignoreCertificate)
            {
                request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            }
            request.Headers.Add("Authorization", "Bearer " + accesstoken);
            request.Headers.Add("accept", contentType);
            request.Headers.Add("x-api-key", accesstoken);

            InsertPaymentLog?.Invoke(url, $"request: {parameters}, " +
                                          $"Headers: {ToJson(request.Headers)}", string.Empty, transID);

            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            using var response = request.GetResponse();

            dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            InsertPaymentLog?.Invoke(url, parameters, responseFromServer, transID);

            return responseFromServer;
        }
        protected string HttpPost(string url, string parameters, string contentType, string accesstoken, string transID)
        {
            var request = WebRequest.Create(url);

            request.Method = "POST";
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            var postData = parameters;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;
            request.Headers.Add("Authorization", "Bearer " + accesstoken);
            request.Headers.Add("accept", contentType);
            request.Headers.Add("x-api-key", accesstoken);

            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            using var response = request.GetResponse();

            dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            InsertPaymentLog?.Invoke(url, parameters, responseFromServer, transID);

            return responseFromServer;
        }
        protected string HttpPost(string url, string parameters, string contentType, string username, string password, string transID , bool isFoxPay)
        {
            var request = WebRequest.Create(url);

            request.Method = "POST";
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            var postData = parameters;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = contentType;
            if (!isFoxPay)
            {
                request.ContentLength = byteArray.Length;
            }
            
            var svcCredentials = username;
            if (!string.IsNullOrEmpty(password))
            {
                svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
            }
            
            request.Headers.Add("Authorization", "Basic " + svcCredentials);
            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            using var response = request.GetResponse();

            dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            InsertPaymentLog?.Invoke(url, parameters, responseFromServer, transID);

            return responseFromServer;
        }
        protected string HttpPostGBK(string url, string parameters, string contentType, string transID)
        {
            var request = WebRequest.Create(url);
            request.Method = "POST";
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            var postData = parameters;
            var byteArray = UTF8ToGBK(parameters);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;

            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            using var response = request.GetResponse();

            dataStream = response.GetResponseStream();
            using var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();
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
            var responseBody = "";

            using (var client = new HttpClient())
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
            using (var cryptoMD5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(encodeStr);
                var hash = cryptoMD5.ComputeHash(bytes);
                var md5 = BitConverter.ToString(hash)
                  .Replace("-", String.Empty);

                return md5;
            }
        }

        protected string ToSHA1(string encodeStr)
        {
            using (var cryptosha1 = new SHA1CryptoServiceProvider())
            {
                var bytes = Encoding.UTF8.GetBytes(encodeStr);
                var hash = cryptosha1.ComputeHash(bytes);
                var sha1 = BitConverter.ToString(hash)
                  .Replace("-", String.Empty);

                return sha1;
            }
        }

        protected string ToHMACSHA256(string encodeStr, string key)
        {
            var encoding = new System.Text.UTF8Encoding();
            var keyByte = encoding.GetBytes(key);
            var messageBytes = encoding.GetBytes(encodeStr);
            using (var hmacSHA256 = new HMACSHA256(keyByte))
            {
                var hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
            }
        }

        // To Base64
        public static string ToHMACSHA256Base64(string message, string secretKey)
        {
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            var cryptographer = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var bytes = cryptographer.ComputeHash(messageBytes);
            var base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
            return base64String;
        }


        protected string ToSHA256(string encodeStr)
        {
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = SHA256Managed.Create().ComputeHash(bytes);

            var builder = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("X2"));
            }

            return builder.ToString();
        }

        protected string ToSHA512(string encodeStr)
        {
            var hash = SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(encodeStr));
            var result = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                result.Append(b.ToString("X2"));
            }
            return result.ToString();
        }

        protected string ToHMACSMD5(string encodeStr, string key)
        {
            var encoding = new System.Text.UTF8Encoding();
            var keyByte = encoding.GetBytes(key);
            var messageBytes = encoding.GetBytes(encodeStr);
            using (var hmacMD5 = new HMACMD5(keyByte))
            {
                var hashMessage = hmacMD5.ComputeHash(messageBytes);
                return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
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
            var outStr = "";
            var reg = new Regex(@"(?i)\\u([0-9a-f]{4})");
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
                var result = str.Substring(str.IndexOf('.') + 1, str.Length - str.IndexOf('.') - 1);
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
        protected byte[] UTF8ToGBK(string str)
        {
            var GBK = Encoding.GetEncoding("GB2312"); // GBK 编码
            Encoding UTF8 = new UTF8Encoding(true); // UTF-8 编码

            var utf8bytes = Encoding.UTF8.GetBytes(str);
            var gbkbytes = Encoding.Convert(UTF8, GBK, utf8bytes);

            return gbkbytes;
        }

        protected string GBKToUTF8(string str)
        {
            var GBK = Encoding.GetEncoding("GB2312"); // GBK 编码
            Encoding UTF8 = new UTF8Encoding(true); // UTF-8 编码

            var gbkbytes = GBK.GetBytes(str);
            var utf8bytes = Encoding.Convert(GBK, UTF8, gbkbytes);
            var toString = UTF8.GetString(utf8bytes);

            return toString;
        }

        protected string UrlEncode(string str)
        {
            var builder = new StringBuilder();
            foreach (var c in str)
            {
                if (HttpUtility.UrlEncode(c.ToString()).Length > 1)
                {
                    builder.Append(HttpUtility.UrlEncode(c.ToString()).ToUpper());
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        protected string ToBase64Encode(string str)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(str);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        protected string ToBase64Decode(string str)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(str);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        protected string AddPrivateHeader(string text)
        {
            return $"-----BEGIN RSA PRIVATE KEY-----\n{text}\n-----END RSA PRIVATE KEY-----";
        }

        protected string AddPublicHeader(string text)
        {
            return $"-----BEGIN PUBLIC KEY-----\n{text}\n-----END PUBLIC KEY-----";
        }

        protected static string RSAPublicKeyJava2DotNet(string publicKey)
        {
            var publicKeyParam = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
            return DotNetUtilities.ToRSA(publicKeyParam).ToXmlString(false);
        }

        protected static string RSAPrivateKeyJava2DotNet(string privateKey)
        {
            var privateKeyParam = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
            return DotNetUtilities.ToRSA(privateKeyParam).ToXmlString(true);
        }
        
        protected string RSAEncryptWithPublicKey(string publickey, string content)
        {
            var rsa = new RSACryptoServiceProvider();
            //byte[] cipherbytes;
            rsa.FromXmlString(publickey);
            var rp = rsa.ExportParameters(false);

            //转换密钥
            AsymmetricKeyParameter pbk = DotNetUtilities.GetRsaPublicKey(rp);

            var c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
            //第一个参数为true表示加密，为false表示解密；第二个参数表示密钥
            c.Init(true, pbk);

            var DataToEncrypt = Encoding.UTF8.GetBytes(content);
            var outBytes = c.DoFinal(DataToEncrypt);
            return Convert.ToBase64String(outBytes);
        }

        protected string RSAEncryptWithPrivateKey(string privateKey, string content)
        {
            //加载私钥
            var privateRsa = new RSACryptoServiceProvider();
            privateRsa.FromXmlString(privateKey);

            //转换密钥
            var keyPair = DotNetUtilities.GetKeyPair(privateRsa);
            var c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding"); //使用RSA/ECB/PKCS1Padding格式

            //第一个参数为true表示加密，为false表示解密；第二个参数表示密钥
            c.Init(true, keyPair.Private);
            var DataToEncrypt = Encoding.UTF8.GetBytes(content);
            var outBytes = c.DoFinal(DataToEncrypt);//加密
            return Convert.ToBase64String(outBytes);
        }

        protected string RSADecryptWithPublicKey(string publicKey, string content)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            var rp = rsa.ExportParameters(false);

            //转换密钥
            AsymmetricKeyParameter pbk = DotNetUtilities.GetRsaPublicKey(rp);

            var c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");

            //第一个参数为true表示加密，为false表示解密；第二个参数表示密钥
            c.Init(false, pbk);

            var DataToDecrypt = Convert.FromBase64String(content);
            var outBytes = c.DoFinal(DataToDecrypt);//解密
            return Encoding.UTF8.GetString(outBytes);
        }

        protected string RSADecryptWithPrivateKey(string privatekey, string content)
        {
            //加载私钥
            var privateRsa = new RSACryptoServiceProvider();
            privateRsa.FromXmlString(privatekey);

            //转换密钥
            var keyPair = DotNetUtilities.GetKeyPair(privateRsa);
            var c = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding"); //使用RSA/ECB/PKCS1Padding格式

            //第一个参数为true表示加密，为false表示解密；第二个参数表示密钥
            c.Init(false, keyPair.Private);
            var DataToEncrypt = Convert.FromBase64String(content);
            var outBytes = c.DoFinal(DataToEncrypt);//加密
            return Encoding.UTF8.GetString(outBytes);
        }
        
        protected string AES128CBCDecrypt(string key , string iv, string content)
        { 
            var keyArray = Encoding.UTF8.GetBytes(key);
            var ivArray = Encoding.UTF8.GetBytes(iv);
            var cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7");
            cipher.Init(false, new ParametersWithIV(new KeyParameter(keyArray), ivArray));
            var encData = Convert.FromBase64String(content);
            
            return Encoding.UTF8.GetString(cipher.DoFinal(encData));
        }

        protected string AES128CBCEncrypt(string key , string iv, string content)
        { 
            var keyArray = Encoding.UTF8.GetBytes(key);
            var ivArray = Encoding.UTF8.GetBytes(iv);
            var cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7");
            cipher.Init(true, new ParametersWithIV(new KeyParameter(keyArray), ivArray));
            var DataToEncrypt = Encoding.UTF8.GetBytes(content);
            var outBytes = cipher.DoFinal(DataToEncrypt);//加密
            return Convert.ToBase64String(outBytes);
        }
        
        protected string currMapping(string currencyId)
        {
            var currMapping = new Dictionary<string, string>
            {
                {"2", "MYR"},
                {"4", "THB"},
                {"13", "CNY"},
                {"15", "IDR"},
                {"51", "VND"},
                {"61", "INR"}
            };

            if (currMapping.ContainsKey(currencyId))
            {

                return currMapping[currencyId];
             }
            else
            {
                return ""; 
            }
        }
    }
}
