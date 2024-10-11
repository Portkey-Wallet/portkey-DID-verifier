using AElf.ExceptionHandler;
using CAVerifierServer.Grains.Dto;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;

namespace CAVerifierServer.Grains.Grain.Exception;

public class GrainVerifyExceptionHandler
{
    public static async Task<FlowBehavior> VerifyGoogleTokenHandler(System.Exception e)
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

    public static async Task<FlowBehavior> VerifyAppleTokenHandler(System.Exception e)
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
}