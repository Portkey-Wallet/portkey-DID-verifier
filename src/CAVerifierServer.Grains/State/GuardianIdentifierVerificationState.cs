namespace CAVerifierServer.Grains.State;

[GenerateSerializer]
public class GuardianIdentifierVerificationState
{
    [Id(0)] public List<GuardianIdentifierVerification> GuardianTypeVerifications { get; set; }
}