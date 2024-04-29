using System;
using System.Text.Json.Serialization;

namespace CAVerifierServer.Account.Dtos;

public class VerifyRevokeCodeDto
{
    [JsonPropertyName("guardianIdentifier")]
    public string GuardianIdentifier { get; set; }

    [JsonPropertyName("VerifierSessionId")]
    public Guid VerifierSessionId{ get; set; }
    
    [JsonPropertyName("VerifyCode")]
    public string VerifyCode{ get; set; }
    
    [JsonPropertyName("Type")]
    public string Type{ get; set; }
}