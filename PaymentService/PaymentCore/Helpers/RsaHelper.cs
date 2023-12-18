using System.Security.Cryptography;
using System.Text;
using PaymentCore.Exceptions;

namespace PaymentCore.Helpers;

public static class RsaHelper
{
    public static bool RsaVerifyData(string str, string target, string publicKey)
    {
        var targetBytes = Convert.FromBase64String(target);
        var inputBytes = Encoding.UTF8.GetBytes(str);
        var keyBytes = Convert.FromBase64String(publicKey);

        var rsa = DecodeX509PublicKey(keyBytes);
        var result = rsa
            .VerifyData(inputBytes, targetBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return result;
    }
    
    private static RSACryptoServiceProvider DecodeX509PublicKey(byte[] x509Key)
    {
        // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
        byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

        using var mem = new MemoryStream(x509Key);
        using var reader = new BinaryReader(mem);
        try
        {
            var bytes = reader.ReadUInt16();
            switch (bytes)
            {
                case 0x8130:
                    reader.ReadByte();    //advance 1 byte
                    break;
                case 0x8230:
                    reader.ReadInt16();   //advance 2 bytes
                    break;
                default:
                    return null;
            }

            var seq = reader.ReadBytes(15);
            if (!CompareBytearrays(seq, seqOid))  //make sure Sequence for OID is correct
                return null;

            bytes = reader.ReadUInt16();
            switch (bytes)
            {
                //data read as little endian order (actual data order for Bit String is 03 81)
                case 0x8103:
                    reader.ReadByte();    //advance 1 byte
                    break;
                case 0x8203:
                    reader.ReadInt16();   //advance 2 bytes
                    break;
                default:
                    return null;
            }

            var bt = reader.ReadByte();
            if (bt != 0x00)     //expect null byte next
                return null;

            bytes = reader.ReadUInt16();
            switch (bytes)
            {
                //data read as little endian order (actual data order for Sequence is 30 81)
                case 0x8130:
                    reader.ReadByte();    //advance 1 byte
                    break;
                case 0x8230:
                    reader.ReadInt16();   //advance 2 bytes
                    break;
                default:
                    return null;
            }

            bytes = reader.ReadUInt16();
            byte lowByte;
            byte highByte = 0x00;

            switch (bytes)
            {
                //data read as little endian order (actual data order for Integer is 02 81)
                case 0x8102:
                    lowByte = reader.ReadByte();  // read next bytes which is bytes in modulus
                    break;
                case 0x8202:
                    highByte = reader.ReadByte(); //advance 2 bytes
                    lowByte = reader.ReadByte();
                    break;
                default:
                    return null;
            }
            byte[] modInt = { lowByte, highByte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
            var modSize = BitConverter.ToInt32(modInt, 0);

            var firstByte = reader.ReadByte();
            reader.BaseStream.Seek(-1, SeekOrigin.Current);

            if (firstByte == 0x00)
            {   //if first byte (highest order) of modulus is zero, don't include it
                reader.ReadByte();    //skip this null byte
                modSize -= 1;   //reduce modulus buffer size by 1
            }

            var modulus = reader.ReadBytes(modSize); //read the modulus bytes

            if (reader.ReadByte() != 0x02)            //expect an Integer for the exponent data
                return null;
            var expBytes = reader.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
            var exponent = reader.ReadBytes(expBytes);

            var rsa = new RSACryptoServiceProvider();
            var rsaKeyInfo = new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent
            };
            rsa.ImportParameters(rsaKeyInfo);
            return rsa;
        }
        catch (Exception ex)
        {
            throw new CustomException("DecodeX509PublicKey failed.", ex);
        }
    }

    private static bool CompareBytearrays(IReadOnlyCollection<byte> a, IReadOnlyList<byte> b)
    {
        if (a.Count != b.Count)
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
}
