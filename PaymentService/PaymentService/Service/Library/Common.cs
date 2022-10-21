using System.Text.RegularExpressions;

namespace PaymentService.Service.Library
{
    public class Common
    {
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Common> _logger;


        public Common(IWebHostEnvironment environment, IConfiguration configuration, ILogger<Common> _logger)
        {
            this.environment = environment;
            this._configuration = configuration;
            this._logger = _logger;
        }
        public string GetEnv()
        {
            if (environment.EnvironmentName == "Development" || environment.EnvironmentName == "Local")
            {
                return "D";
            }
            else if (environment.EnvironmentName == "Staging")
            {
                return "S";
            }
            else if (environment.EnvironmentName == "Production")
            {
                return "P";
            }
            else if (environment.EnvironmentName == "QA")
            {
                return "Q";
            }
            return "P";
        }

        public string IsTest()
        {
           if (environment.EnvironmentName == "Production")
            {
                return "false";
            }
            else 
            {
                return "true";
            }
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
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetClientIP(HttpContext httpContext)
        {
            var HeaderNames = new string[] { "X-Forwarded-For", "HTTP_X_FORWARDED_FOR", "HTTP_INCAP_CLIENT_IP", "HTTP_TRUE_CLIENT_IP", "HTTP_X_REAL_IP", "REMOTE_ADDR" };

            foreach (var Header in HeaderNames)
            {

                if (httpContext.Request.Headers.ContainsKey(Header))
                {
                    string sServerVariables = httpContext.Request.Headers[Header];
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
            int.TryParse(m.Value, out var id);
            return id;
        }

        private string GetIPAddress(string[] IPArry)
        {
            foreach (var ip in IPArry)
            {
                var IPAddress = ip.Trim();
                if (IsIPAddress(IPAddress)
                    && !isInnerIP(IPAddress)) return IPAddress.Trim();
            }

            return null;
        }
        private bool isInnerIP(string ipAddress)
        {
            var ipNum = getIpNum(ipAddress);

            long aBegin = 167772160;// getIpNum("10.0.0.0");
            long aEnd = 184549375;// getIpNum("10.255.255.255");
            long bBegin = 2886729728; //getIpNum("172.16.0.0");
            long bEnd = 2887778303; //getIpNum("172.31.255.255");
            long cBegin = 3232235520; //getIpNum("192.168.0.0");
            long cEnd = 3232301055; //getIpNum("192.168.255.255");

            // Logger.Log("aBegin:" + aBegin + ",aEnd:" + aEnd + ",bBegin:" + bBegin + ",bEnd:" + bEnd + ",cBegin:" + cBegin + ",cEnd:" + cEnd);

            var isInnerIp = isInner(ipNum, aBegin, aEnd) || isInner(ipNum, bBegin, bEnd) || isInner(ipNum, cBegin, cEnd) || ipAddress == "127.0.0.1";
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
        private bool isInner(long userIp, long begin, long end)
        {
            return (userIp >= begin) && (userIp <= end);
        }

        public bool IsWhiteList(string clientIp, string paymentName, string transId,string methodName)
        {
            _logger.LogInformation($"Method={methodName}, Request: PaymentName={paymentName} clientIP={clientIp} transId={transId}");
            var officeList = _configuration.GetSection("OfficeIP")?.Get<string[]>();
            if (officeList.Any(x => x == clientIp))
            {
                return true;
            }

            var whiteList = _configuration.GetSection("WhiteList")?.Get<Dictionary<string, string[]>>();
            if (whiteList != null)
            {
                var whiteListIgnoreCase = new Dictionary<string, string[]>(whiteList, StringComparer.OrdinalIgnoreCase);
                if (whiteListIgnoreCase.ContainsKey(paymentName))
                {
                    string[] iplist;
                    whiteListIgnoreCase.TryGetValue(paymentName, out iplist);
                    if (!iplist.Any(x => x == clientIp))
                    {
                        _logger.LogInformation($"Method={methodName}, Reject: PaymentName={paymentName} clientIP={clientIp} transId={transId}");
                        return false;
                    }
                }

                return true;
            }

            return true;
        }

        #endregion
    }
}
