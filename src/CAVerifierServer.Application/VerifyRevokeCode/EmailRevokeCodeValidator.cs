using System.Threading.Tasks;
using CAVerifierServer.Account.Dtos;
using CAVerifierServer.Grains.Grain;
using Microsoft.Extensions.Logging;
using Orleans;

namespace CAVerifierServer.VerifyRevokeCode;

public class EmailRevokeCodeValidator : IVerifyRevokeCodeValidator
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<EmailRevokeCodeValidator> _logger;

    public EmailRevokeCodeValidator(IClusterClient clusterClient, ILogger<EmailRevokeCodeValidator> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public string Type => "Email";
    public async Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto revokeCodeDto)
    {
        var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(revokeCodeDto.GuardianIdentifier);
        var resultDto = await grain.VerifyRevokeCodeAsync(revokeCodeDto);
        if (resultDto.Success)
        {
            return true;
        }
        _logger.LogError("validate Email verifyCode failed:{reason}",resultDto.Message);
        return false;
    }
}