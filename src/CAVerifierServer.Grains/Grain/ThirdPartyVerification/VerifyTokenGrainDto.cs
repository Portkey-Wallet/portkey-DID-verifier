namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public class VerifyTokenGrainDto
{
    public string AccessToken { get; set; }
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string OperationType { get; set; }
    
    public string MerklePath { get; set; }
    
    
}