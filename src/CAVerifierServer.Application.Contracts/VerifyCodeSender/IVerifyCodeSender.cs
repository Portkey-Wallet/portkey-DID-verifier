using System.Threading.Tasks;
using CAVerifierServer.Account;

namespace CAVerifierServer.VerifyCodeSender;

public interface IVerifyCodeSender
{
    string Type { get; }

    Task SendCodeByGuardianIdentifierAsync(string guardianIdentifier, string code, string showOperateDetail = "");

    Task SendCodeToSecondaryEmailAsync(string guardianIdentifier, string code);
    
    bool ValidateGuardianIdentifier(string guardianIdentifier);

    Task SendTransactionInfoNotificationAsync(string email, EmailTemplate template, string showOperationDetails);
}