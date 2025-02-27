using Orleans;

namespace CAVerifierServer;

[GenerateSerializer]
public class UpdateVerifierSignatureDto
{
    [Id(0)] public string Data { get; set; }
    
    [Id(1)] public string Signature { get; set; }
}