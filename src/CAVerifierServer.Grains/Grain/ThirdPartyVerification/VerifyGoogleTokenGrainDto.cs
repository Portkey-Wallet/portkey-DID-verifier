using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

[GenerateSerializer]
public class VerifyGoogleTokenGrainDto : VerifierCodeDto
{
    [Id(0)] public GoogleUserExtraInfo GoogleUserExtraInfo { get; set; }
}