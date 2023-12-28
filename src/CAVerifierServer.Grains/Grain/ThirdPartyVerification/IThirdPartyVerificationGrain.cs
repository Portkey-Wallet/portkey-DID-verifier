using CAVerifierServer.Grains.Dto;
using Orleans;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public interface IThirdPartyVerificationGrain : IGrainWithStringKey
{
    Task<GrainResultDto<VerifyGoogleTokenGrainDto>> VerifyGoogleTokenAsync(VerifyTokenGrainDto tokenGrainDto);
    Task<GrainResultDto<VerifyAppleTokenGrainDto>> VerifyAppleTokenAsync(VerifyTokenGrainDto tokenGrainDto);
    Task<GrainResultDto<VerifyTelegramTokenGrainDto>> VerifyTelegramTokenAsync(VerifyTokenGrainDto tokenGrainDto);
}