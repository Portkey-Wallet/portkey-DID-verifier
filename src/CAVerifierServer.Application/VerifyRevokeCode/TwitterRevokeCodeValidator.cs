using System;
using System.Threading.Tasks;
using CAVerifierServer.Account.Dtos;
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

    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        try
        {
            await grain.GetTwitterUserInfoAsync(revokeCodeDto.VerifyCode);
            return true;
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "validate Twitter Token error,{error}", e.Message);
            return false;
        }
    }
}