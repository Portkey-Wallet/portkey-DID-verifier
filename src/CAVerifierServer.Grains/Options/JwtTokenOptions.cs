namespace CAVerifierServer.Grains.Options;

[GenerateSerializer]
[Alias("CAVerifierServer.Grains.Options.JwtTokenOptions")]
public class JwtTokenOptions
{
    [Id(0)]
    public string Issuer { get; set; }
    [Id(1)]
    public IEnumerable<string> Audiences { get; set; }
}