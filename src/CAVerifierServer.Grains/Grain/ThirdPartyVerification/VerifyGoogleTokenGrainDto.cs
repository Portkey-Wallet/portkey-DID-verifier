using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public class VerifyGoogleTokenGrainDto : VerifierCodeDto
{
    public GoogleUserExtraInfo GoogleUserExtraInfo { get; set; }
}