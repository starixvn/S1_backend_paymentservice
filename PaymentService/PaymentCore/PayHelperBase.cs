using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Web;
using Org.BouncyCastle.Security;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using PaymentCore.Interface;
using PaymentCore.Models.Result;
using Serilog.Context;
using System.Text.Json.Nodes;

namespace PaymentCore
{
    public delegate void InsertPaymentLogEventHandler(string url, string input, string output, string transId);

    public abstract class PayHelperBase : IPayHelper
    {
        // Startup 的時候 Setter
        public static string ProxyHost;

        // Startup 的時候 Setter
        public static int ProxyPort;

        public static IPaymentHelper PaymentHelper;

        public event InsertPaymentLogEventHandler InsertPaymentLog;
        public abstract DepositResult Debit(Dictionary<string, string> inputs);
        public abstract WithdrawResult Credit(Dictionary<string, string> inputs);
        public abstract Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs);
        public abstract EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs);
        public abstract EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs);
        public abstract NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource);
        public abstract NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource);

        protected string HttpGet(string url, string parameters, string transId, bool ignoreCertificate = false)
        {
            try
            {
                var requestUri = $"{url}?{parameters}";
                var response = HttpClientSend(httpMethod: HttpMethod.Get,
                    content: string.Empty,
                    requestUri: requestUri,
                    contentType: string.Empty,
                    ignoreCertificate: ignoreCertificate);

                LogInformationProxy(url, parameters, response, transId);

                return response;
            }
            catch (Exception ex)
            {
                LogInformationProxy(url, parameters, ex.Message, transId);
                throw;
            }
        }

        protected string HttpPost(string url, string parameters, string contentType, string transId, bool ignoreCertificate = false)
        {
            try
            {
                var response = HttpClientSend(httpMethod: HttpMethod.Post,
                    content: parameters,
                    requestUri: url,
                    contentType: contentType,
                    ignoreCertificate: ignoreCertificate);

                LogInformationProxy(url, parameters, response, transId);

                return response;
            }
            catch (Exception ex)
            {
                LogInformationProxy(url, parameters, ex.Message, transId);
                throw;
            }
        }

        protected string HttpPost(string url, string parameters, string contentType, string accessToken, string transId, bool ignoreCertificate)
        {
            try
            {
                var httpHeaders = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer " + accessToken },
                    { "accept", contentType },
                    { "x-api-key", accessToken }
                };

                LogInformationProxy(url, $"request: {parameters}, " +
                                $"Headers: {ToJson(httpHeaders)}", string.Empty, transId);

                var response = HttpClientSend(httpMethod: HttpMethod.Post,
                    content: parameters,
                    requestUri: url,
                    contentType: contentType,
                    ignoreCertificate: ignoreCertificate,
                    httpHeaders: httpHeaders);

                LogInformationProxy(url, parameters, response, transId);

                return response;
            }
            catch (Exception ex)
            {
                LogInformationProxy(url, parameters, ex.Message, transId);
                throw;
            }
        }

        protected string HttpPost(string url, string parameters, string contentType, string username, string password, string transId)
        {
            try
            {
                var svcCredentials = username;
                if (!string.IsNullOrEmpty(password))
                {
                    svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                }

                var httpHeaders = new Dictionary<string, string>
                {
                    { "Authorization", "Basic " + svcCredentials }
                };

                LogInformationProxy(url, $"request: {parameters}, " +
                                $"Headers: {ToJson(httpHeaders)}", string.Empty, transId);

                var response = HttpClientSend(httpMethod: HttpMethod.Post,
                    content: parameters,
                    requestUri: url,
                    contentType: contentType,
                    ignoreCertificate: false,
                    httpHeaders: httpHeaders);

                LogInformationProxy(url, parameters, response, transId);

                return response;
            }
            catch (Exception ex)
            {
                LogInformationProxy(url, parameters, ex.Message, transId);
                throw;
            }
        }

        protected string HttpPostGBK(string url, string parameters, string contentType, string transId)
        {
            try
            {
                var gBkEncoding = Encoding.GetEncoding("GB2312"); // GBK 编码
                var response = HttpClientSend(httpMethod: HttpMethod.Post,
                    content: UTF8StrToGBKStr(parameters),
                    requestUri: url,
                    contentType: contentType,
                    ignoreCertificate: false,
                    encoding: gBkEncoding);

                LogInformationProxy(url, parameters, response, transId);

                return response;
            }
            catch (Exception ex)
            {
                LogInformationProxy(url, parameters, ex.Message, transId);
                throw;
            }

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

        protected string ToHMACSHA256(string encodeStr)
        {
            var SHA256 = SHA256Managed.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = SHA256.ComputeHash(bytes);
            var result = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString().ToLower();
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

        protected static T FromJson<T>(string jsonString)
        {
            return !string.IsNullOrEmpty(jsonString)
                ? JsonConvert.DeserializeObject<T>(jsonString)
                : default;
        }

        protected string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        // 先保留
        protected byte[] UTF8ToGBK(string str)
        {
            var GBK = Encoding.GetEncoding("GB2312"); // GBK 编码
            Encoding UTF8 = new UTF8Encoding(true); // UTF-8 编码

            var utf8bytes = Encoding.UTF8.GetBytes(str);
            var gbkbytes = Encoding.Convert(UTF8, GBK, utf8bytes);

            return gbkbytes;
        }

        protected string UTF8StrToGBKStr(string str)
        {
            var GBK = Encoding.GetEncoding("GB2312"); // GBK 编码
            Encoding UTF8 = new UTF8Encoding(true); // UTF-8 编码

            var utf8bytes = Encoding.UTF8.GetBytes(str);
            var gbkbytes = Encoding.Convert(UTF8, GBK, utf8bytes);
            var toString = GBK.GetString(utf8bytes);

            return toString;
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

        protected static string RSAPrivateKeyJava2DotNet(string privateKey)
        {
            var privateKeyParam = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
            return DotNetUtilities.ToRSA(privateKeyParam).ToXmlString(true);
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

        protected string RSADecryptWithPrivateKey(string privateKey, string content)
        {
            //加载私钥
            var privateRsa = new RSACryptoServiceProvider();
            privateRsa.FromXmlString(privateKey);

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

        #region rsa

        /// <summary>
        /// RSA签名
        /// </summary>
        /// <param name="content">数据</param>
        /// <param name="privateKey">RSA密钥</param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string RsaSign(string content, string privateKey, HashAlgorithmName? name = null)
        {
            if (name == null)
            {
                name = HashAlgorithmName.MD5;
            }

            var privateKeyBytes = Convert.FromBase64String(privateKey);
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

                var data = Encoding.UTF8.GetBytes(content);

                var signatureBytes = rsa.SignData(data, (HashAlgorithmName)name, RSASignaturePadding.Pkcs1);

                return Convert.ToBase64String(signatureBytes);
            }
        }

        /// <summary>
        /// 公钥加密
        /// </summary>
        /// <param name="dataToEncrypt">待加密文本</param>
        /// <param name="publicKeyString"></param>
        protected static string RsaEncryptWithPublic(string dataToEncrypt, string publicKeyString)
        {
            var rsa = CreateRsaProviderFromPublicKey(publicKeyString);

            using (var RSACryptography = rsa)
            {
                Byte[] PlaintextData = Encoding.UTF8.GetBytes(dataToEncrypt);
                int MaxBlockSize = RSACryptography.KeySize / 8 - 11;    //加密块最大长度限制

                if (PlaintextData.Length <= MaxBlockSize)
                    return Convert.ToBase64String(RSACryptography.Encrypt(PlaintextData, RSAEncryptionPadding.Pkcs1));

                using (MemoryStream PlaiStream = new MemoryStream(PlaintextData))
                using (MemoryStream CrypStream = new MemoryStream())
                {
                    Byte[] Buffer = new Byte[MaxBlockSize];
                    int BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);


                    while (BlockSize > 0)
                    {
                        Byte[] ToEncrypt = new Byte[BlockSize];
                        Array.Copy(Buffer, 0, ToEncrypt, 0, BlockSize);


                        Byte[] Cryptograph = RSACryptography.Encrypt(ToEncrypt, RSAEncryptionPadding.Pkcs1);
                        CrypStream.Write(Cryptograph, 0, Cryptograph.Length);


                        BlockSize = PlaiStream.Read(Buffer, 0, MaxBlockSize);
                    }


                    return Convert.ToBase64String(CrypStream.ToArray());
                }
            }
        }

        /// <summary>
        /// 私钥解密
        /// </summary>
        /// <param name="context"></param>
        /// <param name="privateKey"></param>
        /// <param name="length">1024位证书解密最大字节为128,2048是256</param>
        /// <returns></returns>
        public static string RsaDecryptWithPrivate(string context, string privateKey, int length = 128)
        {
            var rsa = CreateRsaProviderFromPrivateKey(privateKey);
            var cipherBytes = Convert.FromBase64String(context);

            byte[] decryptedData;
            using (var plaiStream = new MemoryStream(cipherBytes))
            {
                using (var decrypStream = new MemoryStream())
                {
                    var offSet = 0;
                    var inputLen = cipherBytes.Length;
                    for (var i = 0; inputLen - offSet > 0; offSet = i * length)
                    {
                        if (inputLen - offSet > length)
                        {
                            var buffer = new Byte[length];
                            plaiStream.Read(buffer, 0, length);
                            var decrypData = rsa.Decrypt(buffer, RSAEncryptionPadding.Pkcs1);
                            decrypStream.Write(decrypData, 0, decrypData.Length);
                        }
                        else
                        {
                            var buffer = new Byte[inputLen - offSet];
                            plaiStream.Read(buffer, 0, inputLen - offSet);
                            var decrypData = rsa.Decrypt(buffer, RSAEncryptionPadding.Pkcs1);
                            decrypStream.Write(decrypData, 0, decrypData.Length);
                        }
                        ++i;
                    }
                    decrypStream.Position = 0;
                    decryptedData = decrypStream.ToArray();
                }
            }

            var plainText = Encoding.UTF8.GetString(decryptedData);


            return plainText;
        }

        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        protected static string ByteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        protected static RSA CreateRsaProviderFromPublicKey(string publicKeyString)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            var seq = new byte[15];

            var x509Key = Convert.FromBase64String(publicKeyString);

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            using (var mem = new MemoryStream(x509Key))
            {
                using (var binr = new BinaryReader(mem))  //wrap Memory Stream with BinaryReader for easy reading
                {
                    byte bt = 0;
                    ushort twobytes = 0;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binr.ReadByte();    //advance 1 byte
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    seq = binr.ReadBytes(15);       //read the Sequence OID
                    if (!CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
                        binr.ReadByte();    //advance 1 byte
                    else if (twobytes == 0x8203)
                        binr.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    bt = binr.ReadByte();
                    if (bt != 0x00)     //expect null byte next
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                        binr.ReadByte();    //advance 1 byte
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();   //advance 2 bytes
                    else
                        return null;

                    twobytes = binr.ReadUInt16();
                    byte lowbyte = 0x00;
                    byte highbyte = 0x00;

                    if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
                        lowbyte = binr.ReadByte();  // read next bytes which is bytes in modulus
                    else if (twobytes == 0x8202)
                    {
                        highbyte = binr.ReadByte(); //advance 2 bytes
                        lowbyte = binr.ReadByte();
                    }
                    else
                        return null;
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                    var modsize = BitConverter.ToInt32(modint, 0);

                    var firstbyte = binr.PeekChar();
                    if (firstbyte == 0x00)
                    {   //if first byte (highest order) of modulus is zero, don't include it
                        binr.ReadByte();    //skip this null byte
                        modsize -= 1;   //reduce modulus buffer size by 1
                    }

                    var modulus = binr.ReadBytes(modsize);   //read the modulus bytes

                    if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
                        return null;
                    var expbytes = (int)binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                    var exponent = binr.ReadBytes(expbytes);

                    // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                    var rsa = RSA.Create();
                    var rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);

                    return rsa;
                }

            }
        }

        protected static RSA CreateRsaProviderFromPrivateKey(string privateKey)
        {
            var privateKeyBits = Convert.FromBase64String(privateKey);

            var rsa = RSA.Create();
            var rsaParameters = new RSAParameters();

            using (var binr = new BinaryReader(new MemoryStream(privateKeyBits)))
            {
                byte bt = 0;
                ushort twobytes = 0;
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)
                    binr.ReadByte();
                else if (twobytes == 0x8230)
                    binr.ReadInt16();
                else
                    throw new Exception("Unexpected value read binr.ReadUInt16()");

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)
                    throw new Exception("Unexpected version");

                bt = binr.ReadByte();
                if (bt != 0x00)
                    throw new Exception("Unexpected value read binr.ReadByte()");

                rsaParameters.Modulus = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Exponent = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.D = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.P = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Q = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DP = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DQ = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
            }

            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        protected static bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            var i = 0;
            foreach (var c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        protected static int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            var count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();
            else
            if (bt == 0x82)
            {
                var highbyte = binr.ReadByte();
                var lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;
            }

            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        #endregion

        protected string CurrencyMapping(string currencyId)
        {
            var currencyMapping = new Dictionary<string, string>
            {
                {"2", "MYR"},
                {"4", "THB"},
                {"13", "CNY"},
                {"15", "IDR"},
                {"51", "VND"},
                {"61", "INR"},
                {"74", "BDT"}
            };

            return currencyMapping.ContainsKey(currencyId) ? currencyMapping[currencyId] : "";
        }
        
        protected static Dictionary<string, string> QueryStringToDic(string queryString)
        {
            var nvc = HttpUtility.ParseQueryString(queryString);
            
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }

        private string HttpClientSend(HttpMethod httpMethod,
            string content,
            string requestUri,
            string contentType,
            bool ignoreCertificate,
            Dictionary<string, string> httpHeaders = null,
            Encoding encoding = null)
        {
            var httpRequestMessage = GetHttpRequestMessage(httpMethod: httpMethod,
                requestUri: requestUri,
                contentType: contentType,
                content: content,
                encoding: encoding);

            var httpClientHandler = GetHttpClientHandler(ProxyHost, ProxyPort, ignoreCertificate);
            using var client = new HttpClient(httpClientHandler);
            SetHttpHeader(httpHeaders, client);
            var response = client.Send(httpRequestMessage);
            return response.Content.ReadAsStringAsync().Result;
        }

        private static void SetHttpHeader(Dictionary<string, string> httpHeaders, HttpClient client)
        {
            if (httpHeaders == null) return;

            foreach (var header in httpHeaders)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        private static HttpClientHandler GetHttpClientHandler(string proxyHost, int proxyPort, bool ignoreCertificate)
        {
            var httpClientHandler = new HttpClientHandler();

            if (ignoreCertificate)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            }

            if (string.IsNullOrEmpty(proxyHost)) return httpClientHandler;

            httpClientHandler.UseProxy = true;
            httpClientHandler.Proxy = new WebProxy(ProxyHost, ProxyPort);
            return httpClientHandler;
        }

        private static HttpRequestMessage GetHttpRequestMessage(HttpMethod httpMethod, string requestUri, string contentType, string content, Encoding encoding = null)
        {
            if (httpMethod == HttpMethod.Post)
            {
                if (encoding == null) // 預設 Encoding.UTF8
                {
                    return new HttpRequestMessage(HttpMethod.Post, requestUri)
                    {
                        Content = new StringContent(content, Encoding.UTF8, contentType)
                    };
                }

                // 指定 Encoding
                return new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(content, encoding, contentType)
                };
            }

            if (httpMethod == HttpMethod.Get)
            {
                return new HttpRequestMessage(HttpMethod.Get, requestUri);
            }

            throw new Exception("httpMethod not implement please check use httpMethod");
        }

        private void LogInformationProxy(string url, string input, string output, string transId)
        {
            const string logTag = "PayHelperBaseHttpRequestResponse";
            LogContext.PushProperty("LogTag", logTag);
            InsertPaymentLog?.Invoke(url, input, output, transId);
        }

        protected string AesEncrypt(string str, string key, bool isEncodeUrl = true)
        {
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);
            byte[] keyBytes = new byte[16];
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(key);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            RijndaelManaged rm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = keyBytes

            };
            byte[] resultArray = rm.CreateEncryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToHexString(resultArray, 0, resultArray.Length);
        }

        protected string AesDecrypt(string str, string secretKey)
        {
            byte[] toEncryptArray = Convert.FromHexString(str);
            byte[] keyBytes = new byte[16];
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            using RijndaelManaged rm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = keyBytes
            };

            byte[] resultArray = rm.CreateDecryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
