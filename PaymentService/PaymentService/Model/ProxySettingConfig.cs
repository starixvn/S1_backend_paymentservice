using Newtonsoft.Json;

namespace PaymentService.Model;

public class ProxySettingConfig
{
    [JsonProperty("proxyHost")]
    public string ProxyHost { get; set; }

    [JsonProperty("proxyPort")]
    public int ProxyPort { get; set; }
}