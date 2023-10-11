using PaymentService.Service.DAL;
using PaymentService.Service.Library;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL;

public class WhitelistBLL
{
    private readonly ConfigBLL _configBll;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WhitelistBLL> _logger;

    public WhitelistBLL(IServiceProvider provider)
    {
        _configBll = provider.GetRequiredService<ConfigBLL>();
        _environment = provider.GetRequiredService<IWebHostEnvironment>();
        _logger = provider.GetRequiredService<ILogger<WhitelistBLL>>();
    }
    
    public bool IsWhitelist(string clientIp, string paymentName, string transId, string methodName)
    {
        // if (!_environment.EnvironmentName.Equals("Production", StringComparison.OrdinalIgnoreCase))
        //     return true;
            
        _logger.LogInformation($"Method={methodName}, Request: PaymentName={paymentName} clientIP={clientIp} transId={transId}");
            
        var officeIps = _configBll.GetConfigValueFromDbByEnvironment<string[]>("OfficeIP", configType: "Whitelist");
            
        if (officeIps.Any(x => x == clientIp))
        {
            return true;
        }

        var whiteList = _configBll.GetConfigValueFromDb<string[]>(paymentName, configType: "Whitelist");

        if (whiteList.Any(x => x.Equals(clientIp)))
            return true;
            
        _logger.LogInformation($"Method={methodName}, Reject: PaymentName={paymentName} clientIP={clientIp} transId={transId}");
            
        return false;
    }
}