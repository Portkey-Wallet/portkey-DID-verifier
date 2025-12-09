using System;
using System.Text.Json.Serialization;

namespace CAVerifierServer.Account;

public class SendVerificationRequestDto
{
    [JsonPropertyName("verifierSessionId")]
    public Guid VerifierSessionId{ get; set; }
}