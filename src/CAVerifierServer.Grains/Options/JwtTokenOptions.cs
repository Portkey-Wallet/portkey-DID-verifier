namespace CAVerifierServer.Telegram.Options;

[GenerateSerializer]
[Alias("CAVerifierServer.Telegram.Options.JwtTokenOptions")]
public class JwtTokenOptions
{
    [Id(0)]
    public string Issuer { get; set; }
    [Id(1)]
    public IEnumerable<string> Audiences { get; set; }
}