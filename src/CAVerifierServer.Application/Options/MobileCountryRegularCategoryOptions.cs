using System.Collections.Generic;

namespace CAVerifierServer.Options;

public class MobileCountryRegularCategoryOptions
{
    public List<MobileInfo> MobileInfos { get; set; } 
}

public class MobileInfo
{
    public string CountryCode { get; set; }
    public string MobileRegular { get; set; }
    public string Country { get; set; }
}