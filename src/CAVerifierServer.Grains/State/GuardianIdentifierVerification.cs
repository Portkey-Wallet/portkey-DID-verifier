namespace CAVerifierServer.Grains.State;

[GenerateSerializer]
public class GuardianIdentifierVerification
{
    [Id(0)] public string GuardianIdentifier { get; set; }

    [Id(1)] public string GuardianType{ get; set; }

    [Id(2)] public string VerificationCode{ get; set; }

    [Id(3)] public DateTime VerificationCodeSentTime{ get; set; }

    [Id(4)] public DateTime VerifiedTime{ get; set; }

    [Id(5)] public bool Verified{ get; set; }
    
    [Id(6)] public string VerificationDoc { get; set; }

    [Id(7)] public string Signature{ get; set; }
    
    [Id(8)] public int ErrorCodeTimes { get; set; }
    
    [Id(9)] public Guid VerifierSessionId { get; set; }
    
    [Id(10)] public string Salt { get; set; }

    [Id(11)] public string GuardianIdentifierHash { get; set; }

    [Id(12)] public string OperationDetails { get; set; }
}