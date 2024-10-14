namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

[GenerateSerializer]
public class VerifyTokenGrainDto
{
    [Id(0)] public string AccessToken { get; set; }
    [Id(1)] public string IdentifierHash { get; set; }
    [Id(2)] public string Salt { get; set; }
    [Id(3)] public string OperationType { get; set; }

    [Id(4)] public string ChainId { get; set; }

    [Id(5)] public string OperationDetails { get; set; }
}