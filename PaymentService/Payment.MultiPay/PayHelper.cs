using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using PaymentCore;
using PaymentCore.Models.Result;

namespace Payment.MultiPay;

public class PayHelper : PayHelperBase
{
    internal string? SignConcatSourceString { get; private set; }

    #region Debit
    public override DepositResult Debit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource)
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

    public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers,
        Dictionary<string, string> payResource)
    {
        throw new NotImplementedException();
    }

    public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
    #endregion

    public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    internal string Encrypt(Dictionary<string, object> parameters, string publicKey)
    {
        var queryString = GetQueryString(parameters);
        SignConcatSourceString = queryString;
        publicKey = publicKey.Replace("\\n", "\n");
        return EncryptData(queryString, publicKey);
    }

    internal (bool, string) ValidateSign(Dictionary<string, object> parameters, string privateKey)
    {
        var queryString = GetQueryString(parameters);
        privateKey = privateKey.Replace("\\n", "\n");
        var signature = parameters["sign"].ToString();
        var decryptData = DecryptData(signature, privateKey);
        
        return (decryptData == queryString, decryptData);
    }

    private string DecryptData(string signature, string privateKey)
    {
        string xmlPrivateKey = ConvertPemPrivateKeyToXml(privateKey);
        byte[] encryptedBytes = Convert.FromBase64String(signature);
        var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlPrivateKey);
        byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static string ConvertPemPrivateKeyToXml(string pemKey)
    {
        var stringReader = new StringReader(pemKey);
        var pemReader = new PemReader(stringReader);
        var keyPair = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();
        
        var rsaParameters = DotNetUtilities.ToRSAParameters(keyPair);
        var rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(rsaParameters);
        return rsa.ToXmlString(true); // true for private key
    }

    private string EncryptData(string httpQuery, string publicKey)
    {
        var publicKeyXml = ConvertPemPublicKeyToXml(publicKey);
        byte[] dataToEncrypt = Encoding.UTF8.GetBytes(httpQuery);
        var rsa = new RSACryptoServiceProvider(4096);

        rsa.FromXmlString(publicKeyXml);

        // Encrypt the byte array using the public key with PKCS#1 v1.5 padding
        byte[] encryptedData = rsa.Encrypt(dataToEncrypt, false); // false indicates PKCS#1 v1.5 padding
        // Return the encrypted data as a base64 string
        return Convert.ToBase64String(encryptedData);
    }

    private static string ConvertPemPublicKeyToXml(string pemKey)
    {
        var stringReader = new StringReader(pemKey);
        var pemReader = new PemReader(stringReader);
        var keyPair = (RsaKeyParameters)pemReader.ReadObject();

        var rsaParameters = DotNetUtilities.ToRSAParameters(keyPair);
        var rsa = new RSACryptoServiceProvider(4096);
        rsa.ImportParameters(rsaParameters);
        return rsa.ToXmlString(false);
    }

    private string GetQueryString(Dictionary<string,object> parameters)
    {
        var sortedParameters = parameters
            .Where(pair => !pair.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key);
        var queryString = string.Join("&", sortedParameters
            .Select(pair => $"{pair.Key}={pair.Value}"));
        return queryString;
    }
}