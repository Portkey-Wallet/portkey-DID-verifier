using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

[GenerateSerializer]
public class VerifyTwitterTokenGrainDto : VerifierCodeDto
{
    [Id(0)] public TwitterUserExtraInfo TwitterUserExtraInfo { get; set; }
}