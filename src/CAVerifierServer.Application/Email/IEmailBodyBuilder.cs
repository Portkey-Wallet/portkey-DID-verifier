using System.Threading.Tasks;

namespace CAVerifierServer.Email;

public interface IEmailBodyBuilder
{
    public Task<string> BuildBodyTemplateWithOperationDetails(string verifierName, string image, string portkey,
        string verifyCode, string showOperationDetails);

    public Task<string> BuildTransactionTemplateBeforeApproval(string verifierName, string image, string portkey,
        string showOperationDetails);

    public Task<string> BuildTransactionTemplateAfterApproval(string verifierName, string image, string portkey,
        string showOperationDetails);

    public Task<string> BuildBodyTemplateForSecondaryEmail(string verifierName, string image, string portkey, string verifyCode);
}