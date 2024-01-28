using System;
using System.Text.Json.Serialization;

namespace CAVerifierServer.Account;

public class SendVerificationRequestInput
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; }
    
    [JsonPropertyName("verificationRequest")]
    public string VerificationRequest { get; set; }
}