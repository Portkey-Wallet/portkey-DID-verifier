using System.Threading.Tasks;
using CAVerifierServer.Verifier.Dtos;
using CAVerifierServer.Account;
using CAVerifierServer.Account.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace CAVerifierServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CAVerification")]
[Route("api/app/account")]
public class CAVerificationController : CAVerifierServerController
{
    private readonly IAccountAppService _accountAppService;
    private readonly ILogger<CAVerificationController> _logger;

    public CAVerificationController(IAccountAppService accountAppService,
        ILogger<CAVerificationController> logger)
    {
        _accountAppService = accountAppService;
        _logger = logger;
    }

    [HttpPost]
    [Route("sendVerificationRequest")]
    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(
        SendVerificationRequestInput input)
    {
        var sendVerificationRequestAsync = await _accountAppService.SendVerificationRequestAsync(input);
        return sendVerificationRequestAsync;
    }

    [HttpPost]
    [Route("verifyCode")]
    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
    {
        return await _accountAppService.VerifyCodeAsync(input);
    }
    
    [HttpPost]
    [Route("secondaryEmail/verifyCode")]
    public async Task<ResponseResultDto<bool>> VerifySecondaryEmailCodeAsync(SecondaryEmailVerifyCodeInput input)
    {
        return await _accountAppService.VerifySecondaryEmailCodeAsync(input);
    }
    
    [HttpPost]
    [Route("send/secondary/email/verify")]
    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationToSecondaryEmail(SecondaryEmailVerificationInput input)
    {
        var result = await _accountAppService.SendVerificationToSecondaryEmail(input);
        return result;
    }
    
    [HttpPost]
    [Route("sendNotification")]
    public async Task<ResponseResultDto<bool>> SendNotificationRequestAsync(
        SendNotificationRequest request)
    {
        request.Template = EmailTemplate.AfterApproval;
        return await _accountAppService.SendNotificationRequestAsync(request);
    }

    [HttpPost("verifyGoogleToken")]
    public async Task<ResponseResultDto<VerifyGoogleTokenDto>> VerifyGoogleTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        return await _accountAppService.VerifyGoogleTokenAsync(tokenRequestDto);
    }

    [HttpPost("verifyAppleToken")]
    public async Task<ResponseResultDto<VerifyAppleTokenDto>> VerifyAppleTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        return await _accountAppService.VerifyAppleTokenAsync(tokenRequestDto);
    }
    
    [HttpPost]
    [Route("verifyFacebookToken")]
    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyFacebookTokenAsync(VerifyTokenRequestDto input)
    {
        return await _accountAppService.VerifyFacebookTokenAsync(input);
    }
    
    [HttpPost]
    [Route("verifyFacebookAccessTokenAndGetUserId")]
    public async Task<ResponseResultDto<VerifyFacebookTokenResponseDto>> VerifyFacebookAccessTokenAndGetUserId(VerifyFacebookAccessTokenRequestDto request)
    {
        return await _accountAppService.VerifyFacebookAccessTokenAsync(request.AccessToken);
    }
    
    

    [HttpPost("verifyTelegramToken")]
    public async Task<ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>> VerifyTelegramTokenAsync(
        VerifyTokenRequestDto tokenRequestDto)
    {
        return await _accountAppService.VerifyTelegramTokenAsync(tokenRequestDto);
    }
    
    [HttpPost("verifyTwitterToken")]
    public async Task<ResponseResultDto<VerifyTwitterTokenDto>> VerifyAppleTwitterAsync(VerifyTokenRequestDto tokenRequestDto)
    {
        return await _accountAppService.VerifyTwitterTokenAsync(tokenRequestDto);
    }
    
    [HttpPost("verifyRevokeCode")]
    public async Task<VerifyRevokeCodeResponseDto> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        return await _accountAppService.VerifyRevokeCodeAsync(revokeCodeDto);
    }
}