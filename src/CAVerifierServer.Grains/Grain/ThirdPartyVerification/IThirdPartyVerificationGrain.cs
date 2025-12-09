using CAVerifierServer.Account;
using CAVerifierServer.Grains.Dto;
using Microsoft.IdentityModel.Tokens;
using Orleans;

namespace CAVerifierServer.Grains.Grain.ThirdPartyVerification;

public interface IThirdPartyVerificationGrain : IGrainWithStringKey
{
    Task<GrainResultDto<VerifyGoogleTokenGrainDto>> VerifyGoogleTokenAsync(VerifyTokenGrainDto tokenGrainDto);
    Task<GrainResultDto<VerifyAppleTokenGrainDto>> VerifyAppleTokenAsync(VerifyTokenGrainDto tokenGrainDto);
    Task<GrainResultDto<VerifyTelegramTokenGrainDto>> VerifyTelegramTokenAsync(VerifyTokenGrainDto tokenGrainDto);
    Task<GrainResultDto<VerifierCodeDto>> VerifyFacebookTokenAsync(VerifyTokenGrainDto tokenGrainDto);
    Task<GrainResultDto<VerifyTwitterTokenGrainDto>> VerifyTwitterTokenAsync(VerifyTokenGrainDto grainDto);
    Task<SecurityToken> ValidateTokenAsync(string identityToken);
    Task<GoogleUserInfoDto> GetUserInfoFromGoogleAsync(string accessToken);
    Task<SecurityToken> ValidateTelegramTokenAsync(string identityToken);
    Task<TwitterUserInfo> GetTwitterUserInfoAsync(string accessToken);
}