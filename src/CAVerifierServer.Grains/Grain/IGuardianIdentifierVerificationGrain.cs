using CAVerifierServer.Account;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Grains.Dto;
using Orleans;

namespace CAVerifierServer.Grains.Grain;

public interface IGuardianIdentifierVerificationGrain : IGrainWithStringKey
{
    Task<GrainResultDto<VerifyCodeDto>> GetVerifyCodeAsync(SendVerificationRequestInput input);
    Task<GrainResultDto<UpdateVerifierSignatureDto>> VerifyAndCreateSignatureAsync(VerifyCodeInput input);
    Task<GrainResultDto<bool>> VerifySecondaryEmailCodeAsync(SecondaryEmailVerifyCodeInput input);
    Task<GrainResultDto<VerifyRevokeCodeResponseDto>> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto);
}