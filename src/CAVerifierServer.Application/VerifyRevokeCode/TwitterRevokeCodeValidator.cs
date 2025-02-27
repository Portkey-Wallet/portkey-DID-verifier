using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Exception;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAVerifierServer.VerifyRevokeCode;

public class TwitterRevokeCodeValidator : IVerifyRevokeCodeValidator
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TwitterRevokeCodeValidator> _logger;

    public TwitterRevokeCodeValidator(IClusterClient clusterClient, ILogger<TwitterRevokeCodeValidator> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public string Type  => "Twitter";

    [ExceptionHandler(typeof(System.Exception), Message = "validate Twitter token failed",
        TargetType = typeof(ApplicationExceptionHandler),
        MethodName = nameof(ApplicationExceptionHandler.VerifyRevokeCodeHandler))]
    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        await grain.GetTwitterUserInfoAsync(revokeCodeDto.VerifyCode);
        return true;
    }
}