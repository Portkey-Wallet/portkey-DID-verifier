using System;
using System.Threading.Tasks;
using CAVerifierServer.Account.Dtos;
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
    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        try
        {
            await grain.ValidateTelegramTokenAsync(revokeCodeDto.VerifyCode);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e,"validate Telegram Token error,{error}",e.Message);
            return false;
        }
       
    }
}