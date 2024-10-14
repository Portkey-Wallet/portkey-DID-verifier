using CAVerifierServer.Verifier.Dtos;
using CAVerifierServer.Account;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

[GenerateSerializer]
public class VerifyTelegramTokenGrainDto : VerifierCodeDto
{
    [Id(0)] public TelegramUserExtraInfo TelegramUserExtraInfo { get; set; }
}