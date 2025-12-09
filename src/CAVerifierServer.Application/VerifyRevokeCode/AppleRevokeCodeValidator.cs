using System;
using System.Threading.Tasks;
using Castle.Core.Logging;
using CAVerifierServer.Account.Dtos;
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

    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);
        try
        {
            await grain.ValidateTokenAsync(revokeCodeDto.VerifyCode);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e,"validate apple token failed :{message}", e.Message);
            return false;
        }
    }
}