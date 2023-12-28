namespace CAVerifierServer.Telegram.Options;

public class JwtTokenOptions
{
    public string PrivateKey { get; set; }
    public string Issuer { get; set; }
    public IEnumerable<string> Audiences { get; set; }
    public int Expire { get; set; }
}