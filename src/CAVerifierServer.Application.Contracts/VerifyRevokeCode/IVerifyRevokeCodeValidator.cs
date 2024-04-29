using System.Threading.Tasks;
using CAVerifierServer.Account.Dtos;

namespace CAVerifierServer.VerifyRevokeCode;

public interface IVerifyRevokeCodeValidator
{
    string Type { get; }

    Task<bool> VerifyRevokeCodeAsync(VerifyRevokeCodeDto verifyRevokeCodeDto);
}