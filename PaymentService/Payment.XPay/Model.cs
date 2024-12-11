using System.Xml.Serialization;

namespace Payment.XPay;

[XmlRoot(ElementName = "xml")]
public class CreatOrderResp
{
    public string billno { get; set; }

    public string displaypage { get; set; }
}
    
[XmlRoot(ElementName = "xml")]
public class CreatPayoutResp
{
    public string status { get; set; }
        
    public string error_code { get; set; }
        
    public string error_description { get; set; }
}