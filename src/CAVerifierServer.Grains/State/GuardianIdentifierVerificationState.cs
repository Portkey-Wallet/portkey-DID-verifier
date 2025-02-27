namespace CAVerifierServer.Grains.State;

[GenerateSerializer]
[Alias("CAVerifierServer.Grains.State.GuardianIdentifierVerificationState")]
public class GuardianIdentifierVerificationState
{
    [Id(0)] public List<GuardianIdentifierVerification> GuardianTypeVerifications { get; set; }
}