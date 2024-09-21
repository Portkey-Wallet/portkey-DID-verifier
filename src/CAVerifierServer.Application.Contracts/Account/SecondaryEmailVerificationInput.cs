using System;
using System.Text.Json.Serialization;

namespace CAVerifierServer.Account;

public class SecondaryEmailVerificationInput
{
    [JsonPropertyName("SecondaryEmail")]
    public string SecondaryEmail { get; set; }
    
    [JsonPropertyName("VerifierSessionId")]
    public Guid VerifierSessionId { get; set; }
}