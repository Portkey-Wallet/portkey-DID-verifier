using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account;
using CAVerifierServer.Verifier.Dtos;
using Serilog;

namespace CAVerifierServer.Exception;

public class ApplicationExceptionHandler
{
    public async Task<FlowBehavior> SendVerificationRequestHandler(System.Exception e)
    {
        Log.Fatal(e, Error.SendVerificationRequestErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.SendVerificationRequestErrorLogPrefix + e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> SendNotificationRequestHandler(System.Exception e)
    {
        Log.Fatal(e, "SendNotificationRequest error");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<bool>
            {
                Success = false,
                Message = e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> SendTransactionInformationBeforeApprovalHandler(System.Exception e)
    {
        Log.Fatal(e, "SendTransactionInformationBeforeApproval error");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<bool>()
            {
                Success = false,
                Message = e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyCodeHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }

    public async Task<FlowBehavior> VerifySecondaryEmailCodeHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<bool>
            {
                Success = false,
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }

    public async Task<FlowBehavior> SendVerificationToSecondaryEmailHandler(System.Exception e)
    {
        Log.Fatal(e, Error.SendVerificationRequestErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.SendVerificationRequestErrorLogPrefix + e.Message
            }
        };
    }

    public async Task<FlowBehavior> VerifyGoogleTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifyGoogleTokenDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyAppleTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifyAppleTokenDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyTelegramTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifyTokenDto<TelegramUserExtraInfo>>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyFacebookTokenHandler(System.Exception e)
    {
        Log.Fatal(e, Error.VerifyCodeErrorLogPrefix + e.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifierCodeDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyFacebookAccessTokenHandler(System.Exception e)
    {
        Log.Fatal(e, "Verify AccessToken failed");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifyFacebookTokenResponseDto>
            {
                Success = false,
                Message = "Verify Facebook accessToken failed."
            }
        };
    }
    
    public async Task<FlowBehavior> VerifyTwitterTokenHandler(System.Exception e)
    {
        Log.Fatal(e, "verify twitter token error");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResponseResultDto<VerifyTwitterTokenDto>
            {
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            }
        };
    }
    
    public async Task<FlowBehavior> SendTextMessageHandler(System.Exception e)
    {
        Log.Fatal(e, "SendTextMessage error");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    public async Task<FlowBehavior> DoSendSmsMessageHandler(System.Exception e)
    {
        Log.Fatal(e, "SendSmsMessage error");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
    
    public async Task<FlowBehavior> SendEmailHandler(System.Exception e)
    {
        Log.Fatal(e, "send aws email failed");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
    
    public async Task<FlowBehavior> HandleShowOperationDetailsJsonHandler(System.Exception e)
    {
        Log.Fatal(e, "HandleShowOperationDetailsJson failed");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = string.Empty
        };
    }
    
    public async Task<FlowBehavior> HandleCommunityListHandler(System.Exception e)
    {
        Log.Fatal(e, "HandleCommunityList failed");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = string.Empty
        };
    }
    public async Task<FlowBehavior> VerifyRevokeCodeHandler(System.Exception e)
    {
        Log.Fatal(e, "Verify Revoke Code failed");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}