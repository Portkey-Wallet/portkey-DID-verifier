using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account;
using Serilog;

namespace CAVerifierServer.Exception;

public class ApplicationExceptionHandler
{
    public static async Task<FlowBehavior> SendVerificationRequestHandler(System.Exception e)
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
    
    public static async Task<FlowBehavior> SendNotificationRequestHandler(System.Exception e)
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
    
    public static async Task<FlowBehavior> SendTransactionInformationBeforeApprovalHandler(System.Exception e)
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
    
    public static async Task<FlowBehavior> VerifyCodeHandler(System.Exception e)
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

    public static async Task<FlowBehavior> VerifySecondaryEmailCodeHandler(System.Exception e)
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

    public static async Task<FlowBehavior> SendVerificationToSecondaryEmailHandler(System.Exception e)
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
}