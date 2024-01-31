using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public class VerifyTwitterTokenGrainDto : VerifierCodeDto
{
    public TwitterUserExtraInfo TwitterUserExtraInfo { get; set; }
}