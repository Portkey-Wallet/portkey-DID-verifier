using System;
using System.Text.Json.Serialization;
using Orleans;

namespace CAVerifierServer.Account.Dtos;

[GenerateSerializer]
public class VerifyRevokeCodeDto
{
    [JsonPropertyName("guardianIdentifier")]
    [Id(0)] public string GuardianIdentifier { get; set; }

    [JsonPropertyName("VerifierSessionId")]
    [Id(1)] public Guid VerifierSessionId{ get; set; }
    
    [JsonPropertyName("VerifyCode")]
    [Id(2)] public string VerifyCode{ get; set; }
    
    [JsonPropertyName("Type")]
    [Id(3)] public string Type{ get; set; }
}