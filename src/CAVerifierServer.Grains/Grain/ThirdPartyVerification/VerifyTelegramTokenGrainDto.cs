using CAVerifierServer.Verifier.Dtos;
using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public class VerifyTelegramTokenGrainDto : VerifierCodeDto
{
    public TelegramUserExtraInfo TelegramUserExtraInfo { get; set; }
}