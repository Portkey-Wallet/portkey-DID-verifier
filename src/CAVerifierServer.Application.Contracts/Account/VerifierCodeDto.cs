using System.Text.Json.Serialization;

namespace CAVerifierServer.Account;

public class VerifierCodeDto
{
    [JsonPropertyName("verificationDoc")] public string VerificationDoc { get; set; }

    [JsonPropertyName("signature")] public string Signature { get; set; }
}