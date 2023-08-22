//1、加密：
var dictionary = new SortedDictionary<string, string>
{
{"merchantCode",SnModel.MerchantCode },
{"merchantTradeNo",SnModel.MerchantTradeNo },
{"userId",SnModel.UserId},
{"amount",SnModel.Amount },
{"notifyUrl",SnModel.NotifyUrl },
{"returnUrl",SnModel.ReturnUrl },
{"terminalType",SnModel.TerminalType},
{"channel",SnModel.Channel },
{"extendedAttrData",SnModel.ExtendedAttrData }
};

//过滤空参数
var dicstr = dictionary.Where(s => !s.Value.IsNullOrWhiteSpace()).Select(s => s.Value);

var signstr = string.Join("", dicstr);

var sign = RsaBouncyCastleHelper.RsaSign(signstr, PrivateKeyStringOrPath, HashAlgorithmName.SHA1);

var bytesign = Convert.FromBase64String(sign);

var hex = HexStringFormat.ByteToHexStr(bytesign);

dictionary.Add("sign", hex);

var contentstr = JsonConvert.SerializeObject(dictionary.Where(s => !s.Value.IsNullOrWhiteSpace()).ToDictionary(s => s.Key, v => v.Value));

var content = RsaBouncyCastleHelper.RsaEncryptWithPublic(contentstr, PublicKeyStringOrPath);



//2、验签
var json = RsaBouncyCastleHelper.RsaDecryptWithPrivate(SnModel.Content, PrivateKeyStringOrPath);

var content = JsonConvert.DeserializeObject<NotifyRequestContent>(json);

var dictionary = new SortedDictionary<string, string>
{
{"merchantCode",content.MerchantCode },
{"merchantTradeNo",content.MerchantTradeNo },
{"tradeNo",content.TradeNo},
{"amount",content.Amount },
{"tradeStatus",content.TradeStatus },
{"notifyTime",content.NotifyTime }
};

//过滤空参数
var dicstr = dictionary.Select(s => s.Value);

var signstr = string.Join("", dicstr);

var sign = HexStringFormat.HexStringToString(content.Sign);

if (!RsaBouncyCastleHelper.VerifySign(signstr, PublicKeyStringOrPath, sign, HashAlgorithmName.SHA1))
{
//数据被篡改

}


public class RsaBouncyCastleHelper
{
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

        var privateKeyParam = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
        var xml = string.Format(
        "<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
        Convert.ToBase64String(privateKeyParam.Modulus.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.PublicExponent.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.P.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.Q.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.DP.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.DQ.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.QInv.ToByteArrayUnsigned()),
        Convert.ToBase64String(privateKeyParam.Exponent.ToByteArrayUnsigned())
        );

        RSA rsa = RSA.Create();
        rsa.FromLvccXmlString(xml);
        byte[] signBytes = rsa.SignData(UTF8Encoding.UTF8.GetBytes(content), name.Value, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signBytes);
    }

    /// <summary>
    /// 公钥加密
    /// </summary>
    /// <param name="dataToEncrypt">待加密文本</param>
    /// <param name="publicKeyString"></param>
    public static string RsaEncryptWithPublic(string dataToEncrypt, string publicKeyString)
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
        var rsa = CreateRsaFromPrivateKey(privateKey);
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
    /// RSA验签
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="publicKey">RSA公钥</param>
    /// <param name="signData">签名字段</param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool VerifySign(string content, string publicKey, string signData, HashAlgorithmName? name = null)
    {
        if (name == null)
        {
            name = HashAlgorithmName.MD5;
        }

        RsaKeyParameters publicKeyParam = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
        var xml = string.Format(
        "<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>",
        Convert.ToBase64String(publicKeyParam.Modulus.ToByteArrayUnsigned()),
        Convert.ToBase64String(publicKeyParam.Exponent.ToByteArrayUnsigned())
        );

        RSA rsa = RSA.Create();
        rsa.FromLvccXmlString(xml);
        return rsa.VerifyData(UTF8Encoding.UTF8.GetBytes(content), Convert.FromBase64String(signData), name.Value, RSASignaturePadding.Pkcs1);
    }
}


public class HexStringFormat
{

    /// <summary>
    /// 字节数组转16进制字符串
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ByteToHexStr(byte[] bytes)
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

    /// <summary>
    /// 转字符串
    /// </summary>
    /// <param name="hexString"></param>
    /// <returns></returns>
    public static string HexStringToString(string hexString)
    {
        hexString = hexString.Replace(" ", "");
        if ((hexString.Length % 2) != 0)
            hexString += " ";
        byte[] returnBytes = new byte[hexString.Length / 2];
        for (int i = 0; i < returnBytes.Length; i++)
            returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2).Trim(), 16);

        return Convert.ToBase64String(returnBytes);
    }

      /// <summary>
        /// RSA导入key
        /// </summary>
        public static void FromLvccXmlString(this RSA rsa, string xmlString)
        {
            RSAParameters parameters = new RSAParameters();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Exponent": parameters.Exponent = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "P": parameters.P = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Q": parameters.Q = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DP": parameters.DP = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DQ": parameters.DQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "InverseQ": parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "D": parameters.D = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }
}

