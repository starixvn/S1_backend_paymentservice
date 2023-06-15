using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentCore;
using PaymentCore.Exceptions;
using PaymentCore.Extensions;
using PaymentCore.Interface;
using PaymentCore.Models.Result;

namespace Payment.Coin2Pay
{
    class PayHelper : PayHelperBase, IExchangeHelper
    {
        private readonly IFlurlHttpHelper _httpHelper;
        private readonly ILogger<PayHelper> _logger;

        public PayHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PayHelper>>();
            _httpHelper = provider.GetRequiredService<IFlurlHttpHelper>();
        }

        #region ExchangeRate

        /// <summary>
        /// 這家沒提供匯率，我問號
        /// </summary>
        public ExchangeResult GetExchange(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
            /*var baseApiUrl = input["host"];
            var currency = input["PayCurrencyId"];
            var payCode = input["PayCode"];
            var platformId = input["PlatformID"];
            var type = input["ExchangeType"]; // 0: 提款, 1: 存款
            
            var req = new Dictionary<string, string>
            {
                { "platformID", platformId },
                { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() }
            };
            
            req.Add("sign", GetSign(req));
            var resp = GetExchangeRate(baseApiUrl, req);
            var priceInfo = resp.Data.PriceList.FirstOrDefault(x => $"{x.Token}{x.Network}".Equals(payCode));
            
            if (priceInfo == null)
            {
                throw new CustomException("Price info data not exists: " + payCode);
            }

            var rate = type.Equals("0") ? priceInfo.PriceWithdrawal : priceInfo.PriceDeposit;

            return new ExchangeResult(ErrorCode.Success, "Success", new CryptoExchangeInfo
            {
                Rate = rate,
                ExchangeAmount = decimal.Parse(),
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
            });*/
        }

        #endregion

        #region Debit

        public override DepositResult Debit(Dictionary<string, string> input)
        {
            var baseApiUrl = input["host"];
            var currency = input["PayCurrencyId"];
            var payCode = input["PayCode"];
            var merchant = input["Merchant"];
            var platformId = input["PlatformID"];
            var transId = input["TransID"];
            var privateKey = input["PrivateKey"];
            var reqAmount = input["Amount"];
            var depositInfo = input["DepositInfo"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "platformID", platformId },
                    { "UID", transId },
                    { "UserName", merchant },
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                    { "currency", currency }
                };
                
                req.Add("sign", GetSign(req, privateKey));

                var (token, network) = PayCode2TokenInfo(payCode);
                var resp = GetAddress(baseApiUrl, req);
                var addr = resp.Data
                    .FirstOrDefault(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase)
                        && x.Network.Equals(network, StringComparison.OrdinalIgnoreCase));

                if (addr == null)
                {
                    throw new CustomException("Address data not exists. network: " + payCode);
                }
                
                var obj = JObject.Parse(depositInfo);
                
                if (!obj.TryGetValue("ExchangeRate", StringComparison.OrdinalIgnoreCase, out var jExchangeRate)
                    || !obj.TryGetValue("ExchangeAmount", StringComparison.OrdinalIgnoreCase, out var jExchangeAmount))
                {
                    throw new CustomException("No exchange info.");
                }

                var formData = new Dictionary<string, string>
                {
                    { "DepositAmount", reqAmount },
                    { "Rate", jExchangeRate.ToObject<string>() },
                    { "RequestAmount", jExchangeAmount.ToObject<string>() },
                    { "Token", token }
                };

                var response = new DepositResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.Success,
                    Url = addr.Address,
                    ThirdId = string.Empty,
                    FormData = formData
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Deposit failed, Code:{ex.Code}, Message:{ex.Message}");

                return new DepositResult(false, ex.Code, ex.Message);
            }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> input)
        {
            var publicKey = input["PublicKey"];
            var req = FromJson<DepositCallBackReq>(bodyContent);
            var reqStr = FromJson<Dictionary<string, string>>(bodyContent);
            
            var dic = new Dictionary<string, string>
            {
                { "uid", reqStr["uid"] },
                { "txid", reqStr["txid"] },
                { "address", reqStr["address"] },
                { "token", reqStr["token"] },
                { "quantity", reqStr["quantity"] },
                { "amount_in_usdt", reqStr["amount_in_usdt"] },
                { "deposit_fee", reqStr["deposit_fee"] },
                { "given_Credit", reqStr["given_Credit"] },
                { "credit_Rate", reqStr["credit_Rate"] },
                { "timestamp", reqStr["timestamp"] },
                { "currency", reqStr["currency"] }
            };

            if (!VerifySign(dic, req.Sign, publicKey))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Comment = "Signature verification failed.",
                    Response = CallbackResponse.Failed
                };
            }

            var transId = req.TransId;
            var depositInfo = input["DepositInfo"];
            var jObj = JObject.Parse(depositInfo);

            if (!jObj.TryGetValue("ExchangeRate", StringComparison.OrdinalIgnoreCase, out var jExchangeRate)
                || !jObj.TryGetValue("ExchangeAmount", StringComparison.OrdinalIgnoreCase, out var jExchangeAmount))
            {
                return new NotifyDepositResult
                {
                    TransStatus = TransStatus.Pending,
                    Comment = "DepositInfo has no exchange rate data.",
                    Response = CallbackResponse.Failed
                };
            }

            var requestAmount = decimal.Parse(input["RequestAmount"]);
            var requestCryptoAmount = jExchangeAmount.ToObject<decimal>();
            var exchangeRate = jExchangeRate.ToObject<decimal>();

            var result = new NotifyDepositResult
            {
                DepositId = transId,
                ThirdId = req.ThirdId,
                Comment = bodyContent,
                TransStatus = TransStatus.Success,
                Response = CallbackResponse.Success
            };
            
            result.ActualAmount =
                GetActualAmount(requestAmount, requestCryptoAmount, req.Quantity, exchangeRate);

            return result;
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Credit

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            
            var baseApiUrl = inputs["host"];
            var apiKey = inputs["key"];
            var payoutKey = inputs["payoutKey"];
            var payoutApiUrl = inputs["payoutUrl"];
            var currency = inputs["PayCurrencyId"];
            var payCode = inputs["PayCode"];
            var reqAmount = inputs["Amount"];
            var transId = inputs["TransID"];
            var withdrawalInfo = inputs["WithdrawalInfo"];
            var platformId = inputs["PlatformID"];
            var privateKey = inputs["PrivateKey"];
            var merchant = inputs["Merchant"];

            try
            {
                var req = new Dictionary<string, string>
                {
                    { "platformID", platformId },
                    { "UID", transId },
                    { "UserName", merchant },
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                    { "currency", currency }
                };

                req.Add("sign", GetSign(req, privateKey));
                
                var resp = GetAddress(baseApiUrl, req);
                var addr = resp.Data
                    .FirstOrDefault(x => x.Network.Equals(payCode, StringComparison.OrdinalIgnoreCase));

                if (addr == null)
                {
                    throw new CustomException("Address data not exists. network: " + payCode);
                }
                
                req = new Dictionary<string, string>
                {
                    { "platformID", platformId },
                    { "from_address", addr.Address },
                    { "to_address", inputs["AccountNo"] },
                    { "order_id", transId },
                    { "creditamount", reqAmount },// 這應該是要先getWithdrawalEstimateValue
                    { "token", transId },
                    { "network", transId },
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() }
                };
                
                req.Add("sign", GetSign(req, privateKey));

                var payoutResp = Payout(payoutApiUrl, req);
                var response = new WithdrawResult
                {
                    Success = true,
                    WithdrawId = payoutResp.ResPayoutId,
                    RawContent = JsonConvert.SerializeObject(payoutResp),
                    WithdrawalInfo = withdrawalInfo
                };

                return response;
            }
            catch (CustomException ex)
            {
                _logger.LogInformation($"Withdrawal failed, Code:{ex.Code}, Message:{ex.Message}");

                return new WithdrawResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
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

        #region Private Methods
        

        private GetTokenPriceResp GetExchangeRate(string baseApiUrl, IDictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.GetExchangeRate;

            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJson<IDictionary<string, string>, GetTokenPriceResp>(apiUrl, req);

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay GetExchangeRate failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}",
                    ErrorCode.GetExchangeRateFailed);
            }

            return response.Result;
        }

        private GetAddressResp GetAddress(string baseApiUrl, IDictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.GetAddress;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJson<IDictionary<string, string>, GetAddressResp>(apiUrl, req);

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay GetAddress failed, HttpStatus: {(int)statusCode} {statusCode}");
            }
            
            if (!result.Result)
                throw new CustomException(
                    $"Coin2Pay GetAddress response error code: {result.Code}, msg: {result.Message}");

            return result;
        }

        private GetWithdrawalEstimateResp GetWithdrawalEstimate(string baseApiUrl, IDictionary<string, string> req)
        {
            var apiUrl = baseApiUrl + Path.GetWithdrawalEstimate;

            var (result, statusCode) = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJsonAsync<IDictionary<string, string>, GetWithdrawalEstimateResp>(apiUrl, req)
                .Result;

            if (!statusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay GetWithdrawalEstimate failed, HttpStatus: {(int)statusCode} {statusCode}");
            }

            if (!result.Result)
                throw new CustomException(
                    $"Coin2Pay GetWithdrawalEstimate response error: {result.Code}, message: {result.Message}");

            return result;
        }

        private PayoutResp Payout(string apiUrl,
            Dictionary<string, string> req)
        {
            var response = _httpHelper
                .WithHeader("accept", MediaType.Json)
                .PostJsonAsync<Dictionary<string, string>, PayoutResp>(apiUrl, req)
                .Result;

            if (!response.StatusCode.IsSuccessStatus())
            {
                throw new CustomException(
                    $"Coin2Pay Payout failed, HttpStatus: {(int)response.StatusCode} {response.StatusCode}" +
                    $", error code: {response.Result.Error} {Coin2PayDefinition.GetPayoutErrorMessage(response.Result.Error)}",
                    ErrorCode.PayoutFailed);
            }

            if (response.Result.Error != 0)
                throw new CustomException(
                    $"Coin2Pay Payout response error: {response.Result.Error} {Coin2PayDefinition.GetPayoutErrorMessage(response.Result.Error)}",
                    ErrorCode.GetWalletAddressFailed);

            return response.Result;
        }

        private static string GetSign(IEnumerable<KeyValuePair<string, string>> param, string privateKey)
        {
            var str = string.Join(string.Empty, param
                .Select(x => x.Value));
            
            Console.WriteLine("str: " + str);
            
            var inputBytes = Encoding.UTF8.GetBytes(str);
            using var alg = SHA256.Create();
            var inputHash = alg.ComputeHash(inputBytes);

            var keyBytes = Convert.FromBase64String(privateKey);
            var rsa = new RSACryptoServiceProvider(2048);
            rsa.ImportRSAPrivateKey(keyBytes, out _);
            
            var signHash = rsa.SignHash(inputHash, "SHA256");
            var sign = ReplaceBase64(Convert.ToBase64String(signHash));

            return sign;
        }
        
        private static bool VerifySign(IEnumerable<KeyValuePair<string, string>> param, string target,
            string publicKey)
        {
            var str = string.Join(string.Empty, param
                .Where(x => !x.Key.Equals("sign", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value));

            Console.WriteLine("str: " + str);
            
            var targetBase64Str = RevertBase64(target);
            var targetBytes = Convert.FromBase64String(targetBase64Str);
            var inputBytes = Encoding.UTF8.GetBytes(str);
            var keyBytes = Convert.FromBase64String(publicKey);
            
            var rsa = DecodeX509PublicKey(keyBytes);
            var result = rsa
                .VerifyData(inputBytes, targetBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            return result;
        }
        
        

        private static decimal GetActualAmount(decimal requestAmount,
            decimal requestCryptoAmount,
            decimal receivedCryptoAmount,
            decimal exchangeRate)
        {
            if (receivedCryptoAmount == requestCryptoAmount)
                return requestAmount;

            // 使用匯率回推平台金額，保留小數 2 位
            var actualAmount = decimal.Round(receivedCryptoAmount * exchangeRate, 2);
            return actualAmount;
        }
        
        private static string RevertBase64(string str)
            => PadBase64(str).Replace('-', '+')
                .Replace('_', '/')
                .Replace(" ", string.Empty);

        private static string ReplaceBase64(string str)
            => str.Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", string.Empty);

        
        private static string PadBase64(string base64String)
        {
            var gapBase64 = base64String.Length % 4;
            if (gapBase64 == 0)
                return base64String;

            gapBase64 = 4 - gapBase64;
            while (gapBase64 > 0)
            {
                base64String += "=";
                gapBase64--;
            }

            return base64String;
        }
        
        private static RSACryptoServiceProvider DecodeX509PublicKey(byte[] x509Key)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
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
                byte lowbyte = 0x00;
                byte highbyte = 0x00;

                switch (bytes)
                {
                    //data read as little endian order (actual data order for Integer is 02 81)
                    case 0x8102:
                        lowbyte = reader.ReadByte();  // read next bytes which is bytes in modulus
                        break;
                    case 0x8202:
                        highbyte = reader.ReadByte(); //advance 2 bytes
                        lowbyte = reader.ReadByte();
                        break;
                    default:
                        return null;
                }
                byte[] modInt = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
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
                int expbytes = reader.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                var exponent = reader.ReadBytes(expbytes);

                // We don't really need to print anything but if we insist to...
                //showBytes("\nExponent", exponent);
                //showBytes("\nModulus", modulus);

                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
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
        
        private static (string Token, string Network) PayCode2TokenInfo(string payCode)
        {
            var tokenInfo = payCode.Split('_');
            
            return (tokenInfo[0], tokenInfo[1]);
        }
        
        #endregion
    }
}