using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Castle.Core.Logging;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Exception;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAVerifierServer.VerifyRevokeCode;

public class AppleRevokeCodeValidator : IVerifyRevokeCodeValidator
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AppleRevokeCodeValidator> _logger;

    public AppleRevokeCodeValidator(IClusterClient clusterClient, ILogger<AppleRevokeCodeValidator> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public string Type  =>  "Apple";

    [ExceptionHandler(typeof(System.Exception), Message = "validate apple token failed",
        TargetType = typeof(ApplicationExceptionHandler), 
        MethodName = nameof(ApplicationExceptionHandler.VerifyRevokeCodeHandler))]
    public virtual async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        await grain.ValidateTokenAsync(revokeCodeDto.VerifyCode);
        return true;
    }
}