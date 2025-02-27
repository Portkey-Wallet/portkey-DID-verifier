using AElf.ExceptionHandler;
using CAVerifierServer.Account;
using CAVerifierServer.Grains.Dto;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace CAVerifierServer.Grains.Grain.Exception;

public class GrainVerifyExceptionHandler
{
    public async Task<FlowBehavior> VerifyGoogleTokenHandler(System.Exception e)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new GrainResultDto<VerifyGoogleTokenGrainDto>
            {
                Message = e.Message
            }
        };
    }

    public async Task<FlowBehavior> VerifyAppleTokenHandler(System.Exception e)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new GrainResultDto<VerifyAppleTokenGrainDto>
            {
                Message = e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyFacebookTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyFacebookErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new GrainResultDto<VerifierCodeDto>
            {
                Message = e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyTelegramTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new GrainResultDto<VerifyTelegramTokenGrainDto>
            {
                Message = e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyTwitterTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyTwitterErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue =  new GrainResultDto<VerifyTwitterTokenGrainDto>
            {
                Message = e.Message
            }
        };
    }

    public async Task<FlowBehavior> ValidateTokenSecurityTokenExpiredHandler(SecurityTokenExpiredException e)
    {
        Log.Fatal(e, Error.VerifyAppleErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new System.Exception(ThirdPartyMessage.TokenExpiresMessage)
        };
    }
    
    public async Task<FlowBehavior> ValidateTokenSecurityTokenHandler(SecurityTokenException e)
    {
        Log.Fatal(e, Error.VerifyAppleErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue =  new System.Exception(ThirdPartyMessage.InvalidTokenMessage)
        };
    }
    
    public async Task<FlowBehavior> ValidateTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyAppleErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue =  new System.Exception(ThirdPartyMessage.InvalidTokenMessage)
        };
    }
    
    public async Task<FlowBehavior> ValidateTelegramSecurityTokenExpiredHandler(SecurityTokenExpiredException e)
    {
        Log.Fatal(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new System.Exception(ThirdPartyMessage.TokenExpiresMessage)
        };
    }
    
    public async Task<FlowBehavior> ValidateTelegramSecurityTokenHandler(SecurityTokenException e)
    {
        Log.Fatal(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue =  new System.Exception(ThirdPartyMessage.InvalidTokenMessage)
        };
    }
    
    public async Task<FlowBehavior> ValidateTelegramTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyTelegramErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue =  new System.Exception(ThirdPartyMessage.InvalidTokenMessage)
        };
    }
}