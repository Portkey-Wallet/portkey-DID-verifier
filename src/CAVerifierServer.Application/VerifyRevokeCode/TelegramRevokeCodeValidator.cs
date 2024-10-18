using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Exception;
using CAVerifierServer.Grains.Grain.ThirdPartyVerification;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAVerifierServer.VerifyRevokeCode;

public class TelegramRevokeCodeValidator : IVerifyRevokeCodeValidator
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TelegramRevokeCodeValidator> _logger;

    public TelegramRevokeCodeValidator(ILogger<TelegramRevokeCodeValidator> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public string Type => "Telegram";
    
    [ExceptionHandler(typeof(System.Exception), Message = "validate Telegram token failed",
        TargetType = typeof(ApplicationExceptionHandler), 
        MethodName = nameof(ApplicationExceptionHandler.VerifyRevokeCodeHandler))]
    public virtual async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        await grain.ValidateTelegramTokenAsync(revokeCodeDto.VerifyCode);
        return true;
    }
}