using Newtonsoft.Json;
using PaymentService.Service.Model;

namespace PaymentService.Service.BLL;

public class ConfigBLL
{
    private readonly IEnumerable<SiteConfig> _siteConfigs;
    private readonly ILogger<ConfigBLL> _logger;
    private readonly string _env;

    public ConfigBLL(IServiceProvider provider)
    {
        var environment = provider.GetRequiredService<IWebHostEnvironment>();
        _siteConfigs = provider.GetServices<SiteConfig>();
        _logger = provider.GetRequiredService<ILogger<ConfigBLL>>();
        _env = environment.EnvironmentName;
    }

    public string GetConfigValue(string configKey, int siteId = 0)
    {
        var config = GetSiteConfigValue(configKey, siteId);

        if (string.IsNullOrEmpty(config) && siteId != 0)
            config = GetSiteConfigValue(configKey, 0);

        return config;
    }

    public string GetConfigValueByEnvironment(string configKey, int siteId = 0)
        => GetConfigValue($"{configKey}.{_env}.json", siteId);

    public T GetConfigValue<T>(string configKey, int siteId = 0)
    {
        var json = GetConfigValue(configKey, siteId);

        return JsonConvert.DeserializeObject<T>(json);
    }
    
    public T GetConfigValueByEnvironment<T>(string configKey, int siteId = 0)
    {
        var json = GetConfigValueByEnvironment(configKey, siteId);

        return JsonConvert.DeserializeObject<T>(json);
    }
    
    private string GetSiteConfigValue(string configKey, int siteId)
    {
        var configs = _siteConfigs
            .FirstOrDefault(x => x.SiteId == siteId);
        
        if (configs == null)
        {
            _logger.LogWarning($"siteId={siteId} config not found.");
            return string.Empty;
        }
        
        if (!configs.Config.TryGetValue(configKey, out var config))
        {
            _logger.LogWarning($"siteId={siteId}, configKey={configKey} not found.");
            return string.Empty;
        }

        return config;
    }
}