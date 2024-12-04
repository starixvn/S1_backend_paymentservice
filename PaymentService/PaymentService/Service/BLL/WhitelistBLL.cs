namespace PaymentService.Service.BLL;

public class WhitelistBLL
{
    private readonly ConfigBLL _configBll;
    private readonly ILogger<WhitelistBLL> _logger;
    private readonly string _environmentName;

    public WhitelistBLL(IServiceProvider provider, IConfiguration configuration)
    {
        _configBll = provider.GetRequiredService<ConfigBLL>();
        _logger = provider.GetRequiredService<ILogger<WhitelistBLL>>();
        _environmentName = configuration.GetValue<string>("EnvironmentName");
    }

    public bool IsWhitelist(string clientIp, string paymentName, string transId, string methodName)
    {
        _logger.LogInformation($"Method={methodName}, Request: PaymentName={paymentName} clientIP={clientIp} transId={transId}");

        if (!_environmentName.Equals("PROD"))
            return true;

        var officeIps = _configBll.GetConfigValueFromDbByEnvironment<string[]>("OfficeIP", configType: "Whitelist");
        if (officeIps.Any(x => x == clientIp))
            return true;

        var whiteList = _configBll.GetConfigValueFromDb<string[]>(paymentName, configType: "Whitelist");
        if (whiteList.Any(x => x.Equals("Pass") || x.Equals(clientIp)))
            return true;

        _logger.LogInformation($"Method={methodName}, Reject: PaymentName={paymentName} clientIP={clientIp} transId={transId}");

        return false;
    }
}