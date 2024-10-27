using System;
using System.Text.Json.Serialization;
using Orleans;

namespace CAVerifierServer.Account;

[GenerateSerializer]
public class SendVerificationRequestInput
{
    [JsonPropertyName("type")]
    [Id(0)] 
    public string Type { get; set; }
    
    [JsonPropertyName("guardianIdentifier")]
    [Id(1)] 
    public string GuardianIdentifier { get; set; }

    [JsonPropertyName("VerifierSessionId")]
    [Id(2)]
    public Guid VerifierSessionId{ get; set; }

    [JsonPropertyName("OperationDetails")]
    [Id(3)] 
    public string OperationDetails { get; set; }
    
    [JsonPropertyName("ShowOperationDetails")]
    [Id(4)]
    public string ShowOperationDetails { get; set; }
}