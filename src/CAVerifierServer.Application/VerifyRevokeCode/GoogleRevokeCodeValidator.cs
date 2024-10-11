using System;
using System.Threading.Tasks;
using CAVerifierServer.Account.Dtos;
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
    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IThirdPartyVerificationGrain>(revokeCodeDto.VerifyCode);

        try
        {
            await grain.GetUserInfoFromGoogleAsync(revokeCodeDto.VerifyCode);
            return true;
        }
        catch (System.Exception e)
        {
           _logger.LogError(e,"validate google Token error,{error}",e.Message);
           return false;
        }
       
    }
}