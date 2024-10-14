using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

[GenerateSerializer]
public class VerifyAppleTokenGrainDto : VerifierCodeDto
{
    [Id(0)] public AppleUserExtraInfo AppleUserExtraInfo { get; set; }
}