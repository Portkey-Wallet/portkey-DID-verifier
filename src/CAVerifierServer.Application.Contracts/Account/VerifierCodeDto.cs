using System.Text.Json.Serialization;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class VerifierCodeDto
{
    [JsonPropertyName("verificationDoc")] [Id(0)] public string VerificationDoc { get; set; }

    [JsonPropertyName("signature")] [Id(1)] public string Signature { get; set; }
}