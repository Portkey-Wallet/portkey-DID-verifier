namespace CAVerifierServer.Grains.Options;

public class VerifierCodeOptions
{
    public int GetCodeFrequencyTimeLimit { get; set; }
    public int GetCodeFrequencyLimit { get; set; }
    public int CodeExpireTime { get; set; }
    public int RetryTimes { get; set; }

}