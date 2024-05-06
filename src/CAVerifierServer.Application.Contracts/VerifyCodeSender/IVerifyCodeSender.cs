using System.Threading.Tasks;

namespace CAVerifierServer.VerifyCodeSender;

public interface IVerifyCodeSender
{
    string Type { get; }

    Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code, string showOperateDetail = "");
    
    bool ValidateGuardianIdentifier(string guardianIdentifier);
    
    
}