namespace CAVerifierServer.Grains.State;

public class GuardianIdentifierVerification
{
    public string GuardianIdentifier { get; set; }

    public string GuardianType{ get; set; }

    public string VerificationCode{ get; set; }

    public DateTime VerificationCodeSentTime{ get; set; }

    public DateTime VerifiedTime{ get; set; }

    public bool Verified{ get; set; }
    
    public string VerificationDoc { get; set; }

    public string Signature{ get; set; }
    
    public int ErrorCodeTimes { get; set; }
    
    public Guid VerifierSessionId { get; set; }
    
    public string Salt { get; set; }

    public string GuardianIdentifierHash { get; set; }

    public string OperateDetails { get; set; }
}