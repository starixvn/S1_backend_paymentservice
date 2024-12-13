namespace PaymentService.Service.Library
{
    using System.Security.Cryptography;
    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    public class Common
    {
        private readonly string environmentConfig;

        public Common(IConfiguration configuration)
        {
            environmentConfig = configuration.GetSection("EnvironmentName").Get<string>();
        }

        public string GetEnv()
        {
            switch (environmentConfig)
            {
                case "Development":
                case "LOCAL":
                    return "D";

                case "SIT":
                    return "S";

                case "UAT":
                    return "U";

                case "PROD":
                    return "P";

                default:
                    return "D";
            }
        }

        public string IsTest()
        {
            return environmentConfig == "PROD" ? "false" : "true";
        }

        public decimal Floor(decimal d, int decimals)
        {
            var value = (long)Math.Pow(10.0, decimals);
            return Convert.ToDecimal(Math.Floor(d * (decimal)value) / (decimal)value);
        }

        #region GetClientIP
        /// <summary>
        /// 获取客户端IP
        /// </summary>
        public string GetClientIP(HttpContext httpContext)
        {
            var headerNames = new[] { "X-Forwarded-For", "HTTP_X_FORWARDED_FOR", "HTTP_INCAP_CLIENT_IP", "HTTP_TRUE_CLIENT_IP", "HTTP_X_REAL_IP", "REMOTE_ADDR" };

            foreach (var header in headerNames)
            {
                if (httpContext.Request.Headers.ContainsKey(header))
                {
                    string sServerVariables = httpContext.Request.Headers[header];
                    var ip = GetIPAddress(sServerVariables.Split(','));

                    if (!string.IsNullOrEmpty(ip))
                    {
                        return ip;
                    }
                }
            }

            return httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        public bool IsManualRequestValid(HttpContext httpContext, string key )
        {
            string requestKey = httpContext.Request.Headers["ManualKey"];
            
            return MD5Encrypt(key) == requestKey;
        }

        public string MD5Encrypt(string key)
        {
            var md5Hasher = new MD5CryptoServiceProvider();
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(key));
            var sBuilder = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public int GetId(string transId)
        {
            var m = Regex.Match(transId, @"\d+");
            _ = int.TryParse(m.Value, out var id);
            return id;
        }

        private string GetIPAddress(IEnumerable<string> ipArr)
        {
            foreach (var ip in ipArr)
            {
                var ipAddress = ip.Trim();
                if (IsIPAddress(ipAddress) && !IsInnerIP(ipAddress))
                    return ipAddress.Trim();
            }

            return null;
        }
        public bool IsInnerIP(string ipAddress)
        {
            var ipNum = getIpNum(ipAddress);

            const long aBegin = 167772160;  // 10.0.0.0;
            const long aEnd = 184549375;    // 10.255.255.255
            const long bBegin = 2886729728; // 172.16.0.0
            const long bEnd = 2887778303;   // 172.31.255.255
            const long cBegin = 3232235520; // 192.168.0.0
            const long cEnd = 3232301055;   // 192.168.255.255

            var isInnerIp = IsInner(ipNum, aBegin, aEnd) || IsInner(ipNum, bBegin, bEnd) || IsInner(ipNum, cBegin, cEnd) || ipAddress == "127.0.0.1";

            return isInnerIp;
        }
        private long getIpNum(string ipAddress)
        {
            var ip = ipAddress.Split('.');
            long a = Convert.ToInt32(ip[0]);
            long b = Convert.ToInt32(ip[1]);
            long c = Convert.ToInt32(ip[2]);
            long d = Convert.ToInt32(ip[3]);

            var ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            return ipNum;
        }
        private bool IsIPAddress(string str1)
        {
            if (string.IsNullOrEmpty(str1) || str1.Length < 7 || str1.Length > 15) return false;
            const string regformat = @"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$";

            var regex = new Regex(regformat, RegexOptions.IgnoreCase);
            return regex.IsMatch(str1);

        }
        private static bool IsInner(long userIp, long begin, long end)
        {
            return userIp >= begin && userIp <= end;
        }

        #endregion
    }
}
