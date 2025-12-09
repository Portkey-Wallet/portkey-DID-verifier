using System.Collections.Generic;

namespace CAVerifierServer.Options;

public class SmsServiceOptions
{
    public Dictionary<string, SmsServiceOption> SmsServiceInfos { get; set; }
}

public class SmsServiceOption
{
    public Dictionary<string, int> SupportingCountriesRatio { get; set; }
}