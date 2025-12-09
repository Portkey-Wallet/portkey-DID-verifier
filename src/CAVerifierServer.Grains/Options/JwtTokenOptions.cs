namespace CAVerifierServer.Telegram.Options;

public class JwtTokenOptions
{
    public string Issuer { get; set; }
    public IEnumerable<string> Audiences { get; set; }
}