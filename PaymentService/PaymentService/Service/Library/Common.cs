using System.Text.RegularExpressions;
using PaymentService.Service.BLL;

namespace PaymentService.Service.Library
{
    public class Common
    {
        private readonly ConfigBLL _configBll;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<Common> _logger;

        public Common(IServiceProvider provider)
        {
            _configBll = provider.GetRequiredService<ConfigBLL>();
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<Common>>();
        }
        
        public string GetEnv()
        {
            switch (_environment.EnvironmentName)
            {
                case "Development":
                case "Local":
                    return "D";
                    
                case "Staging":
                    return "S";
                
                case "Production":
                    return "P";
                
                case "QA":
                    return "Q";
                
                default:
                    return "P";
            }
        }

        public string IsTest()
        {
            return _environment.EnvironmentName == "Production" ? "false" : "true";
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
                if (IsIPAddress(ipAddress)
                    && !IsInnerIP(ipAddress)) return ipAddress.Trim();
            }

            return null;
        }
        private bool IsInnerIP(string ipAddress)
        {
            var ipNum = getIpNum(ipAddress);

            long aBegin = 167772160;// getIpNum("10.0.0.0");
            long aEnd = 184549375;// getIpNum("10.255.255.255");
            long bBegin = 2886729728; //getIpNum("172.16.0.0");
            long bEnd = 2887778303; //getIpNum("172.31.255.255");
            long cBegin = 3232235520; //getIpNum("192.168.0.0");
            long cEnd = 3232301055; //getIpNum("192.168.255.255");

            // Logger.Log("aBegin:" + aBegin + ",aEnd:" + aEnd + ",bBegin:" + bBegin + ",bEnd:" + bEnd + ",cBegin:" + cBegin + ",cEnd:" + cEnd);

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
            return (userIp >= begin) && (userIp <= end);
        }

        public bool IsWhiteList(string clientIp, string paymentName, string transId,string methodName)
        {
            if (!_environment.EnvironmentName.Equals("Production", StringComparison.OrdinalIgnoreCase))
                return true;
            
            _logger.LogInformation($"Method={methodName}, Request: PaymentName={paymentName} clientIP={clientIp} transId={transId}");
            
            var officeIps = _configBll.GetConfigValueByEnvironment<string[]>("OfficeIP");
            
            if (officeIps.Any(x => x == clientIp))
            {
                return true;
            }

            var whiteList = _configBll.GetConfigValue<string[]>(paymentName);

            if (whiteList.Any(x => x.Equals(clientIp)))
                return true;
            
            _logger.LogInformation($"Method={methodName}, Reject: PaymentName={paymentName} clientIP={clientIp} transId={transId}");
            
            return false;
        }

        #endregion
    }
}
