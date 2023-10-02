namespace PaymentService.Service.Model;

public class SiteConfig
{
    public int SiteId { get; set; }
    public string SiteName { get; set; }
    public Dictionary<string, string> Config { get; set; }

    public SiteConfig(ServerInfo serverInfo, Dictionary<string, string> config)
    {
        SiteId = serverInfo.SiteId;
        SiteName = serverInfo.SiteName;
        Config = config;
    }
}