using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Exception;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAVerifierServer.VerifyRevokeCode;

public class GoogleRevokeCodeValidator : IVerifyRevokeCodeValidator
{
    
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GoogleRevokeCodeValidator> _logger;

    public GoogleRevokeCodeValidator(IClusterClient clusterClient, ILogger<GoogleRevokeCodeValidator> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public string Type => "Google";
    
    [ExceptionHandler(typeof(System.Exception), Message = "validate google token failed",
        TargetType = typeof(ApplicationExceptionHandler), 
        MethodName = nameof(ApplicationExceptionHandler.VerifyRevokeCodeHandler))]
    public virtual async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        await grain.GetUserInfoFromGoogleAsync(revokeCodeDto.VerifyCode);
        return true;
    }
}