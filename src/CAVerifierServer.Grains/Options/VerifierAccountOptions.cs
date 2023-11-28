namespace CAVerifierServer.Grains.Options;

public class VerifierAccountOptions
{
    public string Address { get; set; }
    public string PrivateKey { get; set; }
    public string KeyStorePath { get; set; }
    public string KeyStorePassword { get; set; }
}