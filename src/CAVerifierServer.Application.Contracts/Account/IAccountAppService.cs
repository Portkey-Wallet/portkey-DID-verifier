using System.Collections.Generic;
using System.Threading.Tasks;
using CAVerifierServer.Verifier.Dtos;
using Volo.Abp.Application.Services;

namespace CAVerifierServer.Account;

public interface IAccountAppService : IApplicationService
{

     Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(SendVerificationRequestInput input);
     
     Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input);

     Task<string> WhiteListCheckAsync(List<string> ipList);
     
     Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(VerifyTokenRequestDto tokenRequestDto);
     Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(VerifyTokenRequestDto tokenRequestDto);

     
     Task<ResponseResultDto<VerifierCodeDto>> VerifyFacebookTokenAsync(VerifyTokenRequestDto tokenRequestDto);
     Task<ResponseResultDto<VerifyFacebookTokenResponseDto>> VerifyFacebookAccessTokenAsync(string accessToken);

     Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(VerifyTokenRequestDto tokenRequestDto);

}