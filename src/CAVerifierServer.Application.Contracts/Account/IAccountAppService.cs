using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace CAVerifierServer.Account;

public interface IAccountAppService : IApplicationService
{

     Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(SendVerificationRequestInput input);
     
     Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input);

     Task<string> WhiteListCheckAsync(List<string> pubkeyList);
     
     Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(VerifyTokenRequestDto tokenRequestDto);
     Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(VerifyTokenRequestDto tokenRequestDto);
}