using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public class VerifyAppleTokenGrainDto : VerifierCodeDto
{
    public AppleUserExtraInfo AppleUserExtraInfo { get; set; }
}