namespace CAVerifierServer.Grains.Options;

public class AppleAuthOptions
{
    public List<string> Audiences { get; set; }
    public int KeysExpireTime { get; set; }
}